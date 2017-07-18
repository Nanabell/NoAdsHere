using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Database.Models.Guild;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.LogService;
using NoAdsHere.Services.Penalties;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Services.Events
{
    public static class EventHandlers
    {
        private static DiscordShardedClient _client;
        private static DatabaseService _database;
        private static Config _config;
        private static LogChannelService _logger;
        private static readonly Logger Logger = LogManager.GetLogger("EventHandler");

        public static Task StartHandlers(DiscordShardedClient client)
        {
            _client = client;

            foreach (var shard in _client.Shards.OrderBy(socketClient => socketClient.ShardId))
            {
                async Task Handler()
                {
                    Logger.Info($"Shard {shard.ShardId} Ready");
                    await _logger.LogMessageAsync(_client, shard, Emote.Parse("<:Science:330479610812956672>"),
                        $"Ready `G:{shard.Guilds.Count}, U:{shard.Guilds.Sum(g => g.Users.Count)}`");
                    shard.Ready -= Handler;
                }
                shard.Ready += Handler;
            }

            _client.Log += ClientLogger;
            return Task.CompletedTask;
        }

        public static async Task StartServiceHandlers(IServiceProvider provider)
        {
            _logger = provider.GetService<LogChannelService>();
            _config = provider.GetService<Config>();
            _database = provider.GetService<DatabaseService>();

            Logger.Info("Installing CommandHandler");
            await CommandHandler.Install(provider);
            await CommandHandler.ConfigureAsync();

            Logger.Info("Installing FAQ Service");
            await FaqService.Install(provider);
            await FaqService.LoadFaqs();

            Logger.Info("Installing AntiAds Service");
            await AntiAds.AntiAds.Install(provider);
            await AntiAds.AntiAds.StartAsync();

            Logger.Info("Installing Violations Service");
            await Violations.Violations.Install(provider);

            Logger.Info("Loading JobQueue");
            await JobQueue.Install(provider);

            _client.JoinedGuild += JoinedGuild;
            _client.LeftGuild += LeftGuild;
        }

        public static Task ClientLogger(LogMessage message)
        {
            var logger = LogManager.GetLogger("Discord");
            if (message.Exception == null)
                logger.Log(LogLevelParser(message.Severity), message.Message);
            else
                logger.Log(LogLevelParser(message.Severity), message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public static Task WebhookLogger(LogMessage message)
        {
            var logger = LogManager.GetLogger("Webhook");
            if (message.Exception == null)
                logger.Info(message.Message);
            else
                logger.Warn(message.Exception, message.Message);
            return Task.CompletedTask;
        }

        private static LogLevel LogLevelParser(LogSeverity severity)
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

        public static Task CommandLogger(LogMessage message)
        {
            var logger = LogManager.GetLogger("Command");
            if (message.Exception == null)
                logger.Info(message.Message);
            else
                logger.Warn(message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public static async Task JoinedGuild(SocketGuild guild)
        {
            await JoinLog(guild).ConfigureAwait(false);
            var logger = LogManager.GetLogger("AntiAds");
            var penalties = await _database.GetPenaltiesAsync(guild.Id);
            var blocks = await _database.GetBlocksAsync(guild.Id);

            if (!penalties.Any())
            {
                var newPenalties = new List<Penalty>
                {
                    new Penalty(guild.Id, 1, PenaltyType.Nothing, 1),
                    new Penalty(guild.Id, 2, PenaltyType.Warn, 3),
                    new Penalty(guild.Id, 3, PenaltyType.Kick, 5),
                    new Penalty(guild.Id, 4, PenaltyType.Ban, 6)
                };

                logger.Info("Adding default penalties.");
                await _database.InsertManyAsync(newPenalties);
            }
            else
            {
                Logger.Info($"Joined Guild {guild} but {penalties.Count} Penalties were already present, no Default Penalties");
            }

            if (!blocks.Any())
            {
                try
                {
                    await guild.DefaultChannel.SendMessageAsync(
                        "Thank you for inviting NAH. Please note that I'm currently in an Inactive state.\n" +
                        $"Please head over to github for documentations & a quickstart guide how to enable me.*({_config.Prefix.First()}github)*\n" +
                        "I've automatically added the default Penalties please change them to your needs!");
                    logger.Info($"Sent Joinmessage in {guild}/{guild.DefaultChannel}");
                }
                catch (Exception e)
                {
                    logger.Warn(e, $"Failed to send Joinmessage in {guild}/{guild.DefaultChannel}");
                }
            }
            else
            {
                Logger.Info($"Joined Guild {guild} but {blocks.Count} Blocks were already present, No joinMessage");
            }
        }

        private static async Task JoinLog(SocketGuild guild)
        {
            var shard = _client.GetShardFor(guild);
            await _logger.LogMessageAsync(_client, shard, Emote.Parse("<:Yes:330454342282903563>"),
                $"Joined guild `{guild}({guild.Id})`. Users: `{guild.Users.Count}`.");
        }

        private static async Task LeftGuild(SocketGuild socketGuild)
        {
            await LeaveLog(socketGuild).ConfigureAwait(false);
        }

        private static async Task LeaveLog(SocketGuild guild)
        {
            var shard = _client.GetShardFor(guild);
            await _logger.LogMessageAsync(_client, shard, Emote.Parse("<:No:330454370535604224>"),
                $"Left guild `{guild}({guild.Id})`. Users: `{guild.Users.Count}`.");
        }
    }
}