using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class BanPenalty
    {
        public static async Task BanAsync(ILoggerFactory factory, ICommandContext context, string message, string trigger, Emote emote = null, bool autoDelete = false)
        {
            var logger = factory.CreateLogger(typeof(BanPenalty));

            var self = await context.Guild.GetCurrentUserAsync();

            if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages, self))
            {
                if (self.GuildPermissions.BanMembers)
                {
                    try
                    {
                        await context.Guild.AddBanAsync(context.User, 1, $"Banned for Advertisement in {context.Channel}. Trigger: {trigger}");
                        IUserMessage msg;
                        if (emote == null) emote = Emote.Parse("<:Ban:330793436309487626>");
                        if (self.GuildPermissions.UseExternalEmojis && emote != null)
                            msg = await context.Channel.SendMessageAsync($"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
                        else
                            msg = await context.Channel.SendMessageAsync($":no_entry: {context.User.Mention} {message}! Trigger: {trigger} :no_entry:");
                        logger.LogInformation(new EventId(200), $"{context.User} has been banned from {context.Guild}");

                        if (msg != null)
                        {
                            if (autoDelete)
                            {
                                await JobQueue.QueueTrigger(msg, logger);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
                        {
                            var msg = await context.Channel.SendMessageAsync(
                                $":anger: Unable to Ban {context.User}.\n`{e.Message}`");
                            await JobQueue.QueueTrigger(msg, logger);
                        }
                        else
                        {
                            logger.LogWarning(new EventId(403), $"Unable to send Ban penalty message in {context.Guild}/{context.Channel} missing permissions!");
                        }
                        logger.LogWarning(new EventId(400), e, $"Unable to Ban {context.User} from {context.Guild}");
                    }
                }
                else
                {
                    if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
                    {
                        var msg = await context.Channel.SendMessageAsync(
                            $":anger: Unable to Ban {context.User}.\n`Missing Ban Permission`");
                        await JobQueue.QueueTrigger(msg, logger);
                    }
                    else
                    {
                        logger.LogWarning(new EventId(403), $"Unable to send Ban penalty message in {context.Guild}/{context.Channel} missing permissions!");
                    }
                    logger.LogWarning(new EventId(403), $"Unable to ban {context.User} from {context.Guild}, not enoguh permissions to ban");
                }
            }
        }
    }
}