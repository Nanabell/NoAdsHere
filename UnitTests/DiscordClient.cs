using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NoAdsHere.Services.Configuration;

namespace UnitTests
{
    public static class DiscordClient
    {
        private static bool _isLoggedIn;
        private static bool _ready;
        private static readonly DiscordShardedClient Client = new DiscordShardedClient();

        public static async Task<DiscordShardedClient> GetClientAsync()
        {
            if (!_isLoggedIn)
                await LogInAsync();

            while (!_ready)
                await Task.Delay(25);
            return Client;
        }

        private static async Task LogInAsync()
        {
            _isLoggedIn = true;
            await Client.LoginAsync(TokenType.Bot, GetToken());
            await Client.StartAsync();

            Client.Shards.First().Ready += Ready;

            Task Ready()
            {
                Client.Shards.First().Ready += Ready;
                _ready = true;
                return Task.CompletedTask;
            };

            while (!_ready)
                await Task.Delay(25);
        }

        private static string GetToken()
        {
            var vars = Environment.GetEnvironmentVariables();

            return vars.Contains("APPVEYOR") ? Environment.GetEnvironmentVariable("BOT_TOKEN") : Config.Load().Token;
        }
    }
}