using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using Discord.WebSocket;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Guild
{
    [Name("Guild"), Group("Guild")]
    public class GuildModule : ModuleBase
    {
        private readonly DiscordShardedClient _client;
        private InteractiveService _interactiveService;
        private readonly IUnitOfWork _unit;

        public GuildModule(DiscordShardedClient client, IUnitOfWork unit)
        {
            _client = client;
            _unit = unit;
        }

        [Command("Statistics"), Alias("Stats")]
        [RequirePermission(AccessLevel.User)]
        public async Task Stats()
        {
            var stats = await _unit.Statistics.GetAsync(Context.Guild);
            if (stats == null)
            {
                await ReplyAsync(":exclamation: This Guild has no Statistics yet!");
                return;
            }
            var allstats = await _unit.Statistics.GetGlobalAsync();
            if (allstats == null)
            {
                await ReplyAsync(":exclamation: There are no Statistics yet!");
                return;
            }

            var embed = new EmbedBuilder
            {
                Title = "NoAdsHere Statistics",
                Description = "**This Guild:**\n" +
                    $"Blocked Advertisements: {stats.Blocks}\n" +
                    $"Total Warns: {stats.Warns}\n" +
                    $"Total Kicks: {stats.Kicks}\n" +
                    $"Total Bans: {stats.Bans}\n\n" +
                    "**Global:**\n" +
                    $"Total Blocks: {allstats.Blocks}\n" +
                    $"Total Warns: {allstats.Warns}\n" +
                    $"Total Kicks: {allstats.Kicks}\n" +
                    $"Total Bans: {allstats.Bans}\n",
                Color = new Color(0xad0a0a),
                Footer = new EmbedFooterBuilder { Text = $"Uptime: {GetUptime()}" }
            };
            await ReplyAsync("", embed: embed.Build());
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

        [Command("Reset All", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.Moderator)]
        public async Task Reset_Points()
        {
            _interactiveService = new InteractiveService(_client.GetShardFor(Context.Guild));
            var violators = (await _unit.Violators.GetAllAsync(Context.Guild)).ToList();
            await ReplyAsync(
                $"Are you sure you want to reset all points for all Users in {Context.Guild} ? *({violators.Count} total)*" +
                "\n**Yes** - confirm" +
                "\n**No** - cancel" +
                "\n**30 sec timeout**");

            var response = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30),
                new MessageContainsResponsePrecondition("yes"));

            if (response.Content.ToLower() == "yes")
            {
                var changes = ClearPoints(Context.Guild);
                await ReplyAsync($"{changes} {(changes > 1 ? "entries" : "entry")} deleted!");
                var logger = LogManager.GetLogger("Violations");
                logger.Info($"Removed all [{changes}] Violator entries for {Context.Guild}");
                try
                {
                    await response.DeleteAsync();
                }
                catch (Exception e)
                {
                    LogManager.GetLogger("GuildModule").Warn(e, $"Failed to delete message {response.Id} by {response.Author} in {Context.Guild}/{Context.Channel}");
                }
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
            var violator = await _unit.Violators.GetAsync(user);
            _unit.Violators.Remove(violator);
            _unit.SaveChanges();
            var logger = LogManager.GetLogger("Violations");
            logger.Info($"Removed {user}'s entry for {Context.Guild}");
            await ReplyAsync($"{user}'s *({violator.Points + (violator.Points > 1 ? " points" : " point")})* entry deleted.");
        }

        private int ClearPoints(IGuild guild)
        {
            var vioaltors = _unit.Violators.GetAll(guild);
            _unit.Violators.RemoveRange(vioaltors);
            return _unit.SaveChanges();
        }
    }
}