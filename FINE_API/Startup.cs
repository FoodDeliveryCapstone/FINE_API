using Autofac;
using FINE.API.Mapper;
using FINE.Data.MakeConnection;
using FINE.Data.Repository;
using FINE.Data.UnitOfWork;
using FINE.Service.Helpers;
using FirebaseAdmin;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using FINE.API.Helpers;
using FINE.Service.Service;

namespace FINE.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public static readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
#pragma warning disable CA1041 // Provide ObsoleteAttribute message

        [Obsolete]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });
            services.AddControllersWithViews();
            services.AddControllers(x => x.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()))).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                foreach (var converter in GeoJsonSerializer.Create(new GeometryFactory(new PrecisionModel(), 4326)).Converters)
                {
                    options.SerializerSettings.Converters.Add(converter);
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddControllers(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Fine API",
                    Version = "v1"
                });
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                c.AddSecurityDefinition("Bearer", securitySchema
                    );
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                        securitySchema,
                    new string[] { "Bearer" }
                    }
                }

                );
            });
            services.ConnectToConnectionString(Configuration);

            #region JWT

            var appSettingsSection = Configuration.GetSection("Token");

            services.Configure<Token>(appSettingsSection);
            var token = appSettingsSection.Get<Token>();

            var key = Encoding.ASCII.GetBytes(token.SecretKey);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(cfg =>
            {
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration.GetValue<string>("Token:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = Configuration.GetValue<string>("Token:Issuer"),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                };
            });

            #endregion JWT

            //===============================================
            //firebase
            var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "firebase.json");
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(pathToKey)
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register your own things directly with Autofac, like:
            builder.RegisterType<CampusService>().As<ICampusService>();
            builder.RegisterType<AccountService>().As<IAccountService>();
            builder.RegisterType<FcmTokenService>().As<IFcmTokenService>();
            builder.RegisterType<CustomerService>().As<ICustomerService>();
            builder.RegisterType<MembershipCardService>().As<IMembershipCardService>();
            builder.RegisterType<FirebaseMessagingService>().As<IFirebaseMessagingService>();

            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>();
            builder.RegisterGeneric(typeof(GenericRepository<>))
            .As(typeof(IGenericRepository<>))
            .InstancePerLifetimeScope();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            //app.ConfigMigration<TradeMapContext>();
            app.UseCors(MyAllowSpecificOrigins);
            app.UseExceptionHandler("/error");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fine Api V1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
