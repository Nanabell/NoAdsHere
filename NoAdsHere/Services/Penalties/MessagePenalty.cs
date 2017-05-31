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

        public static async Task SendAsync(ICommandContext context, string message, string trigger, string emote = ":no_entry_sign:", bool autoDelete = false)
        {
            IUserMessage msg = null;
            if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, await context.Guild.GetCurrentUserAsync()))
                msg = await context.Channel.SendMessageAsync($"{emote} {context.User.Mention} {message}! Trigger: {trigger} {emote}");
            else Logger.Warn("Unable to send NothingPenalty Message due to missing Permission");

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
    }
}