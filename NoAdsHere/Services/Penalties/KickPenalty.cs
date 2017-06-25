using Discord;
using Discord.Commands;
using NLog;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class KickPenalty
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static async Task KickAsync(ICommandContext context, string message, string trigger, string emote = "<:discordPolice:318388314422116362>", bool autoDelete = false)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.KickMembers)
            {
                try
                {
                    await ((IGuildUser)context.User).KickAsync();
                    IUserMessage msg;
                    if (self.GuildPermissions.UseExternalEmojis)
                        msg = await context.Channel.SendMessageAsync($"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
                    else
                        msg = await context.Channel.SendMessageAsync($":boot: {context.User.Mention} {message}! Trigger: {trigger} :boot:");
                    Logger.Info($"{context.User} has been kicked from {context.Guild}.");

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
                    Logger.Warn(e, $"Unable to kick {context.User} from {context.Guild}");
                }
            }
            else
            {
                Logger.Warn($"Unable to kick {context.User} from {context.Guild}, not enough permissions to kick");
            }
        }
    }
}