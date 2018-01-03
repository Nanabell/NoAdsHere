using System.Threading.Tasks;
using Bot.Common;
using Bot.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot.Modules.Moderation
{
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireAccessLevel(AccessLevel.Moderator)]
    public class SoftbanCommand : ModuleBase<SocketCommandContext>
    {
        private static bool _canUseEmotes;
        
        [Command("Softban")]
        public async Task SoftBan(SocketGuildUser target, [Remainder] string reason = null)
        {
            var invoker = (SocketGuildUser) Context.User;
            var self = Context.Guild.CurrentUser;

            _canUseEmotes = self.GuildPermissions.UseExternalEmojis;
            
            if (invoker.Id == target.Id)
            {
                await ReplyAsync(_canUseEmotes ? "<:ThisIsFine:356157243923628043>" : ":x:");
                return;
            }

            if (invoker.Hierarchy > target.Hierarchy)
            {
                if (self.Hierarchy > target.Hierarchy)
                {
                    
                }
                else
                {
                    await ReplyAsync(_canUseEmotes 
                        ? "<:ThisIsFine:356157243923628043>" 
                        : ":x:" +
                        $" I dont have enough permission to softban `{target}`");
                }
            }
            else
            {
                await ReplyAsync(_canUseEmotes
                    ? "<:ThisIsFine:356157243923628043>"
                    : ":x: " +
                      $" You dont have enough permission to softban `{target}`");
            }
        }
    }
}