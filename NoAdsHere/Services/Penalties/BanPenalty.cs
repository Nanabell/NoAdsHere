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

        public static async Task BanAsync(ICommandContext context, string message, string trigger, Emote emote = null, bool autoDelete = false)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.BanMembers)
            {
                try
                {
                    await context.Guild.AddBanAsync(context.User);
                    IUserMessage msg;
                    if (emote == null) emote = Emote.Parse("<:Ban:330793436309487626>");
                    if (self.GuildPermissions.UseExternalEmojis && emote != null)
                        msg = await context.Channel.SendMessageAsync($"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
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