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
using NoAdsHere.Services.Penalties;

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
            var violator = await _mongo.GetCollection<Violator>(_client).GetUserAsync(context.User as IGuildUser);
            await DecreasePoints(context, violator);
            await IncreasePoint(context, violator);
            await ExecutePenalty(context, violator, blockType);
        }

        private static async Task IncreasePoint(ICommandContext context, Violator violator)
        {
            var collection = _mongo.GetCollection<Violator>(_client);
            violator.LatestViolation = DateTime.Now;
            violator.Points++;
            Logger.Info($"Increased points for {context.User} by 1 for a total of {violator.Points}");
            await collection.SaveAsync(violator);
        }

        private static async Task ExecutePenalty(ICommandContext context, Violator violator, BlockType blockType)
        {
            var penalties = await _mongo.GetCollection<Penalty>(_client).GetPenaltiesAsync(violator.GuildId);

            foreach (var penalty in penalties.OrderBy(p => p.RequiredPoints))
            {
                if (violator.Points != penalty.RequiredPoints) continue;
                const string defaultMessage = "Advertisement is not allowed in this Server";
                var message = penalty.Message ?? defaultMessage;
                switch (penalty.PenaltyType)
                {
                    case PenaltyType.Nothing:
                        await MessagePenalty.SendAsync(context, message, GetTrigger(blockType),
                            autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"User {context.User} exceeded the limit for Penalty {penalty.PenaltyId}({penalty.RequiredPoints}) on {context.Guild}. Executing Penalty on Level Nothing");
                        break;

                    case PenaltyType.Warn:
                        await MessagePenalty.SendAsync(context, message + " ***Last Warning!***", GetTrigger(blockType),
                            ":warning:", penalty.AutoDelete);
                        Logger.Info(
                            $"User {context.User} exceeded the limit for Penalty {penalty.PenaltyId}({penalty.RequiredPoints}) on {context.Guild}. Executing Penalty on Level Warn");
                        break;

                    case PenaltyType.Kick:
                        await KickPenalty.KickAsync(context, message, GetTrigger(blockType), autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"User {context.User} exceeded the limit for Penalty {penalty.PenaltyId}({penalty.RequiredPoints}) on {context.Guild}. Executing Penalty on Level Kick");
                        break;

                    case PenaltyType.Ban:
                        await BanPenalty.BanAsync(context, message, GetTrigger(blockType), autoDelete: penalty.AutoDelete);
                        Logger.Info(
                            $"User {context.User} exceeded the limit for Penalty {penalty.PenaltyId}({penalty.RequiredPoints}) on {context.Guild}. Executing Penalty on Level Kick");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (violator.Points >= penalties.Max(p => p.RequiredPoints))
            {
                var collection = _mongo.GetCollection<Violator>(_client);
                await collection.DeleteAsync(violator);
                Logger.Info($"User {context.User} reached the last Penalty dropping from Database");
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
                    return "Message contained a Youtube Link.";

                case BlockType.All:
                    return "[Error] this Message should never appear!";

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null);
            }
        }

        private static int CalcDecreasingPoints(Violator violator)
        {
            var decPoints = 0;
            var time = violator.LatestViolation;
            while (DateTime.Now > time)
            {
                if (DateTime.Now > time.AddHours(12))
                {
                    time = time.AddHours(12);
                    decPoints++;
                }
                else break;
            }
            return decPoints;
        }

        private static async Task DecreasePoints(ICommandContext context, Violator violator)
        {
            var points = CalcDecreasingPoints(violator);
            if (points > 0)
            {
                var collection = _mongo.GetCollection<Violator>(_client);
                violator.Points = (points < violator.Points ? violator.Points - points : 0);
                Logger.Info($"Decreased Points for {context.User} by {points} for a total of {violator.Points}");
                await collection.SaveAsync(violator);
            }
        }
    }
}