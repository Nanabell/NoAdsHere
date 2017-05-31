using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;

namespace NoAdsHere.Commands.Master
{
    public class MasterModule : ModuleBase
    {
        [Command("Reset Guild")]
        [RequirePermission(AccessLevel.Master)]
        public async Task Reset(ulong guildId)
        {
            await Task.CompletedTask;
        }
    }
}