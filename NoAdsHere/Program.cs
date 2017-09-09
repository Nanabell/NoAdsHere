using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NoAdsHere.Database;
using NoAdsHere.Database.UnitOfWork;
using NoAdsHere.Services.Events;
using NoAdsHere.Services.LogService;
using Quartz;
using Quartz.Impl;
using System;
using System.IO;
using System.Threading.Tasks;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.Github;
using NoAdsHere.Services.Violations;

namespace NoAdsHere
{
    public static class Program
    {
        private static DiscordShardedClient _client;
        private static IConfigurationRoot _config;
        private static NoAdsHereContext _context;
        private static ILogger _logger;

        public static async Task Main()
        {
            _context = new NoAdsHereContext();
            await _context.Database.MigrateAsync();

            _config = BuildConfiguration();
            var provider = ConfigureServices(_config);

            _logger = provider.GetService<ILoggerFactory>().CreateLogger(typeof(Program));
            _client = provider.GetService<DiscordShardedClient>();
            _config = provider.GetService<IConfigurationRoot>();

            _logger.LogInformation(new EventId(100), $"Starting client with {_config["Shards"]} shard/s");

            await EventHandlers.StartServiceHandlers(provider);

            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private static async Task<IScheduler> GetTaskScheduler()
        {
            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();
            await scheduler.Start();
            return scheduler;
        }

        private static IServiceProvider ConfigureServices(IConfigurationRoot config)
        {
            var provider = new ServiceCollection()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 10,
                    TotalShards = Convert.ToInt32(config["Shards"])
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Debug,
                    ThrowOnError = true,
                    DefaultRunMode = RunMode.Sync
                }))
                .AddSingleton(_config)
                .AddSingleton(new NoAdsHereUnit(new NoAdsHereContext()) as IUnitOfWork)
                .AddSingleton(new LogChannelService(_config))
                .AddSingleton<AntiAdsService>()
                .AddSingleton<FaqService>()
                .AddSingleton<ViolationsService>()
                .AddSingleton<GithubService>()
                .AddSingleton(GetTaskScheduler().GetAwaiter().GetResult())
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

        public static IConfigurationRoot BuildConfiguration()
        {
            try
            {
                return new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddYamlFile("config.yaml", false, true)
                    .Build();
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("Configuration File not found!");
            }
        }
    }
}