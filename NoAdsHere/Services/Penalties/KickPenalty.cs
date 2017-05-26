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

        public static async Task KickAsync(ICommandContext context, string message = null)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.KickMembers)
            {
                try
                {
                    await ((IGuildUser) context.User).KickAsync();
                    await context.Channel.SendMessageAsync($":boot: {context.User.Mention} {message ?? "has been kicked for Advertisement"} :boot:");
                    Logger.Info($"{context.User} has been kicked from {context.Guild.Id}");
                }
                catch (Exception e)
                {
                    Logger.Warn($"Unable to kick {context.User}. {e.Message}");
                }
            }
        }
    }
}