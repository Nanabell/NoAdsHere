using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Bot
{
    public class Bot
    {
        private readonly Config _config;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public Bot(IConfiguration configuration)
        {
            _config = configuration.Get<Config>();

            Client = new DiscordShardedClient(new DiscordSocketConfig
            {
                TotalShards = _config.Shards,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 30
            });
        }

        public DiscordShardedClient Client { get; }

        public async Task StartAsync()
        {
            try
            {
                await Client.LoginAsync(TokenType.Bot, _config.Token);
            }
            catch (HttpException exception)
            {
                _logger.Fatal(exception, "Provided Token is invalid. Please provide a valid token and restart!");
                Environment.Exit(0);
            }
            await Client.StartAsync();
            _logger.Info("Started Discord Bot with {0} shard/s ", _config.Shards);
        }

        public async Task StopAsync()
        {
            await Client.StopAsync();
            await Client.LogoutAsync();
            Client.Dispose();
        }
    }
}