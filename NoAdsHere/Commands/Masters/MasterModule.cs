using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using NoAdsHere.Commands.Penalties;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Commands.Masters
{
    [Name("Master")]
    public class MasterModule : ModuleBase
    {
        private readonly DatabaseService _database;

        public MasterModule(DatabaseService database)
        {
            _database = database;
        }

        [Command("Reset Guild")]
        [RequirePermission(AccessLevel.Master)]
        public async Task Reset(ulong guildId)
        {
            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild != null)
            {
                await _database.GetCollection<Penalty>().DeleteManyAsync(f => f.GuildId == guildId);
                await PenaltyModule.Restore(_database, Context.Client, guild);
                await ReplyAsync($"Guild {guild} has been reset to default penalties");
            }
        }
    }
}