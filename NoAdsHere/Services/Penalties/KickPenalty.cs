using Discord;
using Discord.Commands;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Penalties
{
    public static class KickPenalty
    {
        private static Logger _logger = LogManager.GetLogger("AntiAds");

        public static async Task KickAsync(ICommandContext context)
        {
            var self = await context.Guild.GetCurrentUserAsync();

            if (self.GuildPermissions.KickMembers)
            {
                try
                {
                    await (context.User as IGuildUser).KickAsync();
                    await context.Channel.SendMessageAsync($":boot: {context.User.Mention} has been kicked for Advertisement :boot:");
                    _logger.Info($"{context.User} has been kicked from {context.Guild.Id}");
                }
                catch (Exception e)
                {
                    _logger.Warn($"Unable to kick {context.User}. {e.Message}");
                }
            }
        }
    }
}