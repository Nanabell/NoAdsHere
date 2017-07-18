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
using Discord.Addons.InteractiveCommands;
using NoAdsHere.Services.Events;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.LogService;
using Quartz;
using Quartz.Impl;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using NoAdsHere.Services.Database;

namespace NoAdsHere
{
    internal class Program
    {
        private static void Main() =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordShardedClient _client;
        private Config _config;
        private MongoClient _mongo;
        private DatabaseService _database;
        private readonly Logger _logger = LogManager.GetLogger("Core");
        private IScheduler _scheduler;

        public async Task RunAsync()
        {
            _config = Config.Load();

            _logger.Info($"Creating Discord Sharded Client with {_config.TotalShards} Shards");
            _client = new DiscordShardedClient(new DiscordSocketConfig
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

            _logger.Info("Creating MongoClient");
            _mongo = CreateDatabaseConnection();
            _logger.Info("Adding Enum String Convention to ConventionRegistry");
            LoadConventionPack();

            _logger.Info("Starting DatabaseService");
            _database = ConfigureDatabaseService();

            _scheduler = await StartQuartz().ConfigureAwait(false);

            var provider = ConfigureServices();
            await EventHandlers.StartServiceHandlers(provider);

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
            bool connected;
            _logger.Info("Attempting to connect to Docker Database");
            var docker = new MongoClient("mongodb://database");
            var db = docker.GetDatabase("admin");
            connected = db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(2000);
            if (connected)
            {
                _logger.Info("Successfully conencted to docker database");
                return docker;
            }
            _logger.Fatal("Failed to connect to docker database falling back to config database");
            return new MongoClient(_config.Database.ConnectionString);
        }

        private void LoadConventionPack()
        {
            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("EnumStringConvention", pack, t => true);
        }

        private DatabaseService ConfigureDatabaseService()
            => new DatabaseService(_mongo, _config.Database.UseDb);

        private IServiceProvider ConfigureServices()
        {
            _logger.Info("Configuring dependency injection and services...");
            var servies = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_config)
                .AddSingleton(_mongo)
                .AddSingleton(_database)
                .AddSingleton(new LogChannelService(_config))
                .AddSingleton(_scheduler)
                .AddSingleton(new FaqSystem(_database, _config))
                .AddSingleton(new InteractiveService(_client.Shards.First()))
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false, ThrowOnError = false, LogLevel = LogSeverity.Verbose, DefaultRunMode = RunMode.Sync }));

            var provider = servies.BuildServiceProvider();
            return provider;
        }
    }
}