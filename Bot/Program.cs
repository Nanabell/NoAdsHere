using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Bot
{
    internal class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static void Main() => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            Logger.Info("Hello World!");

            Logger.Info("Loading Configuration");
            var config = CreateConfiguration();

            Logger.Info("Loading Service Provider");
            var provider = CreateProvider(config);

            var bot = provider.GetRequiredService<Bot>(); 
            await bot.StartAsync();

            
            
            await Task.Delay(-1);
        }

        private static IServiceProvider CreateProvider(IConfiguration configuration)
        {
            return new ServiceCollection()
                .AddSingleton<Bot>()
                .AddSingleton(configuration)
                .BuildServiceProvider();
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile("config.yaml", false, true)
                .Build();
        }
    }
}