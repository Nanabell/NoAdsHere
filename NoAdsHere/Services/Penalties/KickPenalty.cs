using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class KickPenalty
    {
        public static async Task KickAsync(ILoggerFactory factory, ICommandContext context, string message, string trigger, Emote emote = null, bool autoDelete = false)
        {
            var logger = factory.CreateLogger(typeof(KickPenalty));
            var self = await context.Guild.GetCurrentUserAsync();

            if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages, self))
            {
                if (self.GuildPermissions.KickMembers)
                {
                    try
                    {
                        await ((IGuildUser)context.User).KickAsync($"Kicked for Advertisement in {context.Channel}. Trigger: {trigger}");
                        IUserMessage msg;
                        if (emote == null) emote = Emote.Parse("<:Kick:330793607919566852>");
                        if (self.GuildPermissions.UseExternalEmojis && emote != null)
                            msg = await context.Channel.SendMessageAsync($"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
                        else
                            msg = await context.Channel.SendMessageAsync($":boot: {context.User.Mention} {message}! Trigger: {trigger} :boot:");
                        logger.LogInformation(new EventId(200), $"{context.User} has been kicked from {context.Guild}.");

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
                                $":anger: Unable to kick {context.User}.\n`{e.Message}`");
                            await JobQueue.QueueTrigger(msg, logger);
                        }
                        else
                        {
                            logger.LogWarning(new EventId(403), $"Unable to send Kick penalty message in {context.Guild}/{context.Channel} missing permissions!");
                        }
                        logger.LogWarning(new EventId(400), e, $"Unable to kick {context.User} from {context.Guild}");
                    }
                }
                else
                {
                    if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
                    {
                        var msg = await context.Channel.SendMessageAsync(
                            $":anger: Unable to kick {context.User}.\n`Missing Kick Permission`");
                        await JobQueue.QueueTrigger(msg, logger);
                    }
                    else
                    {
                        logger.LogWarning(new EventId(403), $"Unable to send Kick penalty message in {context.Guild}/{context.Channel} missing permissions!");
                    }
                    logger.LogWarning(new EventId(403), $"Unable to kick {context.User} from {context.Guild}, not enough permissions to kick");
                }
            }
        }
    }
}