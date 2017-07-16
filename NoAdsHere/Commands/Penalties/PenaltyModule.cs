using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NoAdsHere.Database.Models.GuildSettings;
using System.Collections.Generic;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Commands.Penalties
{
    [Name("Penalties"), Alias("Penalty"), Group("Penalties")]
    public class PenaltyModule : ModuleBase
    {
        private readonly DatabaseService _database;
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public PenaltyModule(DatabaseService database)
        {
            _database = database;
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(PenaltyType type, int at, bool autoDelete = false, [Remainder]string message = null)
        {
            var allpenalties = await _database.GetPenaltiesAsync(Context.Guild.Id);
            var penaltyCount = allpenalties.Count;

            var newPenalty = new Penalty(Context.Guild.Id, penaltyCount + 1, type, at, message, autoDelete);
            await _database.InsertOneAsync(newPenalty);

            if (at == 0)
                await ReplyAsync($":white_check_mark: Penalty {type}`(ID: {penaltyCount + 1})` has been added but is disabled. :white_check_mark:");
            else
                await ReplyAsync($":white_check_mark: Penalty {type}`(id: {penaltyCount + 1})` has been added with {at} required points :white_check_mark:");
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(int penaltyId)
        {
            var penalty = await _database.GetPenaltyAsync(Context.Guild.Id, penaltyId);

            if (penalty != null)
            {
                await penalty.DeleteAsync();
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
            var penalties = await _database.GetPenaltiesAsync(Context.Guild.Id);

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
            var penalties = await _database.GetPenaltiesAsync(Context.Guild.Id);

            foreach (var penalty in penalties)
            {
                await penalty.DeleteAsync();
            }
            await Restore(_database, Context.Client, Context.Guild).ConfigureAwait(false);

            await ReplyAsync("Penalties have been restored to default.");
        }

        public static async Task Restore(DatabaseService database, IDiscordClient client, IGuild guild)
        {
            var penalties = await database.GetPenaltiesAsync(guild.Id);
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
                await database.InsertManyAsync(newPenalties);
                Logger.Info($"Added default penalties to guild {guild}");
            }
        }
    }
}