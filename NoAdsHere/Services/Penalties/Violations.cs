using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
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

        public static async Task AddPoint(ICommandContext context)
        {
            var violator = await _mongo.GetCollection<Violator>(_client).GetUserAsync(context.User as IGuildUser);
            await DecreasePoints(context, violator);
            await IncreasePoint(context, violator);
        }

        private static async Task IncreasePoint(ICommandContext context, Violator violator)
        {
            var collection = _mongo.GetCollection<Violator>(_client);
            violator.LatestViolation = DateTime.Now;
            violator.Points++;
            Logger.Info($"Increased points for {context.User} by 1 for a total of {violator.Points}");
            await collection.SaveAsync(violator);
            await ExecutePenalty(context, violator);
        }

        private static async Task ExecutePenalty(ICommandContext context, Violator violator)
        {
            var setting = await _mongo.GetCollection<GuildSetting>(_client).GetGuildAsync(violator.GuildId);

            if (violator.Points == setting.Penaltings.InfoMessage && setting.Penaltings.InfoMessage != 0)
            {
                Logger.Info($"{context.User} exceeded the limit ({setting.Penaltings.InfoMessage}) for InfoMessage");
                await InfoMessagePenalty.SendAsync(context);
            }
            else if (violator.Points == setting.Penaltings.WarnMessage && setting.Penaltings.WarnMessage != 0)
            {
                Logger.Info($"{context.User} exceeded the limit ({setting.Penaltings.InfoMessage}) for WarnMessage");
                await WarnMessagePenalty.SendAsync(context);
            }
            else if (violator.Points == setting.Penaltings.Kick && setting.Penaltings.Kick != 0)
            {
                Logger.Info($"{context.User} exceeded the limit ({setting.Penaltings.InfoMessage}) for Kick");
                await KickPenalty.KickAsync(context);
            }
            else if (violator.Points >= setting.Penaltings.Ban && setting.Penaltings.Ban != 0)
            {
                var collection = _mongo.GetCollection<Violator>(_client);

                Logger.Info($"{context.User} exceeded the limit ({setting.Penaltings.InfoMessage}) for Ban");
                await BanPenalty.BanAsync(context);
                await collection.DeleteAsync(violator);
                Logger.Info($"Dropped Database Entry for {context.User}");
            }
        }

        private static int CalcDecreasingPoints(Violator violator)
        {
            int decPoints = 0;
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