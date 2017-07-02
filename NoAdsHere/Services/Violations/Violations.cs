using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Database.Models.Violator;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.LogService;
using NoAdsHere.Services.Penalties;

namespace NoAdsHere.Services.Violations
{
    public static class Violations
    {
        private static DiscordShardedClient _client;
        private static MongoClient _mongo;
        private static LogChannelService _logChannelService;
        private static Config _config;
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static Task Install(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordShardedClient>();
            _mongo = provider.GetService<MongoClient>();
            _logChannelService = provider.GetService<LogChannelService>();
            _config = provider.GetService<Config>();
            return Task.CompletedTask;
        }

        public static async Task Add(ICommandContext context, BlockType blockType)
        {
            var violator = await _mongo.GetCollection<Violator>(_client).GetUserAsync(context.Guild.Id, context.User.Id);
            violator = await TryDecreasePoints(context, violator).ConfigureAwait(false);
            violator = await IncreasePoint(context, violator).ConfigureAwait(false);
            await ExecutePenalty(context, violator, blockType).ConfigureAwait(false);
        }

        private static async Task<Violator> IncreasePoint(ICommandContext context, Violator violator)
        {
            violator.LatestViolation = DateTime.UtcNow;
            violator.Points++;
            Logger.Info($"{context.User}'s Points {violator.Points - 1} => {violator.Points}");
            await violator.SaveAsync();
            return violator;
        }

        private static async Task ExecutePenalty(ICommandContext context, Violator violator, BlockType blockType)
        {
            var penalties = await _mongo.GetCollection<Penalty>(_client).GetPenaltiesAsync(violator.GuildId);
            var collection = _mongo.GetCollection<Stats>(_client);
            var stats = await collection.GetGuildStatsAsync(context.Guild);
            stats.Blocks++;

            foreach (var penalty in penalties.OrderBy(p => p.RequiredPoints))
            {
                if (violator.Points != penalty.RequiredPoints) continue;
                var message = penalty.Message ?? GetDefaultMessage(penalty.PenaltyType);


                string logresponse;
                switch (penalty.PenaltyType)
                {
                    case PenaltyType.Nothing:
                        await MessagePenalty.SendWithEmoteAsync(context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.PenaltyId} (Nothing {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}.";
                        Logger.Info(logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:NoAds:330796107540201472>"), logresponse);
                        break;

                    case PenaltyType.Warn:
                        await MessagePenalty.SendWithEmoteAsync(context, message, GetTrigger(blockType),
                            Emote.Parse("<:Warn:330799457371160579>"), penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.PenaltyId} (Warn {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}.";
                        Logger.Info(logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Warn:330799457371160579>"), logresponse);
                        stats.Warns++;
                        break;

                    case PenaltyType.Kick:
                        await KickPenalty.KickAsync(context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.PenaltyId} (Kick {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}";
                        Logger.Info(logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Kick:330793607919566852>"), logresponse);
                        stats.Kicks++;
                        break;

                    case PenaltyType.Ban:
                        await BanPenalty.BanAsync(context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        logresponse =
                            $"{context.User} reached Penalty {penalty.PenaltyId} (Ban {penalty.RequiredPoints}p) in {context.Guild}/{context.Channel}";
                        Logger.Info(logresponse);
                        await _logChannelService.LogMessageAsync(_client, _client.GetShardFor(context.Guild),
                            Emote.Parse("<:Ban:330793436309487626>"), logresponse);
                        stats.Bans++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            await stats.SaveAsync();

            if (violator.Points >= penalties.Max(p => p.RequiredPoints))
            {
                await violator.DeleteAsync();
                Logger.Info($"{context.User} reached the last penalty in {context.Guild}, dropping from Database.");
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

        public static string GetTrigger(BlockType blockType)
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

                case BlockType.All:
                    return "[Error] This Message should never appear!";

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null);
            }
        }

        public static async Task<Violator> TryDecreasePoints(ICommandContext context, Violator violator)
        {
            var decPoints = 0;
            var time = violator.LatestViolation;
            while (DateTime.UtcNow > time)
            {
                if (DateTime.UtcNow > time.AddHours(_config.PointDecreaseHours))
                {
                    if (decPoints == violator.Points)
                        break;
                    
                    time = time.AddHours(_config.PointDecreaseHours);
                    decPoints++;
                    violator.Points = violator.Points - decPoints <= 0 ? 0 : violator.Points - decPoints;
                    violator.LatestViolation = time;
                }
                else break;
            }
            Logger.Info($"Decreased {context.User}'s points({violator.Points + decPoints}) by {decPoints} for a total of {violator.Points}");
            await violator.SaveAsync();
            return violator;
        }
    }
}