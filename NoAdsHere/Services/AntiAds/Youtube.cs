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
                await TryDelete(context);
            }
        }

        private ICommandContext GetConxtext(SocketUserMessage message)
            => new SocketCommandContext(_client, message);

        private async Task TryDelete(ICommandContext context)
        {
            var guildUser = context.User as IGuildUser;
            var youtubeIgnores = await _mongo.GetCollection<Ignore>(_client).GetIgnoresAsync(context.Guild.Id, IgnoreingTypes.Youtube);

            if (youtubeIgnores.Any())
            {
                var channelIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Channel);
                var roleIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Role);
                var userIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.User);

                if (channelIgnores.Any(c => c.IgnoredId == context.Channel.Id)) return;
                if (guildUser != null && guildUser.RoleIds.Any(userRole => roleIgnores.Any(r => r.IgnoredId == userRole))) return;
                if (userIgnores.Any(u => u.IgnoredId == context.User.Id)) return;
            }
            var youtubeBlock = await _mongo.GetCollection<Block>(_client).GetBlockAsync(context.Guild.Id, BlockTypes.Youtube);

            if (youtubeBlock.IsEnabled)
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
                await Violations.Violations.AddPoint(context);
            }
        }
    }
}