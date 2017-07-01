using Discord;
using Discord.Commands;
using NLog;
using NoAdsHere.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class MessagePenalty
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static async Task SendWithEmoteAsync(ICommandContext context, string message, string trigger, Emote emote = null, bool autoDelete = false)
        {
            var self = await context.Guild.GetCurrentUserAsync();
            IUserMessage msg = null;
            if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, self))
            {
                if (emote == null) emote = Emote.Parse("<:NoAds:330796107540201472>");
                if (self.GuildPermissions.UseExternalEmojis && emote != null)
                    msg = await context.Channel.SendMessageAsync(
                        $"<:{emote}:{emote.Id}> {context.User.Mention} {message}! Trigger: {trigger} <:{emote}:{emote.Id}>");
                else
                    msg = await context.Channel.SendMessageAsync(
                        $":no_entry_sign: {context.User.Mention} {message}! Trigger: {trigger} :no_entry_sign:");
            }
            else Logger.Warn($"Unable to send nothing penalty message in {context.Guild}/{context.Channel} missing permissions!");

            if (msg != null)
            {
                if (autoDelete)
                {
                    await JobQueue.QueueTrigger(msg);
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
            else Logger.Warn($"Unable to send nothing penalty message in {context.Guild}/{context.Channel} missing permissions!");

            if (msg != null)
            {
                if (autoDelete)
                {
                    await JobQueue.QueueTrigger(msg);
                }
            }
        }
    }
}