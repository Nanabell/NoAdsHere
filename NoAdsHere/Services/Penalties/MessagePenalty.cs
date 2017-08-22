using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class MessagePenalty
    {
        private static ILogger _logger;

        public static async Task SendWithEmoteAsync(ILoggerFactory factory, ICommandContext context, string message, string trigger, Emote emote = null, bool autoDelete = false)
        {
            _logger = factory.CreateLogger(typeof(MessagePenalty));
            var self = await context.Guild.GetCurrentUserAsync();
            IUserMessage msg = null;
            if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
            {
                if (emote == null) emote = Emote.Parse("<:NoAds:330796107540201472>");
                if (self.GuildPermissions.UseExternalEmojis && emote != null)
                    msg = await context.Channel.SendMessageAsync(
                        $"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
                else
                    msg = await context.Channel.SendMessageAsync(
                        $":no_entry_sign: {context.User.Mention} {message}! Trigger: {trigger} :no_entry_sign:");
            }
            else _logger.LogWarning(new EventId(430), $"Unable to send Message penalty message in {context.Guild}/{context.Channel} missing permissions!");

            if (msg != null)
            {
                if (autoDelete)
                {
                    await JobQueue.QueueTrigger(msg, _logger);
                }
            }
        }

        public static async Task SendWithEmojiAsync(ICommandContext context, string message, string trigger, Emoji emoji = null, bool autoDelete = false)
        {
            var self = await context.Guild.GetCurrentUserAsync();
            IUserMessage msg = null;
            if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
            {
                if (emoji == null) emoji = new Emoji(":warning:");
                msg = await context.Channel.SendMessageAsync(
                    $"{emoji.Name} {context.User.Mention} {message}! Trigger: {trigger} {emoji.Name}");
            }
            else _logger.LogWarning(new EventId(403), $"Unable to send nothing penalty message in {context.Guild}/{context.Channel} missing permissions!");

            if (msg != null)
            {
                if (autoDelete)
                {
                    await JobQueue.QueueTrigger(msg, _logger);
                }
            }
        }
    }
}