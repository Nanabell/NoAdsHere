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
using NoAdsHere.Database;
using NoAdsHere.Database.Models;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Services.Penalties;

namespace NoAdsHere.Services.AntiAds
{
    public class Twitch
    {
        private readonly Regex _stream = new Regex(@"(?i)twitch\.(?i)tv\/(#)?([a-zA-Z0-9][\w]{2,24})", RegexOptions.Compiled);
        private readonly Regex _video = new Regex(@"(?i)twitch\.(?i)tv\/(?i)videos\/(#)?([0-9]{2,24})", RegexOptions.Compiled);
        private readonly Regex _clip = new Regex(@"(?i)clips\.(?i)twitch\.(?i)tv\/(#)?([a-zA-Z0-9][\w]{4,50})", RegexOptions.Compiled);
        private readonly DiscordSocketClient _client;
        private readonly MongoClient _mongo;
        private readonly Logger _logger = LogManager.GetLogger("AntiAds");

        public Twitch(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordSocketClient>();
            _mongo = provider.GetService<MongoClient>();
        }

        public Task StartService()
        {
            _client.MessageReceived += TwitchChecker;
            _logger.Info("Anti Twitch Service Started");
            return Task.CompletedTask;
        }


        private async Task TwitchChecker(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;

            var context = GetConxtext(message);
            if (context.Guild == null) return;
            if (context.User.IsBot) return;

            if (_stream.IsMatch(context.Message.Content) || _clip.IsMatch(context.Message.Content) || _video.IsMatch(context.Message.Content))
            {
                _logger.Info($"Detected Youtube Link in Message {context.Message.Id}");
                await TryDelete(context);
            }
        }
        
        private ICommandContext GetConxtext(SocketUserMessage message)
            => new SocketCommandContext(_client, message);

        private async Task TryDelete(ICommandContext context)
        {
            var guildUser = context.User as IGuildUser;
            var twitchIgnores = await _mongo.GetCollection<Ignore>(_client).GetIgnoresAsync(context.Guild.Id, IgnoreingTypes.Twitch);

            if (twitchIgnores.Any())
            {
                var channelIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Channel);
                var roleIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Role);
                var userIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.User);

                if (channelIgnores.Any(c => c.IgnoredId == context.Channel.Id)) return;
                if (guildUser != null && guildUser.RoleIds.Any(userRole => roleIgnores.Any(r => r.IgnoredId == userRole))) return;
                if (userIgnores.Any(u => u.IgnoredId == context.User.Id)) return;
            }
            var twitchBlock = await _mongo.GetCollection<Block>(_client).GetBlockAsync(context.Guild.Id, BlockTypes.Twitch);

            if (twitchBlock.IsEnabled)
            {
                if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                    await context.Guild.GetCurrentUserAsync()))
                {
                    _logger.Info($"Deleting Message {context.Message.Id} from {context.User}. Message contained a Twitch Link");

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
                await Violations.Violations.AddPoint(context);
            }
        }
    }
}