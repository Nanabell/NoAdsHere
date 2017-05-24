using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Services.Penalties;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoAdsHere.Services.AntiAds
{
    public class DiscordInvites
    {
        private static readonly Regex _invite = new Regex(@"(?:(?i)discord(?:(?:\.|.?dot.?)(?i)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled);
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
            _logger.Info("Anti Invite service Started!");
            return Task.CompletedTask;
        }

        private async Task InviteChecker(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message == null) return;

            var context = GetContext(message);
            if (context.Guild == null) return;

            if (_invite.IsMatch(context.Message.Content))
            {
                _logger.Info($"Detected Invite in Message {context.Message.Id}");
                var setting = await _mongo.GetCollection<GuildSetting>(_client).GetGuildAsync(context.Guild.Id);

                await TryDelete(setting, context);
            }
        }

        private ICommandContext GetContext(SocketUserMessage message)
            => new SocketCommandContext(_client, message) as ICommandContext;

        private async Task TryDelete(GuildSetting settings, ICommandContext context)
        {
            var guildUser = await context.Guild.GetUserAsync(context.User.Id);
            if (settings.Ignorings.Users.Contains(context.User.Id)) return;
            if (settings.Ignorings.Channels.Contains(context.Channel.Id)) return;
            foreach (var userRole in guildUser.RoleIds)
            {
                if (settings.Ignorings.Roles.Contains(userRole))
                    return;
            }

            if (settings.Blockings.Invites)
            {
                if (context.Channel.CheckChannelPermission(Discord.ChannelPermission.ManageMessages, await context.Guild.GetCurrentUserAsync()))
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
                    _logger.Warn($"Unable to Delete Message {context.Message.Id}. Missing ManageMessage Permission");
                await Violations.AddPoint(context);
            }
        }
    }
}