using Discord;
using Discord.Commands;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class BanPenalty
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static async Task BanAsync(ICommandContext context)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.BanMembers)
            {
                try
                {
                    await context.Guild.AddBanAsync(context.User);
                    if (self.GuildPermissions.UseExternalEmojis)
                        await context.Channel.SendMessageAsync($"<:banzy:316314495695716352> {context.User.Mention} has been banned for Advertisement <:banzy:316314495695716352>");
                    else
                        await context.Channel.SendMessageAsync($":no_entry: {context.User.Mention} has been banned for Advertisement :no_entry:");
                    Logger.Info($"{context.User} has been banned from {context.Guild.Id}");
                }
                catch (Exception e)
                {
                    Logger.Warn($"Unable to ban {context.User}. {e.Message}");
                }
            }
        }
    }
}