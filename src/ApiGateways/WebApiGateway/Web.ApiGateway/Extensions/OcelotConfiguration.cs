using Consul;
using System.Text;

namespace Web.ApiGateway.Extensions
{
    public static class OcelotConfiguration
    {
        public static IApplicationBuilder AddOcelotConfiguration(this IApplicationBuilder application,IConfigurationBuilder configuration)
        {
            //var serviceProvider = services.BuildServiceProvider();
            var consulClient = application.ApplicationServices.GetService<IConsulClient>();

            

            // Get the configuration root
            var configurationRoot = configuration.Build();

            // Modify the URLs in the SwaggerEndPoints section
            var swaggerEndPoints = configurationRoot.GetSection("SwaggerEndPoints").GetChildren();
            foreach (var endPoint in swaggerEndPoints)
            {
                string key = endPoint.GetValue<string>("Key");
                var configSection = endPoint.GetSection("Config");
                var sections = configSection.GetChildren();
                foreach (var section in sections)
                {

                    var urlBytes = consulClient.KV.Get($"{key}/swagger").Result.Response.Value;
                    var url = Encoding.UTF8.GetString(urlBytes);
                    section["Url"] = url;
                }
            }


            return application;
        }

        public static IServiceCollection AddConsul(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient,ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = configuration["ConsulConfig:Address"];
                consulConfig.Address = new Uri(address);
            }));

            return services;
        }
    }
}
