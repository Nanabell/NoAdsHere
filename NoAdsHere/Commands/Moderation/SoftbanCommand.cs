using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using Discord.WebSocket;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Moderation
{
    [Name("Moderation"), Alias("", "Mod"), Group("Moderation")]
    public class SoftbanCommand : ModuleBase<SocketCommandContext>
    {
        [Command("Softban", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.HighModerator)]
        [RequireBotPermission(GuildPermission.BanMembers), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task SoftBanAsync(SocketGuildUser target, string argument = "", [Remainder] string reason = null)
        {
            if (target.Id == Context.Client.CurrentUser.Id || target.Id == Context.User.Id)
            {
                await ReplyAsync(Context.Guild.CurrentUser.GuildPermissions.UseExternalEmojis ? "<:ThisIsFine:356157243923628043>" : ":x:");
                return;
            }

            if (Context.User is SocketGuildUser invoker && invoker.Hierarchy > target.Hierarchy)
            {
                if (Context.Guild.CurrentUser.Hierarchy > target.Hierarchy)
                {
                    if (argument.ToLower() != "-f")
                    {
                        var interactive = new InteractiveService(Context.Client);
                        await ReplyAsync(
                            $"Are you sure you want to Softban **{target}`({target.Id})`**?" +
                            $"\n`-f` to skip this.");

                        var response = await interactive.WaitForMessage(Context.User, Context.Channel,
                            TimeSpan.FromSeconds(30));

                        var responseResults = new List<string> { "yes", "y" };
                        if (responseResults.All(res => !response.Content.ToLower().Contains(res)))
                        {
                            await ReplyAsync($"**{target}`({target.Id})`** was not softbanned!");
                            return;
                        }
                    }

                    var banRason = argument == "-f" ? reason : $"{argument} {reason}";
                    await Context.Guild.AddBanAsync(target, 7,
                        !string.IsNullOrWhiteSpace(banRason) ? banRason : "No reason specified!");
                    await Context.Guild.RemoveBanAsync(target);

                    await ReplyAsync($"**{target}`({target.Id})`** was softbanned!");
                }
                else
                {
                    await ReplyAsync(":warning: Im not permitted to ban this user!");
                }
            }
            else
            {
                await ReplyAsync($":warning: You are not permitted to ban this user!");
            }
        }
    }
}