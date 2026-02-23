using Microsoft.Extensions.DependencyInjection;
using psecsapi.Console.Commands;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all ICommand implementations found in the assembly.
        /// </summary>
        public static IServiceCollection AddCommands(this IServiceCollection services)
        {
            var commandTypes = typeof(ICommand).Assembly
                .GetTypes()
                .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in commandTypes)
            {
                services.AddSingleton(typeof(ICommand), type);
            }

            return services;
        }

        /// <summary>
        /// Registers configuration services.
        /// </summary>
        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<ConfigRepository>();
            services.AddSingleton<CliConfig>(sp =>
            {
                var repo = sp.GetRequiredService<ConfigRepository>();
                return new CliConfig(repo);
            });

            return services;
        }

        /// <summary>
        /// Registers HTTP clients.
        /// </summary>
        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            // Base HttpClient with SSL handling for dev certificates
            services.AddSingleton<HttpClient>(sp =>
            {
                var config = sp.GetRequiredService<CliConfig>();
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(config.System.BaseUrl!)
                };
                return client;
            });

            // Authenticated client with auto-refresh
            services.AddSingleton<AuthenticatedHttpClient>(sp =>
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                var config = sp.GetRequiredService<CliConfig>();
                return new AuthenticatedHttpClient(httpClient, config);
            });

            return services;
        }
    }
}
