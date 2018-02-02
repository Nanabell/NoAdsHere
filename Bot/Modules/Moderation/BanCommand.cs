using System.Threading.Tasks;
using Bot.Common;
using Bot.Preconditions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

namespace Bot.Modules.Moderation
{
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireAccessLevel(AccessLevel.Admin)]
    public class BanCommand : CentralModuleBase
    {
        [Command("Ban")]
        public async Task Ban(SocketGuildUser target, [Remainder] string reason = null)
            => await BanAsync(Context, target.Id, reason);
        
        [Command("Ban")]
        public async Task Ban(ulong targetId, [Remainder] string reason = null)
            => await BanAsync(Context, targetId, reason);

        private async Task BanAsync(SocketCommandContext context, ulong targetId, string reason)
        {
            var target = context.Guild.GetUser(targetId);
            var targetHierarchy = 0;
            if (target != null)
                targetHierarchy = target.Hierarchy;
            
            var invoker = (SocketGuildUser) Context.User;
            var self = Context.Guild.CurrentUser;

            if (invoker.Id == targetId)
            {
                await SendMessageWithEmotesAsync("{0}",
                    new object[] {"<:ThisIsFine:356157243923628043>"},
                    new object[] {":x:"});
                return;
            }

            if (invoker.Hierarchy > targetHierarchy)
            {
                if (self.Hierarchy > targetHierarchy)
                {
                    try
                    {
                        await Context.Guild.AddBanAsync(targetId, 7, $"{invoker}: " + reason);
                        await ReplyAsync(
                            $"***{Format.Sanitize(target?.ToString() ?? targetId.ToString())}*** has been Banned!");
                    }
                    catch (HttpException ex)
                    {
                        await ReplyAsync(
                            $"Failed to Ban ***{Format.Sanitize(target?.ToString() ?? targetId.ToString())}***\n"
                            + ex.Message);
                    }
                }
                else
                {
                    await SendMessageWithEmotesAsync("{0} "
                        + $"I don't have enough permission to Ban {Format.Italics(Format.Sanitize(target?.ToString() ?? targetId.ToString()))}",
                        new object[] {"<:ThisIsFine:356157243923628043>"},
                        new object[] {":x:"});
                }
            }
            else
            {
                await SendMessageWithEmotesAsync("{0} "
                    + $"You don't have enough permission to Ban {Format.Italics(Format.Sanitize(target?.ToString() ?? targetId.ToString()))}",
                    new object[] {"<:ThisIsFine:356157243923628043>"},
                    new object[] {":x:"});
            }
        }
    }
}