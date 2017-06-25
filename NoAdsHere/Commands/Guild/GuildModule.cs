using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Database.Models.Violator;

namespace NoAdsHere.Commands.Guild
{
    [Name("Guild"), Group("Guild")]
    public class GuildModule : ModuleBase
    {
        private readonly InteractiveService _interactiveService;
        private readonly MongoClient _mongo;

        public GuildModule(InteractiveService interactiveService, MongoClient mongo)
        {
            if (interactiveService != null) _interactiveService = interactiveService;
            if (mongo != null) _mongo = mongo;
        }

        [Command("Statistics"), Alias("Stats")]
        [RequirePermission(AccessLevel.User)]
        public async Task Stats()
        {
            var stats = await _mongo.GetCollection<Stats>(Context.Client).GetGuildStatsAsync(Context.Guild);
            var allstats = await _mongo.GetCollection<Stats>(Context.Client).GetAllStatsAsync();
            var embed = new EmbedBuilder
            {
                Title = "NoAdsHere Statistics",
                Description = "**This Guild:**\n",
                Color = new Color(0xad0a0a)
            };
            embed.Description += $"Blocked Advertisements: {stats.Blocks}\n";
            embed.Description += $"Total Warns: {stats.Warns}\n";
            embed.Description += $"Total Kicks: {stats.Kicks}\n";
            embed.Description += $"Total Bans: {stats.Bans}\n\n";
            embed.Description += "**Global:**\n";
            embed.Description += $"Total Blocks: {allstats.Sum(s => s.Blocks)}\n";
            embed.Description += $"Total Warns: {allstats.Sum(s => s.Warns)}\n";
            embed.Description += $"Total Kicks: {allstats.Sum(s => s.Kicks)}\n";
            embed.Description += $"Total Bans: {allstats.Sum(s => s.Bans)}\n";
            embed.WithFooter(footer => footer.Text = $"Uptime: {GetUptime()}");

            await ReplyAsync("", embed: embed);
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        
        [Command("Reset Points", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.Moderator)]
        public async Task Reset_Points()
        {
            var violators = await _mongo.GetCollection<Violator>(Context.Client).GetAllByGuildAsync(Context.Guild.Id);
            await ReplyAsync(
                $"Are you sure you want to reset all points for all Users in {Context.Guild} ? *({violators.Count} total)*\n**Yes** - confirm\n**No** - cancel\n**30 sec timeout**");
            var response = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30),
                new MessageContainsResponsePrecondition("yes", "no"));

            if (response.Content.ToLower() == "yes")
            {
                var result = await ClearPoints(Context);
                await ReplyAsync($"{result.DeletedCount} {(result.DeletedCount > 1 ? "entries" : "entry")} deleted!");
                var logger = LogManager.GetLogger("Violations");
                logger.Info($"Removed all [{result.DeletedCount}] Violator entries for {Context.Guild}");
            }
            else
            {
                await ReplyAsync("*Canceled*");
            }
        }

        [Command("Reset User")]
        [RequirePermission(AccessLevel.Moderator)]
        public async Task Reset_User(IGuildUser user)
        {
            var collection = _mongo.GetCollection<Violator>(Context.Client);
            var violator = await collection.GetUserAsync(user.GuildId, user.Id);
            await collection.DeleteAsync(violator);
            var logger = LogManager.GetLogger("Violations");
            logger.Info($"Removed {user}'s entry for {Context.Guild}");
            await ReplyAsync($"{user}'s *({violator.Points + (violator.Points > 1 ? " points" : " point")})* entry deleted.");
        }

        private async Task<DeleteResult> ClearPoints(ICommandContext context)
        {
            var collection = _mongo.GetCollection<Violator>(context.Client);
            return await collection.DeleteManyAsync(violator => violator.GuildId == context.Guild.Id);
        }
    }
}