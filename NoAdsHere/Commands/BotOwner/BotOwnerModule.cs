using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Commands.Penalties;

namespace NoAdsHere.Commands.BotOwner
{
    public class BotOwnerModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public BotOwnerModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }


        [Command("Update")]
        [RequireOwner]
        public async Task Update()
        {
            await Task.CompletedTask;
        }

        [Command("Reset all guilds.")]
        [RequireOwner]
        public async Task Reset()
        {
            var database = _mongo.GetDatabase(Context.Client.CurrentUser.Username.Replace(" ", ""));
            database.DropCollection("Penalty");

            var counter = 0;
            foreach (var guild in await Context.Client.GetGuildsAsync())
            {
                counter++;
                await PenaltyModule.Restore(_mongo, Context.Client as DiscordSocketClient, guild as SocketGuild);

            }
            await ReplyAsync($"{counter} guilds have been reset.");
        
        }
    }
}