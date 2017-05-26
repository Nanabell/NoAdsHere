using Discord;
using Discord.Commands;
using NLog;
using NoAdsHere.Common;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class InfoMessagePenalty
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");

        public static async Task SendAsync(ICommandContext context, string message = null)
        {
            if (context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, await context.Guild.GetCurrentUserAsync()))
                await context.Channel.SendMessageAsync($":no_entry_sign: {context.User.Mention} {message ?? "Advertisement is not allowed in this server!"} :no_entry_sign:");
            else Logger.Warn("Unable to send InfoMessage due to missing Permission");
        }
    }
}