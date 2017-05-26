using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;

namespace NoAdsHere.Services.AntiAds
{
    public class DiscordInvites
    {
        private readonly Regex _invite = new Regex(@"(?:(?i)discord(?:(?:\.|.?dot.?)(?i)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled);
        private readonly DiscordSocketClient _client;
        private readonly MongoClient _mongo;
        private readonly Logger _logger = LogManager.GetLogger("AntiAds");

        public DiscordInvites(IServiceProvider provider)
        {
            _client = provider.GetService<DiscordSocketClient>();
            _mongo = provider.GetService<MongoClient>();
        }

        public Task StartService()
        {
            _client.MessageReceived += InviteChecker;
            _logger.Info("Anti Invite service Started");
            return Task.CompletedTask;
        }

        private async Task InviteChecker(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message == null) return;

            var context = GetContext(message);
            if (context.Guild == null) return;
            if (context.User.IsBot) return;

            if (_invite.IsMatch(context.Message.Content))
            {
                _logger.Info($"Detected Invite in Message {context.Message.Id}");
                await TryDelete(context);
            }
        }

        private ICommandContext GetContext(SocketUserMessage message)
            => new SocketCommandContext(_client, message);

        private async Task TryDelete(ICommandContext context)
        {
            var guildUser = context.User as IGuildUser;
            var inviteIgnores = await _mongo.GetCollection<Ignore>(_client).GetIgnoresAsync(context.Guild.Id, IgnoreingTypes.Invites);

            if (inviteIgnores.Any())
            {
                var channelIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Channel);
                var roleIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Role);
                var userIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.User);

                if (channelIgnores.Any(c => c.IgnoredId == context.Channel.Id)) return;
                if (guildUser != null && guildUser.RoleIds.Any(userRole => roleIgnores.Any(r => r.IgnoredId == userRole))) return;
                if (userIgnores.Any(u => u.IgnoredId == context.User.Id)) return;
            }
            var inviteBlock = await _mongo.GetCollection<Block>(_client).GetBlockAsync(context.Guild.Id, BlockTypes.Invites);

            if (inviteBlock.IsEnabled)
            {
                if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                    await context.Guild.GetCurrentUserAsync()))
                {
                    _logger.Info($"Deleting Message {context.Message.Id} from {context.User}. Message contained an Invite");
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