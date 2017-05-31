using Discord;
using Discord.Commands;
using NLog;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class BanPenalty
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static async Task BanAsync(ICommandContext context, string message, string trigger, string emote = "<:banzy:316314495695716352>", bool autoDelete = false)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.BanMembers)
            {
                try
                {
                    await context.Guild.AddBanAsync(context.User);
                    IUserMessage msg = null;
                    if (self.GuildPermissions.UseExternalEmojis)
                        msg = await context.Channel.SendMessageAsync($"<:banzy:316314495695716352> {context.User.Mention} {message}! Trigger: {trigger} <:banzy:316314495695716352>");
                    else
                        msg = await context.Channel.SendMessageAsync($":no_entry: {context.User.Mention} {message}! Trigger: {trigger} :no_entry:");
                    Logger.Info($"{context.User} has been banned from {context.Guild.Id}");

                    if (msg != null)
                    {
                        if (autoDelete)
                        {
                            var _ = Task.Run(async () =>
                            {
                                await Task.Delay(7000);
                                await msg.DeleteAsync();
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn($"Unable to ban {context.User}. {e.Message}");
                }
            }
        }
    }
}