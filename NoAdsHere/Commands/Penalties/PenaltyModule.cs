using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using Discord.WebSocket;
using System.Collections.Generic;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;

namespace NoAdsHere.Commands.Penalties
{
    [Name("Penalties"), Alias("Penalty"), Group("Penalties")]
    public class PenaltyModule : ModuleBase
    {
        private readonly MongoClient _mongo;
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public PenaltyModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string type, int at, bool autoDelete = false, [Remainder]string message = null)
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var allpenalties = await collection.GetPenaltiesAsync(Context.Guild.Id);
            var penaltyCount = allpenalties.Count;

            var penatlyType = PenaltyParser(type.ToLower());

            var newPenalty = new Penalty(Context.Guild.Id, penaltyCount + 1, penatlyType, at, message, autoDelete);
            await collection.InsertOneAsync(newPenalty);

            if (at == 0)
                await ReplyAsync($":white_check_mark: Penalty {type}`(ID: {penaltyCount + 1})` has been added but is disabled. :white_check_mark:");
            else
                await ReplyAsync($":white_check_mark: Penalty {type}`(id: {penaltyCount + 1})` has been added with {at} required points :white_check_mark:");
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(int penaltyId)
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var penalty = await collection.GetPenaltyAsync(Context.Guild.Id, penaltyId);

            if (penalty != null)
            {
                await collection.DeleteAsync(penalty);
                await ReplyAsync($":white_check_mark: Penalty  {penalty.PenaltyType}`({penalty.PenaltyId})` removed :white_check_mark:");
            }
            else
            {
                await ReplyAsync($"Penalty with ID {penaltyId} does not exist!");
            }
        }

        [Command("List")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task List()
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var penalties = await collection.GetPenaltiesAsync(Context.Guild.Id);

            var sb = new StringBuilder();
            sb.AppendLine("```");
            foreach (var penalty in penalties.OrderBy(o => o.PenaltyId))
            {
                sb.AppendLine($"{penalty.PenaltyId.ToString().PadRight(2)}: {penalty.PenaltyType.ToString().PadRight(11)} @ {penalty.RequiredPoints} Points | {penalty.Message ?? "Default Message"}");
            }
            sb.AppendLine("```");

            await ReplyAsync(sb.ToString());
        }

        [Command("Restore Default")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Default()
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var penalties = await collection.GetPenaltiesAsync(Context.Guild.Id);

            foreach (var penalty in penalties)
            {
                await collection.DeleteAsync(penalty);
            }
            await Restore(_mongo, Context.Client as DiscordShardedClient, Context.Guild as SocketGuild).ConfigureAwait(false);

            await ReplyAsync("Penalties have been restored to default.");
        }

        private static PenaltyType PenaltyParser(string type)
        {
            switch (type)
            {
                case "nothing":
                case "info":
                    return PenaltyType.Nothing;

                case "warn":
                    return PenaltyType.Warn;

                case "kick":
                    return PenaltyType.Kick;

                case "ban":
                    return PenaltyType.Ban;

                default:
                    return PenaltyType.Nothing;
            }
        }

        public static async Task Restore(MongoClient mongo, IDiscordClient client, SocketGuild guild)
        {
            var collection = mongo.GetCollection<Penalty>(client);
            var penalties = await collection.GetPenaltiesAsync(guild.Id);
            var newPenalties = new List<Penalty>();

            if (penalties.All(p => p.PenaltyId != 1))
                newPenalties.Add(new Penalty(guild.Id, 1, PenaltyType.Nothing, 1, autoDelete: true));
            if (penalties.All(p => p.PenaltyId != 2))
                newPenalties.Add(new Penalty(guild.Id, 2, PenaltyType.Warn, 3, autoDelete: true));
            if (penalties.All(p => p.PenaltyId != 3))
                newPenalties.Add(new Penalty(guild.Id, 3, PenaltyType.Kick, 5, autoDelete: true));
            if (penalties.All(p => p.PenaltyId != 4))
                newPenalties.Add(new Penalty(guild.Id, 4, PenaltyType.Ban, 6, autoDelete: true));

            if (newPenalties.Any())
            {
                await collection.InsertManyAsync(newPenalties);
                Logger.Info($"Added default penalties to guild {guild}");
            }
        }
    }
}