using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using NoAdsHere.Services.LogService;
using NoAdsHere.Services.Penalties;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Violations
{
    public static class Violations
    {
        private static DiscordShardedClient _client;
        private static LogChannelService _logChannelService;
        private static IUnitOfWork _unit;
        private static IConfigurationRoot _config;
        private static ILoggerFactory _factory;
        private static ILogger _logger;

        public static Task Install(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordShardedClient>();
            _unit = provider.GetService<IUnitOfWork>();
            _logChannelService = provider.GetService<LogChannelService>();
            _config = provider.GetService<IConfigurationRoot>();
            _factory = provider.GetService<ILoggerFactory>();
            _logger = _factory.CreateLogger(typeof(Violations));
            return Task.CompletedTask;
        }

        public static async Task Add(ICommandContext context, BlockType blockType)
        {
            var violator = await _unit.Violators.GetOrCreateAsync(context.User as IGuildUser);
            violator = DecreasePoints(context, violator);
            violator = IncreasePoint(context, violator);
            await ExecutePenalty(context, violator, blockType).ConfigureAwait(false);
            _unit.SaveChanges();
        }

        private static Violator IncreasePoint(ICommandContext context, Violator violator)
        {
            violator.LatestViolation = DateTime.UtcNow;
            violator.Points++;
            _logger.LogInformation(new EventId(200), $"{context.User}'s Points {violator.Points - 1} => {violator.Points}");
            return violator;
        }

        private static async Task ExecutePenalty(ICommandContext context, Violator violator, BlockType blockType)
        {
            var penalties = (await _unit.Penalties.GetOrCreateAllAsync(context.Guild)).ToList();
            var stats = await _unit.Statistics.GetOrCreateAsync(context.Guild);
            stats.Blocks++;

            foreach (var penalty in penalties.OrderBy(p => p.RequiredPoints))
            {
                if (violator.Points != penalty.RequiredPoints) continue;
                var message = penalty.Message ?? GetDefaultMessage(penalty.PenaltyType);

                string logresponse;
                switch (penalty.PenaltyType)
                {
                    case PenaltyType.Nothing:
                        await MessagePenalty.SendWithEmoteAsync(_factory, context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.Id} (Nothing {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}.";
                        _logger.LogInformation(new EventId(200), logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:NoAds:330796107540201472>"), logresponse);
                        break;

                    case PenaltyType.Warn:
                        await MessagePenalty.SendWithEmoteAsync(_factory, context, message, GetTrigger(blockType),
                            Emote.Parse("<:Warn:330799457371160579>"), penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.Id} (Warn {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}.";
                        _logger.LogInformation(new EventId(200), logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Warn:330799457371160579>"), logresponse);
                        stats.Warns++;
                        break;

                    case PenaltyType.Kick:
                        await KickPenalty.KickAsync(_factory, context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.Id} (Kick {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}";
                        _logger.LogInformation(new EventId(200), logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Kick:330793607919566852>"), logresponse);
                        stats.Kicks++;
                        break;

                    case PenaltyType.Ban:
                        await BanPenalty.BanAsync(_factory, context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.Id} (Ban {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}";
                        _logger.LogInformation(new EventId(200), logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Ban:330793436309487626>"), logresponse);
                        stats.Bans++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (violator.Points >= penalties.Max(p => p.RequiredPoints))
            {
                _unit.Violators.Remove(violator);
                _logger.LogInformation(new EventId(200), $"{context.User} reached the last penalty in {context.Guild}, dropping from Database.");
            }
        }

        private static string GetDefaultMessage(PenaltyType penaltyType)
        {
            switch (penaltyType)
            {
                case PenaltyType.Nothing:
                    return "Advertisement is not allowed in this Server";

                case PenaltyType.Warn:
                    return "Advertisement is not allowed in this Server! ***Last Warning***";

                case PenaltyType.Kick:
                    return "has been kicked for Advertisement";

                case PenaltyType.Ban:
                    return "has been banned for Advertisement";

                default:
                    throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
            }
        }

        private static string GetTrigger(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.InstantInvite:
                    return "Message contained an Invite.";

                case BlockType.TwitchStream:
                    return "Message contained a Twitch Stream.";

                case BlockType.TwitchVideo:
                    return "Message contained a Twitch Video.";

                case BlockType.TwitchClip:
                    return "Message contained a Twitch Clip.";

                case BlockType.YoutubeLink:
                    return "Message contained a YouTube Link.";

                case BlockType.SteamScam:
                    return "Message contained a Steam Advertisement Link!";

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null);
            }
        }

        public static Violator DecreasePoints(ICommandContext context, Violator violator)
        {
            var decPoints = 0;
            var time = violator.LatestViolation;
            while (DateTime.UtcNow > time)
            {
                if (DateTime.UtcNow > time.AddHours(Convert.ToDouble(_config["PointDecreaseHours"])))
                {
                    if (decPoints == violator.Points)
                        break;

                    time = time.AddHours(Convert.ToDouble(_config["PointDecreaseHours"]));
                    decPoints++;
                    violator.Points = violator.Points - decPoints <= 0 ? 0 : violator.Points - decPoints;
                    violator.LatestViolation = time;
                }
                else break;
            }
            _logger.LogInformation(new EventId(200), $"Decreased {context.User}'s points({violator.Points + decPoints}) by {decPoints} for a total of {violator.Points}");
            return violator;
        }
    }
}