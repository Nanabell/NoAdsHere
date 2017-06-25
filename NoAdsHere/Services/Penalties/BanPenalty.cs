using Discord;
using Discord.Commands;
using NLog;
using Quartz;
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
                    IUserMessage msg;
                    if (self.GuildPermissions.UseExternalEmojis)
                        msg = await context.Channel.SendMessageAsync($"<:banzy:316314495695716352> {context.User.Mention} {message}! Trigger: {trigger} <:banzy:316314495695716352>");
                    else
                        msg = await context.Channel.SendMessageAsync($":no_entry: {context.User.Mention} {message}! Trigger: {trigger} :no_entry:");
                    Logger.Info($"{context.User} has been banned from {context.Guild}");

                    if (msg != null)
                    {
                        if (autoDelete)
                        {
                            await JobQueue.QueueTrigger(msg);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Unable to ban {context.User} from {context.Guild}");
                }
            }
            else
            {
                Logger.Warn($"Unable to ban {context.User} from {context.Guild}, not enoguh permissions to ban");
            }
        }
    }
}