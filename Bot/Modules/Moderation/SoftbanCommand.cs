﻿using System.Threading.Tasks;
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
    [RequireAccessLevel(AccessLevel.Moderator)]
    public class SoftbanCommand : CentralModuleBase
    {
        [Command("Softban")]
        public async Task SoftBan(SocketGuildUser target, [Remainder] string reason = null)
            => await SoftBan(Context, target.Id, reason);

        [Command("Softban")]
        public async Task Softban(ulong targetId, [Remainder] string reason = null)
            => await SoftBan(Context, targetId, reason);

        private async Task SoftBan(SocketCommandContext context, ulong targetId, string reason)
        {
            var target = context.Guild.GetUser(targetId);
            var targetHierarchy = 0;
            if (target != null)
                targetHierarchy = target.Hierarchy;
            
            var invoker = (SocketGuildUser) context.User;
            var self = context.Guild.CurrentUser;

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
                        await context.Guild.AddBanAsync(targetId, 7, $"{invoker}: " + reason);
                        await context.Guild.RemoveBanAsync(target);
                        await ReplyAsync(
                            $"***{Format.Sanitize(target?.ToString() ?? targetId.ToString())}*** has been Softbanned!");
                    }
                    catch (HttpException ex)
                    {
                        await ReplyAsync(
                            $"Failed to Softban ***{Format.Sanitize(target?.ToString() ?? targetId.ToString())}***\n"
                            + ex.Message);
                    }
                }
                else
                {
                    await SendMessageWithEmotesAsync("{0} "
                        + $"I don't have enough permission to Softban {Format.Italics(Format.Sanitize(target?.ToString() ?? targetId.ToString()))}",
                        new object[] {"<:ThisIsFine:356157243923628043>"},
                        new object[] {":x:"});
                }
            }
            else
            {
                await SendMessageWithEmotesAsync("{0} "
                    + $"You don't have enough permission to Softban {Format.Italics(Format.Sanitize(target?.ToString() ?? targetId.ToString()))}",
                    new object[] {"<:ThisIsFine:356157243923628043>"},
                    new object[] {":x:"});
            }
        }
    }
}