using FINE.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace FINE.Data.MakeConnection
{
    public static class MakeConnection
    {
        public static IServiceCollection ConnectToConnectionString(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FineDevDbV2Context>(ServiceLifetime.Transient);
            services.AddDbContext<FineDevDbV2Context>(options =>
            {
                options.UseLazyLoadingProxies();
                options.UseSqlServer(configuration.GetConnectionString("SQLServerDatabase"), sql => sql.UseNetTopologySuite());
            });
            return services;
        }
    }
}