using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Database.Models.Violator;

namespace NoAdsHere.Commands.Ungrouped
{
    [Name("Not Grouped")]
    public class UngroupedModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public UngroupedModule(MongoClient mongo)
        {
            _mongo = mongo;
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
            var violator = await _mongo.GetCollection<Violator>(Context.Client).GetUserAsync(Context.User as IGuildUser);
            var penalties = await _mongo.GetCollection<Penalty>(Context.Client).GetPenaltiesAsync(Context.Guild.Id);
            var nextPenalty = penalties.OrderBy(p => p.RequiredPoints).FirstOrDefault(penalty => penalty.RequiredPoints > violator.Points);

            var until = TimeSpan.Zero;
            if (violator.Points > 0)
                until = violator.LatestViolation.AddHours(12) - DateTime.Now;
            await ReplyAsync(
                // ReSharper disable once UseFormatSpecifierInInterpolation
                $"You currently have {violator.Points} points. {(until != TimeSpan.Zero ? $"You will lose one point in {until.ToString(@"hh'h'\:mm'm'\:ss's'")}" : "")}" +
                $"{(nextPenalty != null ? $"\nThe next Penalty*({nextPenalty.PenaltyType})* is at {nextPenalty.RequiredPoints} points" : "")}");
        }
    }
}