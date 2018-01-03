using Discord.Commands;
using NoAdsHere.Commands.Penalties;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NoAdsHere.Commands.Masters
{
    [Name("Master")]
    public class MasterModule : ModuleBase
    {
        /*
        [Command("Reset Guild")]
        [RequirePermission(AccessLevel.Master)]
        public async Task Reset(ulong guildId)
        {
            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild != null)
            {
                await _unit.Penalties.GetAllAsync(Context.Guild);
                await PenaltyModule.Restore(_unit, guild);
                await ReplyAsync($"Guild {guild} has been reset to default penalties");
            }
            else
            {
                await ReplyAsync($"Guild with the ID `{guildId}` not found!");
            }
        }
        */
    }
}