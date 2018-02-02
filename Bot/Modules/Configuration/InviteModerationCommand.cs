using System.Threading.Tasks;
using Bot.Common;
using Bot.Services;
using Discord.Commands;

namespace Bot.Modules.Configuration
{
    public class InviteModerationCommand : CentralModuleBase
    {
        private readonly InviteModeration _inviteProtector;

        public InviteModerationCommand(InviteModeration inviteProtector)
        {
            _inviteProtector = inviteProtector;
        }

        [Command("InviteModeration"), Alias("InvModeration", "InvMod")]
        public async Task InviteModeration(bool setting)
        {
            switch (setting)
            {
                case true:
                    await _inviteProtector.AddGuildAsync(Context.Guild);
                    await ReplyAsync("Invite Moderation has been enabled!");
                    break;
                    
                default:
                    await _inviteProtector.RemoveGuildAsync(Context.Guild);
                    await ReplyAsync("Invite Moderation has been disabled!");
                    break;
            }
        }
    }
}