using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using NoAdsHere.Services.FAQ;
using NoAdsHere.Services.Github;
using NoAdsHere.Services.LogService;
using NoAdsHere.Services.Penalties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.Violations;

namespace NoAdsHere.Services.Events
{
    public static class EventHandlers
    {
        private static DiscordShardedClient _client;
        private static IUnitOfWork _unit;
        private static IConfigurationRoot _config;
        private static LogChannelService _channellogger;
        private static CommandHandler _handler;
        private static ILoggerFactory _factory;
        private static ILogger _logger;

        public static async Task StartServiceHandlers(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordShardedClient>();
            _channellogger = provider.GetService<LogChannelService>();
            _config = provider.GetService<IConfigurationRoot>();
            _unit = provider.GetService<IUnitOfWork>();
            _factory = provider.GetService<ILoggerFactory>();
            _logger = _factory.CreateLogger(typeof(EventHandlers));
            var adsService = provider.GetService<AntiAdsService>();
            var cmdService = provider.GetService<CommandService>();
            var faqService = provider.GetService<FaqService>();
            var githubService = provider.GetService<GithubService>();

            await ReadyLogger();

            _handler = new CommandHandler(provider);
            await _handler.LoadModulesAndStartAsync();
            _logger.LogInformation(new EventId(200), "Created & Started CommandHandler");

            await faqService.LoadFaqsAsync();
            _logger.LogInformation(new EventId(200), "Installed and loaded FAQ Service");

            adsService.StartService();
            _logger.LogInformation(new EventId(200), "Started AntiAds Service");

            await JobQueue.Install(provider);
            _logger.LogInformation(new EventId(200), "Loaded JobQueue");

            await githubService.StartAsync();
            _logger.LogInformation(new EventId(200), "Started Github service");

            _client.Log += ClientLogger;
            cmdService.Log += CommandLogger;

            _client.JoinedGuild += JoinedGuild;
            _client.LeftGuild += LeftGuild;
        }

        public static async Task StopCommandHandlerAsync()
        {
            await _handler.StopHandler();
        }

        private static Task ReadyLogger()
        {
            foreach (var shard in _client.Shards.OrderBy(socketClient => socketClient.ShardId))
            {
                async Task Handler()
                {
                    _logger.LogInformation(new EventId(100), $"Shard {shard.ShardId} Ready");
                    await _channellogger.LogMessageAsync(_client, shard, Emote.Parse("<:Science:330479610812956672>"),
                        $"Ready `G:{shard.Guilds.Count}, U:{shard.Guilds.Sum(g => g.MemberCount)}`");
                    shard.Ready -= Handler;
                }
                shard.Ready += Handler;
            }

            return Task.CompletedTask;
        }

        private static Task ClientLogger(LogMessage message)
        {
            LogDiscord(message);
            return Task.CompletedTask;
        }

        public static Task WebhookLogger(LogMessage message)
        {
            var logger = _factory.CreateLogger("Webhook");
            if (message.Exception == null)
                logger.LogInformation(new EventId(0), message.Message);
            else
                logger.LogWarning(new EventId(500), message.Exception, message.Message);
            return Task.CompletedTask;
        }

        private static void LogDiscord(LogMessage logMessage)
        {
            var logger = _factory.CreateLogger("Discord");
            switch (logMessage.Severity)
            {
                case LogSeverity.Debug:
                    if (logMessage.Exception == null)
                        logger.LogTrace(new EventId(0), logMessage.Message);
                    else
                        logger.LogTrace(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                case LogSeverity.Verbose:
                    if (logMessage.Exception == null)
                        logger.LogDebug(new EventId(0), logMessage.Message);
                    else
                        logger.LogDebug(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                case LogSeverity.Info:
                    if (logMessage.Exception == null)
                        logger.LogInformation(new EventId(0), logMessage.Message);
                    else
                        logger.LogInformation(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                case LogSeverity.Warning:
                    if (logMessage.Exception == null)
                        logger.LogWarning(new EventId(515), logMessage.Message);
                    else
                        logger.LogWarning(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                case LogSeverity.Error:
                    if (logMessage.Exception == null)
                        logger.LogError(new EventId(500), logMessage.Message);
                    else
                        logger.LogError(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                case LogSeverity.Critical:
                    if (logMessage.Exception == null)
                        logger.LogCritical(new EventId(500), logMessage.Message);
                    else
                        logger.LogCritical(new EventId(500), logMessage.Exception, logMessage.Message);
                    return;

                default:
                    return;
            }
        }

        public static Task CommandLogger(LogMessage message)
        {
            var logger = _factory.CreateLogger("CommandService");
            if (message.Exception == null)
                logger.LogInformation(new EventId(0), message.Message);
            else
                logger.LogWarning(new EventId(500), message.Exception, message.Message);

            return Task.CompletedTask;
        }

        private static async Task JoinedGuild(SocketGuild guild)
        {
            await JoinLog(guild).ConfigureAwait(false);
            var penalties = (await _unit.Penalties.GetAllAsync(guild)).ToList();
            var blocks = (await _unit.Blocks.GetAllAsync(guild)).ToList();

            if (!penalties.Any())
            {
                var newPenalties = new List<Penalty>
                {
                    new Penalty(guild, PenaltyType.Nothing, 1),
                    new Penalty(guild, PenaltyType.Warn, 3),
                    new Penalty(guild, PenaltyType.Kick, 5),
                    new Penalty(guild, PenaltyType.Ban, 6)
                };

                _logger.LogInformation(new EventId(100), "Adding default penalties.");
                await _unit.Penalties.AddRangeAsync(newPenalties);
                _unit.SaveChanges();
            }
            else
            {
                _logger.LogWarning(new EventId(208), $"Joined Guild {guild} but {penalties.Count} Penalties were already present, no Default Penalties");
            }

            if (!blocks.Any())
            {
                try
                {
                    await guild.DefaultChannel.SendMessageAsync(
                        "Thank you for inviting NAH. Please note that I'm currently in an Inactive state.\n" +
                        $"Please head over to github for documentations & a quickstart guide how to enable me.*({_config["Prefixes:Main"]} github)*\n" +
                        "I've automatically added the default Penalties please change them to your needs!");
                    _logger.LogInformation(new EventId(200), $"Sent Joinmessage in {guild}/{guild.DefaultChannel}");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(new EventId(403), e, $"Failed to send Joinmessage in {guild}/{guild.DefaultChannel}");
                }
            }
            else
            {
                _logger.LogInformation(new EventId(208), $"Joined Guild {guild} but {blocks.Count} Blocks were already present, No joinMessage");
            }
        }

        private static async Task JoinLog(SocketGuild guild)
        {
            var shard = _client.GetShardFor(guild);
            await _channellogger.LogMessageAsync(_client, shard, Emote.Parse("<:Yes:330454342282903563>"),
                $"Joined guild `{guild}({guild.Id})`. Users: `{guild.Users.Count}`.");
        }

        private static async Task LeftGuild(SocketGuild socketGuild)
        {
            await LeaveLog(socketGuild).ConfigureAwait(false);
        }

        private static async Task LeaveLog(SocketGuild guild)
        {
            var shard = _client.GetShardFor(guild);
            await _channellogger.LogMessageAsync(_client, shard, Emote.Parse("<:No:330454370535604224>"),
                $"Left guild `{guild}({guild.Id})`. Users: `{guild.Users.Count}`.");
        }
    }
}