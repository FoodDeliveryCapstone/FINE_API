using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FINE.API.AppStart
{
    public static class AuthConfig
    {
        public static void ConfigureAuthServices(this IServiceCollection services, IConfiguration Configuration)
        {
            var tokenConfig = Configuration.GetSection("Token");
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenConfig["SecretKey"])),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = tokenConfig["Issuer"],
                    ValidAudience = tokenConfig["Issuer"]
                };
            });
            //authorization
            services.AddAuthorization(config =>
                {
                    config.AddPolicy("FineAuthorization", policyBuilder =>
                        policyBuilder.RequireClaim("Role")
                    );
                });

            services.AddScoped<IAuthorizationHandler, RolesAuthorizationHandler>();
        }                        

        public static void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
    public class RolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       RolesAuthorizationRequirement requirement)
        {
            if (context.User == null || !context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var validRole = false;
            if (requirement.AllowedRoles == null ||
                requirement.AllowedRoles.Any() == false)
            {
                validRole = true;
            }
            else
            {
                var claims = context.User.Claims;
                var userRoles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
                var roles = requirement.AllowedRoles;

                validRole = roles.Any(r => userRoles.Contains(r));
            }

            if (validRole)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
}
