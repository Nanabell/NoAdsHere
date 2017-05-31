using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Discord.Commands;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.Penalties;
using NoAdsHere.Services.Violations;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using System.Linq;
using System.Collections.Generic;
using NoAdsHere.Common;

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
        private readonly bool ReadyExecuted = false;
        private IServiceProvider provider;

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
            _client.Ready += Ready;
            _client.JoinedGuild += JoinedGuild;

            _config = Config.Load();
            _mongo = CreateDatabaseConnection();

            provider = ConfigureServices();

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task Ready()
        {
            if (!ReadyExecuted)
            {
                await Task.Delay(500);
                _handler = new CommandHandler(provider);
                await _handler.ConfigureAsync();

                await Violations.Install(provider);

                await AntiAds.Install(provider);
                await AntiAds.StartServiceAsync();
            }
        }

        private async Task JoinedGuild(SocketGuild guild)
        {
            var collection = _mongo.GetCollection<Penalty>(_client);
            var penalties = await collection.GetPenaltiesAsync(guild.Id);
            var newPenalties = new List<Penalty>();

            if (penalties.All(p => p.PenaltyId != 1))
            {
                newPenalties.Add(new Penalty(guild.Id, 1, PenaltyType.Nothing, 1));
                _logger.Info("Adding default InfoMessage Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 2))
            {
                newPenalties.Add(new Penalty(guild.Id, 2, PenaltyType.Warn, 3));
                _logger.Info("Adding default WarnMessage Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 3))
            {
                newPenalties.Add(new Penalty(guild.Id, 3, PenaltyType.Kick, 5));
                _logger.Info("Adding default Kick Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 4))
            {
                newPenalties.Add(new Penalty(guild.Id, 4, PenaltyType.Ban, 6));
                _logger.Info("Adding default Ban Penalty");
            }

            if (newPenalties.Any())
                await collection.InsertManyAsync(newPenalties);
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