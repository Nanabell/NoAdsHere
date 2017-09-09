using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.UnitOfWork;
using NoAdsHere.Services.Violations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Ungrouped
{
    [Name("Not Grouped")]
    public class UngroupedModule : ModuleBase
    {
        private readonly ViolationsService _violationsService;
        private readonly IConfigurationRoot _config;
        private readonly IUnitOfWork _unit;

        public UngroupedModule(ViolationsService violationsService, IConfigurationRoot config, IUnitOfWork unit)
        {
            _violationsService = violationsService;
            _config = config;
            _unit = unit;
        }

        [Command("Github")]
        [RequirePermission(AccessLevel.User)]
        public async Task Github()
        {
            await ReplyAsync(
                "You can find my source-code & Invite here: https://github.com/Nanabell/NoAdsHere\nPlease visit the Wiki tab for documentations.");
        }

        [Command("Documentation"), Alias("Docs")]
        [RequirePermission(AccessLevel.User)]
        public async Task Docs()
        {
            await ReplyAsync(
                "Documentation for NAH can be found on the Github-Wiki pages!\nhttps://github.com/Nanabell/NoAdsHere/wiki");
        }

        [Command("My Points")]
        [RequirePermission(AccessLevel.User)]
        public async Task My_Points()
        {
            var violator = await _unit.Violators.GetOrCreateAsync(Context.User as IGuildUser);
            var penalties = await _unit.Penalties.GetAllAsync(Context.Guild);

            violator = _violationsService.DecreasePoints(Context, violator);
            _unit.SaveChanges();

            var nextPenalty = penalties.OrderBy(p => p.RequiredPoints).FirstOrDefault(penalty => penalty.RequiredPoints > violator.Points);

            var until = TimeSpan.Zero;
            if (violator.Points > 0)
                until = violator.LatestViolation.AddHours(Convert.ToDouble(_config["PointDecreaseHours"])) - DateTime.UtcNow;
            await ReplyAsync(
                // ReSharper disable once UseFormatSpecifierInInterpolation
                $"You currently have {violator.Points} points. {(until != TimeSpan.Zero ? $"You will lose one point in {until.ToString(@"hh'h'\:mm'm'\:ss's'")}" : "")}" +
                $"{(nextPenalty != null ? $"\nThe next Penalty*({nextPenalty.PenaltyType})* is at {nextPenalty.RequiredPoints} points" : "")}");
        }
    }
}