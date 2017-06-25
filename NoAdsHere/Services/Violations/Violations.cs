using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Database.Models.Violator;
using NoAdsHere.Services.Penalties;
using static NoAdsHere.ConstSettings;

namespace NoAdsHere.Services.Violations
{
    public static class Violations
    {
        private static DiscordSocketClient _client;
        private static MongoClient _mongo;
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static Task Install(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordSocketClient>();
            _mongo = provider.GetService<MongoClient>();
            return Task.CompletedTask;
        }

        public static async Task Add(ICommandContext context, BlockType blockType)
        {
            var violator = await _mongo.GetCollection<Violator>(_client).GetUserAsync(context.Guild.Id, context.User.Id);
            violator = await TryDecreasePoints(context, violator);
            violator = await IncreasePoint(context, violator);
            await ExecutePenalty(context, violator, blockType);
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
                var message = penalty.Message ?? GetDefaultMessage(blockType, penalty.PenaltyType);

                switch (penalty.PenaltyType)
                {
                    case PenaltyType.Nothing:
                        await MessagePenalty.SendAsync(context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"{context.User} reached Penalty Nothing {penalty.PenaltyId}({penalty.RequiredPoints}) in {context.Guild}/{context.Channel}.");
                        break;

                    case PenaltyType.Warn:
                        await MessagePenalty.SendAsync(context, message, GetTrigger(blockType),
                            ":warning:", penalty.AutoDelete);
                        Logger.Info(
                            $"{context.User} reached Penalty Warn {penalty.PenaltyId}({penalty.RequiredPoints}) in {context.Guild}/{context.Channel}");
                        stats.Warns++;
                        break;

                    case PenaltyType.Kick:
                        await KickPenalty.KickAsync(context, message, GetTrigger(blockType), autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"{context.User} reached Penalty Kick {penalty.PenaltyId}({penalty.RequiredPoints}) in {context.Guild}/{context.Channel}");
                        stats.Kicks++;
                        break;

                    case PenaltyType.Ban:
                        await BanPenalty.BanAsync(context, message, GetTrigger(blockType), autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"User {context.User} reached Penalty Ban {penalty.PenaltyId}({penalty.RequiredPoints}) in {context.Guild}/{context.Channel}");
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

        private static string GetDefaultMessage(BlockType blockType, PenaltyType penaltyType)
        {
            //TODO: Implement dffrent Messages for diffrent Blocktypes.
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

        private static int CalcDecreasingPoints(Violator violator)
        {
            var decPoints = 0;
            var time = violator.LatestViolation;
            while (DateTime.UtcNow > time)
            {
                if (DateTime.UtcNow > time.AddHours(PointDecreaseHours))
                {
                    time = time.AddHours(PointDecreaseHours);
                    decPoints++;
                }
                else break;
            }
            return decPoints;
        }

        public static async Task<Violator> TryDecreasePoints(ICommandContext context, Violator violator)
        {
            var points = CalcDecreasingPoints(violator);
            if (points <= 0) return violator;
            violator.LatestViolation = DateTime.UtcNow;
            violator.Points = points < violator.Points ? violator.Points - points : 0;
            Logger.Info($"Decreased points for {context.User} by {points} for a total of {violator.Points}");
            await violator.SaveAsync();
            return violator;
        }
    }
}