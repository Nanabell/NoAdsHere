using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Bot.Common
{
    public class CentralModuleBase : ModuleBase<SocketCommandContext>
    {
        protected async Task<IUserMessage> SendMessageWithEmotesAsync(string message, object[] emotes, object[] fallbackEmojis)
        {
            var self = Context.Guild.CurrentUser;
            
            return await Context.Channel.SendMessageAsync(string.Format(message,
                self.GuildPermissions.UseExternalEmojis ? emotes : fallbackEmojis));
        }
    }
}