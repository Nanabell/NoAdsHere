using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using NoAdsHere.Database.Entities;
using NoAdsHere.Services.Advertisement;
using NoAdsHere.Services.Events;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.Github;

namespace NoAdsHere
{
    public static class Program
    {
        private static DiscordShardedClient _client;
        private static IConfiguration _config;
        private static ILogger _logger;

        public static async Task Main()
        {
            _config = BuildConfiguration();
            var provider = ConfigureServices(_config);

            _logger = provider.GetService<ILoggerFactory>().CreateLogger(typeof(Program));
            _client = provider.GetService<DiscordShardedClient>();
            _config = provider.GetService<IConfiguration>();

            provider.GetService<EventLogger>();
            provider.GetService<AntiAdvertisementService>();
            provider.GetService<GithubService>();
            
            var comands = new CommandHandler(provider);
            await comands.LoadModulesAndStartAsync();

            _logger.LogInformation($"Starting client with {_config.Get<Config>().Shards} shard/s");
            await _client.LoginAsync(TokenType.Bot, _config.Get<Config>().Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }



        private static IServiceProvider ConfigureServices(IConfiguration config)
        {
            var provider = new ServiceCollection()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 10,
                    TotalShards = config.Get<Config>().Shards
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Debug,
                    ThrowOnError = true,
                    DefaultRunMode = RunMode.Sync
                }))
                .AddSingleton(_config)
                //.AddSingleton<WebhookLogService>()
                .AddSingleton<AntiAdvertisementService>()
                .AddSingleton<EventLogger>()
                .AddSingleton<FaqService>()
                .AddSingleton<GithubService>()
                //.AddSingleton<FaqService>()
                //.AddSingleton<ViolationsServiceOld>()
                //.AddSingleton<GithubService>()
                //.AddSingleton<LockdownService>()
                //.AddSingleton(GetTaskScheduler().GetAwaiter().GetResult())
                .BuildServiceProvider();

            ConfigureLogging(provider);

            return provider;
        }

        private static void ConfigureLogging(IServiceProvider provider)
        {
            var factory = provider.GetService<ILoggerFactory>();
            factory.AddNLog();
            factory.ConfigureNLog("../../../NLog.config");
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile("config.yaml", false, true)
                .Build();
        }
        /*
        private static async Task<IScheduler> GetTaskScheduler()
        {
            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();
            await scheduler.Start();
            return scheduler;
        }
        */
    }
}