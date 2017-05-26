using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Services;
using Discord.WebSocket;
using System.Collections.Generic;
using NLog;

namespace NoAdsHere.Commands.Penalties
{
    [Name("Penalties"), Group("Penalties")]
    public class PenaltiesModule : ModuleBase
    {
        private readonly MongoClient _mongo;
        private readonly Logger _logger = LogManager.GetLogger("AntiAds");

        public PenaltiesModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Add")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Add(string type, int at, [Remainder]string message = null)
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var allpenalties = await collection.GetPenaltiesAsync(Context.Guild.Id);
            var penaltyCount = allpenalties.Count;

            var penatlyType = PenaltyParser(type.ToLower());

            var newPenalty = new Penalty(Context.Guild.Id, penaltyCount + 1, penatlyType, at, message);
            await collection.InsertOneAsync(newPenalty);

            if (at == 0)
                await ReplyAsync($":white_check_mark: Penalty  {type}`(id: {penaltyCount + 1})` added but Disabled :white_check_mark:");
            else
                await ReplyAsync($":white_check_mark: Penalty  {type}`(id: {penaltyCount + 1})` added with {at} Required Points :white_check_mark:");
        }

        [Command("Remove")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
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
                await ReplyAsync($"Penalty with id {penaltyId} not existent!");
            }
        }

        [Command("List")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
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

        [Command("Resotre Default")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Default()
        {
            var collection = _mongo.GetCollection<Penalty>(Context.Client);
            var penalties = await collection.GetPenaltiesAsync(Context.Guild.Id);

            foreach (var penalty in penalties)
            {
                await collection.DeleteAsync(penalty);
            }
            await Restore(Context.Client as DiscordSocketClient, Context.Guild as SocketGuild);

            await ReplyAsync("Penalties have been restored to default");
        }

        private static PenaltyTypes PenaltyParser(string type)
        {
            switch (type)
            {
                case "info":
                    return PenaltyTypes.InfoMessage;

                case "warn":
                    return PenaltyTypes.WarnMessage;

                case "kick":
                    return PenaltyTypes.Kick;

                case "ban":
                    return PenaltyTypes.Ban;

                default:
                    return PenaltyTypes.Nothing;
            }
        }

        private async Task Restore(DiscordSocketClient client, SocketGuild guild)
        {
            var collection = _mongo.GetCollection<Penalty>(client);
            var penalties = await collection.GetPenaltiesAsync(guild.Id);
            var newPenalties = new List<Penalty>();

            if (penalties.All(p => p.PenaltyId != 1))
            {
                newPenalties.Add(new Penalty(guild.Id, 1, PenaltyTypes.InfoMessage, 1));
                _logger.Info("Adding default InfoMessage Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 2))
            {
                newPenalties.Add(new Penalty(guild.Id, 2, PenaltyTypes.WarnMessage, 3));
                _logger.Info("Adding default WarnMessage Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 3))
            {
                newPenalties.Add(new Penalty(guild.Id, 3, PenaltyTypes.Kick, 5));
                _logger.Info("Adding default Kick Penalty");
            }
            if (penalties.All(p => p.PenaltyId != 4))
            {
                newPenalties.Add(new Penalty(guild.Id, 4, PenaltyTypes.Ban, 6));
                _logger.Info("Adding default Ban Penalty");
            }

            if (newPenalties.Any())
                await collection.InsertManyAsync(newPenalties);
        }
    }
}