using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Services.Penalties;

namespace NoAdsHere.Services.AntiAds
{
    public class Youtube
    {
        private readonly Regex _ytLink = new Regex(@"(?i)youtu(?:\.(?i)be|be\.com)\/(?:.*v(?:\/|=)|(?:.*\/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private readonly DiscordSocketClient _client;
        private readonly MongoClient _mongo;
        private readonly Logger _logger = LogManager.GetLogger("AntiAds");

        public Youtube(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordSocketClient>();
            _mongo = provider.GetService<MongoClient>();
        }

        public Task StartService()
        {
            _client.MessageReceived += YoutubeChecker;
            _logger.Info("Anti Youtube Service Started");
            return Task.CompletedTask;
        }

        private async Task YoutubeChecker(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;

            var context = GetConxtext(message);
            if (context.Guild == null) return;
            if (context.User.IsBot) return;

            if (_ytLink.IsMatch(context.Message.Content))
            {
                _logger.Info($"Detected Youtube Link in Message {context.Message.Id}");
                var setting = await _mongo.GetCollection<GuildSetting>(_client).GetGuildAsync(context.Guild.Id);

                await TryDelete(setting, context);
            }
        }

        private ICommandContext GetConxtext(SocketUserMessage message)
            => new SocketCommandContext(_client, message);

        private async Task TryDelete(GuildSetting settings, ICommandContext context)
        {
            var guildUser = context.User as IGuildUser;
            if (settings.Ignorings.Users.Contains(context.User.Id)) return;
            if (settings.Ignorings.Channels.Contains(context.Channel.Id)) return;
            if (guildUser != null && guildUser.RoleIds.Any(userRole => settings.Ignorings.Roles.Contains(userRole))) return;

            if (settings.Blockings.Youtube)
            {
                if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                    await context.Guild.GetCurrentUserAsync()))
                {
                    _logger.Info($"Deleting Message {context.Message.Id} from {context.User}. Message contained a Youtube Link");

                    try
                    {
                        await context.Message.DeleteAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e, $"Deleting of Message {context.Message.Id} Failed");
                    }
                }
                else
                    _logger.Warn($"Unable to Delete Message {context.Message.Id}. Missing ManageMessages Permission");
                await Violations.AddPoint(context);
            }
        }
    }
}