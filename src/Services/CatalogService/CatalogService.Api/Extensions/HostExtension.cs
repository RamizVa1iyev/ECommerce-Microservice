using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CatalogService.Api.Extensions
{
    public static class HostExtension
    {
        public static IServiceCollection MigrateDbContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<TContext>>();
            var context = serviceProvider.GetService<TContext>();

            try
            {
                logger.LogInformation("Migrating database assosiated with context {DbContextName}", typeof(TContext).Name);
                var retry = Policy.Handle<SqlException>()
                    .WaitAndRetry(new TimeSpan[]
                    {
                            TimeSpan.FromSeconds(3),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(8),
                    });

                retry.Execute(() =>
                {
                    context.Database.EnsureCreated();
                    context.Database.Migrate();
                });

                logger.LogInformation("Migrating database assosiated with context {DbContextName}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while migrating  the database used on context {DbContextName}", typeof(TContext).Name);
            }
            return services;
        }
    }
}
