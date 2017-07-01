using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using NoAdsHere.Commands.Penalties;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;

namespace NoAdsHere.Commands.Masters
{
    [Name("Master")]
    public class MasterModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public MasterModule(MongoClient mongo)
        {
            _mongo = mongo;
        }

        [Command("Reset Guild")]
        [RequirePermission(AccessLevel.Master)]
        public async Task Reset(ulong guildId)
        {
            var guild = await Context.Client.GetGuildAsync(guildId);
            if (guild != null)
            {
                var collection = _mongo.GetCollection<Penalty>(Context.Client);
                await collection.DeleteManyAsync(f => f.GuildId == guildId);
                await PenaltyModule.Restore(_mongo, Context.Client as DiscordShardedClient, guild as SocketGuild);
                await ReplyAsync($"Guild {guild} has been reset to default penalties");
            }          
        }
    }
}