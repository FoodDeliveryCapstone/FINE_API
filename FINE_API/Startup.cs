﻿using Autofac;
using FINE.API.AppStart;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.OpenApi.Models;
using Reso.Core.Extension;
using ServiceStack.Redis;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using static FINE.Service.Service.ITimeslotService;

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

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(Configuration["Endpoint:RedisEndpoint"]));
            services.AddMemoryCache();
            services.ConfigMemoryCacheAndRedisCache(Configuration["Endpoint:RedisEndpoint"]);
            services.AddMvc(option => option.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(opt => opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

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

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer iJIUzI1NiIsInR5cCI6IkpXVCGlzIElzc2'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
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
            services.ConfigureAuthServices(Configuration);
            services.ConnectToConnectionString(Configuration);
            services.ConfigureHangfireServices(Configuration);

            ServiceHelpers.Initialize(Configuration);

            #region Firebase
            var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "firebase.json");
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(pathToKey)
            });
            #endregion 

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register your own things directly with Autofac, like:
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>();

            builder.RegisterType<FloorService>().As<IFloorService>();
            builder.RegisterType<DestinationService>().As<IDestinationService>();
            builder.RegisterType<StationService>().As<IStationService>();

            builder.RegisterType<StoreService>().As<IStoreService>();
            builder.RegisterType<TimeslotService>().As<ITimeslotService>();
            builder.RegisterType<ProductService>().As<IProductService>();
            builder.RegisterType<MenuService>().As<IMenuService>();

            builder.RegisterType<OrderService>().As<IOrderService>();
            builder.RegisterType<PaymentService>().As<IPaymentService>();
            builder.RegisterType<QrCodeService>().As<IQrCodeService>();

            builder.RegisterType<AccountService>().As<IAccountService>();
            builder.RegisterType<CustomerService>().As<ICustomerService>();
            builder.RegisterType<FcmTokenService>().As<IFcmTokenService>();
            builder.RegisterType<FirebaseMessagingService>().As<IFirebaseMessagingService>();

            builder.RegisterType<StaffService>().As<IStaffService>();
            builder.RegisterType<PackageService>().As<IPackageService>();
            builder.RegisterType<BoxService>().As<IBoxService>();
            builder.RegisterType<OrderDetailService>().As<IOrderDetailService>();      
            builder.RegisterType<NotifyService>().As<INotifyService>();
            builder.RegisterType<ShipperService>().As<IShipperService>();
            builder.RegisterType<NotifyService>().As<INotifyService>();
            builder.RegisterType<SimulateService>().As<ISimulateService>();
            builder.RegisterType<ProductAttributeService>().As<IProductAttributeService>();
            builder.RegisterType<TransactionService>().As<ITransactionService>();
            builder.RegisterType<ProductInMenuService>().As<IProductInMenuService>();
            builder.RegisterType<ImportService>().As<IImportService>();
            builder.RegisterType<LogService>().As<ILogService>();

            builder.Register<IRedisClientsManager>(c =>
            new RedisManagerPool(Configuration.GetConnectionString("RedisConnectionString")));
            builder.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());

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
            AuthConfig.Configure(app);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
