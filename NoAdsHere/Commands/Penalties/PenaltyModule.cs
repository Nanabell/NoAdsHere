using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Penalties
{
    [Name("Penalties"), Alias("Penalty"), Group("Penalties")]
    public class PenaltyModule : ModuleBase
    {
        private readonly IUnitOfWork _unit;
        private static ILogger _logger;

        public PenaltyModule(IUnitOfWork unit, ILoggerFactory factory)
        {
            _unit = unit;
            _logger = factory.CreateLogger<PenaltyModule>();
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(PenaltyType type, int at, bool autoDelete = false, [Remainder]string message = null)
        {
            var penalty = new Penalty(Context.Guild, type, at, message, autoDelete);
            _unit.Penalties.Add(penalty);
            _unit.SaveChanges();

            if (at == 0)
                await ReplyAsync($":white_check_mark: Penalty {type} has been added but is disabled. :white_check_mark:");
            else
                await ReplyAsync($":white_check_mark: Penalty {type} has been added with {at} required points :white_check_mark:");
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(int penaltyId)
        {
            var penalty = await _unit.Penalties.GetAsync(penaltyId);

            if (penalty != null)
            {
                if (penalty.GuildId == Context.Guild.Id)
                {
                    _unit.Penalties.Remove(penalty);
                    _unit.SaveChanges();
                    await ReplyAsync($":white_check_mark: Penalty {penalty.PenaltyType} `ID: ({penalty.Id})` removed :white_check_mark:");
                }
                else
                {
                    await ReplyAsync($":anger: You cannot delete a penalty which doesn't belong to your guild!");
                }
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
            var penalties = await _unit.Penalties.GetAllAsync(Context.Guild);

            var sb = new StringBuilder();
            sb.AppendLine("```");
            foreach (var penalty in penalties.OrderBy(p => p.RequiredPoints))
            {
                sb.AppendLine($"{penalty.Id.ToString().PadRight(4)}: {penalty.PenaltyType.ToString().PadRight(11)} @ {penalty.RequiredPoints} Points | {penalty.Message ?? "Default Message"}");
            }
            sb.AppendLine("```");

            await ReplyAsync(sb.ToString());
        }

        [Command("Restore Default")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Default()
        {
            await Restore(_unit, Context.Guild).ConfigureAwait(false);
            await ReplyAsync("Penalties have been restored to default.");
        }

        public static async Task Restore(IUnitOfWork unit, IGuild guild)
        {
            var penalties = await unit.Penalties.GetAllAsync(guild);
            if (penalties.Any())
                unit.Penalties.RemoveRange(penalties);

            var penaltyList = new List<Penalty>
            {
                new Penalty(guild, PenaltyType.Nothing, 1, autoDelete: true),
                new Penalty(guild, PenaltyType.Warn, 3, autoDelete: true),
                new Penalty(guild, PenaltyType.Kick, 5, autoDelete: true),
                new Penalty(guild, PenaltyType.Ban, 6, autoDelete: true)
            };
            await unit.Penalties.AddRangeAsync(penaltyList);

            var changes = unit.SaveChanges();
            _logger.LogDebug(new EventId(200), $"Sucessfully saved {changes} changes to database");

            if (changes > 0)
                _logger.LogInformation(new EventId(200), $"Added default penalties to guild {guild}");
        }
    }
}