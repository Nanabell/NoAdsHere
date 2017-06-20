using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Commands.Penalties;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;

namespace NoAdsHere.Commands.BotOwner
{
    [Name("Bot Owner")]
    public class BotOwnerModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public BotOwnerModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Shutdown")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("*Shutting down..*");
            var client = Context.Client as DiscordSocketClient;
            if (client != null)
            {
                var _ = StopAsync(client);
            }
        }

        private static async Task StopAsync(DiscordSocketClient client)
        {
            await CommandHandler.StopHandler();
            
            await client.LogoutAsync();
            await client.StopAsync();
            var logger = LogManager.GetLogger("Discord");
            logger.Info("Bot was shut down via command.");
            Environment.Exit(0);
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

        [Command("Add Master")]
        [RequireOwner]
        public async Task Add_Master(IUser user)
        {
            var newMaster = new Master(user.Id);
            var collection = _mongo.GetCollection<Master>(Context.Client);

            var masters = await collection.GetMastersAsync();

            if (masters.All(m => m.UserId != user.Id))
            {
                await collection.InsertOneAsync(newMaster);
                await ReplyAsync($"{user} added to global Masters!");
            }
            else
            {
                await ReplyAsync($"{user} is already a Master.");
            }
            
        }

        [Command("Remove Master")]
        [RequireOwner]
        public async Task Remove_Master(IUser user)
        {
            var collection = _mongo.GetCollection<Master>(Context.Client);
            var masters = await collection.GetMastersAsync();

            if (masters.Any(m => m.UserId == user.Id))
            {
                await collection.DeleteAsync(masters.First(m => m.UserId == user.Id));
                await ReplyAsync($"{user} removed from global Masters!");
            }
            else
            {
                await ReplyAsync($"{user} is not a Master.");
            }
        }
    }
}