using Autofac;
using FINE.API.Helpers;
using FINE.API.Mapper;
using FINE.Data.MakeConnection;
using FINE.Data.Repository;
using FINE.Data.UnitOfWork;
using FINE.Service.Helpers;
using FINE.Service.Service;
using FirebaseAdmin;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.OpenApi.Models;
using System.Text;

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
                        builder
                        //.WithOrigins(GetDomain())
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });
            services.AddControllersWithViews();
            services.AddControllers(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FINE API",
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
                c.AddSecurityDefinition("Bearer", securitySchema);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                        securitySchema,
                    new string[] { "Bearer" }
                    }
                });
            });
            services.ConnectToConnectionString(Configuration);

            #region JWT

            var appSettingsSection = Configuration.GetSection("Token");

            services.Configure<Token>(appSettingsSection);
            var appSettings = appSettingsSection.Get<Token>();

            var key = Encoding.ASCII.GetBytes(appSettings.SecretKey);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
            });
            var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "firebase.json");
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(pathToKey)
            });

            #endregion JWT

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register your own things directly with Autofac, like:
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>();

            builder.RegisterType<FirebaseMessagingService>().As<IFirebaseMessagingService>();
            builder.RegisterType<MembershipCardService>().As<IMembershipCardService>();
            builder.RegisterType<FcmTokenService>().As<IFcmTokenService>();
            builder.RegisterType<CustomerService>().As<ICustomerService>();
            builder.RegisterType<AccountService>().As<IAccountService>();
            builder.RegisterType<CampusService>().As<ICampusService>();

            builder.RegisterGeneric(typeof(GenericRepository<>))
            .As(typeof(IGenericRepository<>))
            .InstancePerLifetimeScope();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            //app.ConfigMigration<>();
            app.UseCors(MyAllowSpecificOrigins);
            app.UseExceptionHandler("/error");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FINE_API V1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseDeveloperExceptionPage();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
