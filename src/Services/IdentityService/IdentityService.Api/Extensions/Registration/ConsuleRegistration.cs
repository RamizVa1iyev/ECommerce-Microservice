using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Text;

namespace IdentityService.Api.Extensions.Registration
{
    public static class ConsuleRegistration
    {
        public static IServiceCollection ConfigureConsul(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = configuration["ConsulConfig:Address"];
                consulConfig.Address = new Uri(address);
            }));

            return services;
        }

        public static IApplicationBuilder RegisterWithConsul(this IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

            var loggingFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            var logger = loggingFactory.CreateLogger<IApplicationBuilder>();

            var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>();
            var firstUrl = serverAddresses.Addresses.FirstOrDefault();

            //Get Server IP Address
            var features = app.Properties["server.Features"] as FeatureCollection;
            var addresses = features.Get<IServerAddressesFeature>();
            var address = addresses.Addresses.First();

            var uri = new Uri(address);

            var registration = new AgentServiceRegistration
            {
                ID = "IdentityService",
                Name = "IdentityService",
                Address = $"{uri.Host}",
                Port = uri.Port,
                Tags = new[] { "Identity Service", "Identity" }
            };

            logger.LogInformation("Registering consul");

            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            consulClient.Agent.ServiceRegister(registration).Wait();

            //Add swagger url
            var swaggerUrl = $"{address}/swagger/v1/swagger.json";

            var putPair = new KVPair($"{registration.ID}/swagger")
            {
                Value = Encoding.UTF8.GetBytes(swaggerUrl)
            };

            consulClient.KV.Put(putPair).Wait();

            lifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("Deregistering from consul");
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });

            return app;
        }
    }
}
