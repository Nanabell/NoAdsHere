using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Discord.Commands;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Database;
using Discord.Addons.InteractiveCommands;
using NoAdsHere.Services.Events;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.Log;
using Quartz;
using Quartz.Impl;

namespace NoAdsHere
{
    internal class Program
    {
        private static void Main() =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordShardedClient _client;
        private Config _config;
        private MongoClient _mongo;
        private readonly Logger _logger = LogManager.GetLogger("Core");
        private IServiceProvider _provider;
        private IScheduler _scheduler;

        public async Task RunAsync()
        {
            _config = Config.Load();
            
            _logger.Info($"Creating Discord Sharded Client with {_config.TotalShards} Shards");
            _client = new DiscordShardedClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                TotalShards = _config.TotalShards,
                MessageCacheSize = 10,
#if DEBUG
                LogLevel = LogSeverity.Debug,
#else
                LogLevel = LogSeverity.Verbose,
#endif
            });

            await EventHandlers.StartHandlers(_client);

            
            _mongo = CreateDatabaseConnection();
            _scheduler = await StartQuartz();
            DatabaseBase.Mongo = _mongo;
            DatabaseBase.Client = _client;

            _provider = ConfigureServices();
            await EventHandlers.StartServiceHandlers(_provider);

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }


        private static async Task<IScheduler> StartQuartz()
        {
            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();
            await scheduler.Start();
            return scheduler;
        }

        private MongoClient CreateDatabaseConnection()
        {
            _logger.Info("Connecting to Mongo Database");
            return new MongoClient(_config.Database.ConnectionString);
        }

        private IServiceProvider ConfigureServices()
        {
            _logger.Info("Configuring dependency injection and services...");
            var servies = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_config)
                .AddSingleton(_mongo)
                .AddSingleton(new LogChannelService(_config))
                .AddSingleton(_scheduler)
                .AddSingleton(new FaqSystem(_client, _mongo))
                .AddSingleton(new InteractiveService(_client.Shards.First()))
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false, ThrowOnError = false, LogLevel = LogSeverity.Verbose, DefaultRunMode = RunMode.Sync}));

            var provider = servies.BuildServiceProvider();
            return provider;
        }
    }
}