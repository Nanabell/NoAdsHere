using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using NoAdsHere.Services.Confguration;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Discord.Commands;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.Penalties;

namespace NoAdsHere
{
    internal class Program
    {
        private static void Main() =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private Config _config;
        private MongoClient _mongo;
        private CommandHandler _handler;
        private readonly Logger _logger = LogManager.GetLogger("Core");
        private readonly Logger _discordLogger = LogManager.GetLogger("Discord");

        public async Task RunAsync()
        {
            _logger.Info("Creating DiscordClient");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Debug,
#else
                LogLevel = LogSeverity.Verbose,
#endif
            });

            _client.Log += ClientLogger;

            _config = Config.Load();
            _mongo = CreateDatabaseConnection();

            var serviceProvider = ConfigureServices();

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            _handler = new CommandHandler(serviceProvider);
            await _handler.ConfigureAsync();

            await Violations.Install(serviceProvider);

            var inviteChecker = new DiscordInvites(serviceProvider);
            await inviteChecker.StartService();

            var youtubeChecker = new Youtube(serviceProvider);
            await youtubeChecker.StartService();

            var twitchChecker = new Twitch(serviceProvider);
            await twitchChecker.StartService();

            await Task.Delay(-1);
        }

        private MongoClient CreateDatabaseConnection()
        {
            _logger.Info("Connecting to MongoDb Database");
            return new MongoClient(_config.Database.ConnectionString);
        }

        private IServiceProvider ConfigureServices()
        {
            _logger.Info("Configuring ServiceDependencyMap");
            var servies = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_config)
                .AddSingleton(_mongo)
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false, ThrowOnError = false, LogLevel = LogSeverity.Verbose }));

            var provider = servies.BuildServiceProvider();
            return provider;
        }

        private Task ClientLogger(LogMessage message)
        {
            if (message.Exception == null)
                _discordLogger.Log(LogLevelParser(message.Severity), message.Message);
            else
                _discordLogger.Log(LogLevelParser(message.Severity), message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public static LogLevel LogLevelParser(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Debug:
                    return LogLevel.Trace;

                case LogSeverity.Verbose:
                    return LogLevel.Debug;

                case LogSeverity.Info:
                    return LogLevel.Info;

                case LogSeverity.Warning:
                    return LogLevel.Warn;

                case LogSeverity.Error:
                    return LogLevel.Error;

                case LogSeverity.Critical:
                    return LogLevel.Fatal;

                default:
                    return LogLevel.Off;
            }
        }
    }
}