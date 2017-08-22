using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NoAdsHere.Services.Events;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Services.LogService
{
    public class LogChannelService
    {
        private bool IsEnabled;
        private readonly DiscordWebhookClient _client;

        public LogChannelService(IConfigurationRoot config)
        {
            if (config["webhook:token"] == null || Convert.ToUInt64(config["webhook:id"]) == 0)
                IsEnabled = false;
            else IsEnabled = true;

            _client = new DiscordWebhookClient(Convert.ToUInt64(config["webhook:id"]), config["webhook:token"]);
            _client.Log += EventHandlers.WebhookLogger;
        }

        internal async Task SendMessageAsync(string message)
        {
            if (IsEnabled)
                await _client.SendMessageAsync(message);
        }

        internal async Task LogMessageAsync(DiscordShardedClient client, DiscordSocketClient shard, Emote emote,
            string message)
        {
            if (IsEnabled)
                await _client.SendMessageAsync($"`[{shard.ShardId:00} / {client.Shards.Count:00}]` " +
                    $"<:{emote.Name}:{emote.Id}> " +
                    $"{message}", false, null, shard.CurrentUser.Username,
                    shard.CurrentUser.GetAvatarUrl(ImageFormat.Png));
        }
    }
}