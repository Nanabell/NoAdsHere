using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Services.LogService
{
    public class WebhookLogService
    {
        private readonly bool _isEnabled;
        private readonly DiscordWebhookClient _webhookClient;
        private readonly DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private readonly ILogger _logger;

        public WebhookLogService(DiscordShardedClient client, IConfigurationRoot config, ILoggerFactory factory)
        {
            _client = client;
            _config = config;
            _logger = factory.CreateLogger<WebhookLogService>();

            if (config["webhook:token"] == null || Convert.ToUInt64(config["webhook:id"]) == 0)
            {
                _isEnabled = false;
                _logger.LogWarning("Disabled Webhook logging as either no Token or Client id was present");
            }
            else
            {
                _isEnabled = true;
                _logger.LogInformation("Enabled Webhook logging");
                _webhookClient = new DiscordWebhookClient(Convert.ToUInt64(config["webhook:id"]), config["webhook:token"]);
            }
        }

        private async Task LogAsync(string text, bool tts, Embed[] embeds, string username, string avatarUrl)
            => await _webhookClient.SendMessageAsync(text, tts, embeds, username, avatarUrl);

        private async Task LogMessageAsync(string message, bool tts, string username, string avatarUrl)
            => await LogAsync(message, tts, null, username, avatarUrl);

        private async Task LogEmbedsAsync(bool tts, Embed[] embeds, string username, string avatarUrl)
            => await LogAsync("", tts, embeds, username, avatarUrl);

        private async Task LogEmbedAsync(bool tts, Embed embed, string username, string avatarUrl)
            => await LogEmbedsAsync(tts, new[] { embed }, username, avatarUrl);

        private IEmote TryGetEmote(string emoteString)
        {
            try
            {
                return Emote.Parse(emoteString);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Emote {emoteString} misconfigured");
                return null;
            }
        }

        private string FormatMessage(DiscordSocketClient client, IEmote emote)
            => $"`[{client.ShardId:00} / {_client.Shards.Count:00}]` {emote} ";

        private EmbedBuilder FormatEmbed(DiscordSocketClient client, IEmote emote, EmbedBuilder embed)
        {
            embed.Title = $"{emote} `[{client.ShardId:00} / {_client.Shards.Count:00}]` ";
            embed.Timestamp = DateTimeOffset.Now;
            return embed;
        }

        internal async Task LogMessageAsync(DiscordSocketClient client, IEmote emote, string message, bool tts)
        {
            if (!_isEnabled)
                return;

            var bot = client.CurrentUser;
            await LogMessageAsync(FormatMessage(client, emote) + message, tts, bot.Username, bot.GetAvatarUrl());
        }

        internal async Task LogEmbedAsync(DiscordSocketClient client, EmbedBuilder embed, bool tts)
        {
            if (!_isEnabled)
                return;

            var bot = client.CurrentUser;
            await LogEmbedAsync(tts, embed.Build(), bot.Username, bot.GetAvatarUrl());
        }

        internal async Task LogMessageAsync(IGuild guild, IEmote emote, string message, bool tts)
            => await LogMessageAsync(_client.GetShardFor(guild), emote, message, tts);

        internal async Task LogEmbedAsync(IGuild guild, EmbedBuilder embed, bool tts)
            => await LogEmbedAsync(_client.GetShardFor(guild), embed, tts);

        internal async Task LogActionAsync(IGuild guild, string message)
        {
            var emote = TryGetEmote(_config["Emotes:Action"]);
            await LogMessageAsync(guild, emote, message, false);
        }

        internal async Task LogJoinAsync(IGuild guild)
        {
            var emote = TryGetEmote(_config["Emotes:Join"]);
            var embed = FormatEmbed(_client.GetShardFor(guild), emote, new EmbedBuilder());
            embed.Title += "Joined Guild";
            embed.Description += $"{guild} `{guild.Id}`\nUsers: {((SocketGuild)guild).MemberCount}";
            embed.Color = new Color(0x27e53d);

            await LogEmbedAsync(guild, embed, false);
        }

        internal async Task LogLeaveAsync(IGuild guild)
        {
            var emote = TryGetEmote(_config["Emotes:Leave"]);
            var embed = FormatEmbed(_client.GetShardFor(guild), emote, new EmbedBuilder());
            embed.Title += "Left Guild";
            embed.Description += $"{guild} `{guild.Id}`\nUsers: {((SocketGuild)guild).MemberCount}";
            embed.Color = new Color(0xe8274d);

            await LogEmbedAsync(guild, embed, false);
        }

        internal async Task LogReadyAsync(DiscordSocketClient client)
        {
            var emote = TryGetEmote(_config["Emotes:Ready"]);
            var embed = FormatEmbed(client, emote, new EmbedBuilder());
            embed.Title += "Ready";
            embed.Description += $"Guilds: `{client.Guilds.Count}`\nUsers: {client.Guilds.Sum(g => g.MemberCount)}";
            embed.Color = new Color(0x7b60f2);

            await LogEmbedAsync(client, embed, false);
        }

        /*
        internal async Task LogPenaltyAsync(ICommandContext context, Penalty penalty)
        {
            var emote = TryGetEmote(_config[$"Emotes:Penalties:{penalty.PenaltyType}"]);
            var embed = FormatEmbed(_client.GetShardFor(context.Guild), emote, new EmbedBuilder());
            embed.Title += "Penalty Reached";
            embed.Description +=
                $"{context.User} reached penalty {penalty.Id} `({penalty.PenaltyType})` in:\n{context.Guild}/{context.Channel}";
            embed.Color = GetPenaltyColor(penalty);

            await LogEmbedAsync(context.Guild, embed, false);
        }

        private static Color GetPenaltyColor(Penalty penalty)
        {
            switch (penalty.PenaltyType)
            {
                case PenaltyType.Nothing:
                    return new Color(0x9ff72c);

                case PenaltyType.Warn:
                    return new Color(0xf7ed2c);

                case PenaltyType.Kick:
                    return new Color(0xf78e2c);

                case PenaltyType.Ban:
                    return new Color(0xf72c2c);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        */
    }
}