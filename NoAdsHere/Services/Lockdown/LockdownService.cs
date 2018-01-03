using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;

namespace NoAdsHere.Services.Lockdown
{
    public class LockdownService
    {
        private readonly DiscordShardedClient _client;
        private readonly ILogger _logger;

        private readonly Emoji _lock = new Emoji("🔒");
        private readonly Emoji _unlock = new Emoji("🔓");

        public LockdownService(DiscordShardedClient client, ILoggerFactory factory)
        {
            _client = client;
            _logger = factory.CreateLogger<LockdownService>();
        }

        public void Start()
        {
            _client.MessageReceived += LockingHandler;
        }

        public void Stop()
        {
            _client.MessageReceived -= LockingHandler;
        }

        private async Task LockingHandler(SocketMessage socketMessage)
        {
            if (socketMessage is SocketUserMessage message)
            {
                var context = new ShardedCommandContext(_client, message);

                if (context.IsPrivate)
                    return;

                if (!context.Channel.CheckChannelPermission(ChannelPermission.ManageChannels, context.Guild.CurrentUser))
                    return;

                if (context.Channel is SocketGuildChannel channel)
                {
                    if (context.Message.Content == _lock.Name)
                    {
                        await channel.AddPermissionOverwriteAsync(context.Guild.EveryoneRole, GetLockPerms(GetOverwritePermissions(context, channel)));
                        await message.AddReactionAsync(_lock);
                        _logger.LogInformation($"{context.User} locked {context.Guild}/{channel}");
                    }
                    else if (context.Message.Content == _unlock.Name)
                    {
                        await channel.AddPermissionOverwriteAsync(context.Guild.EveryoneRole, GetUnlockPerms(GetOverwritePermissions(context, channel)));
                        await message.AddReactionAsync(_unlock);
                        _logger.LogInformation($"{context.User} unlocked {context.Guild}/{channel}");
                    }
                }
            }
        }

        private static OverwritePermissions GetOverwritePermissions(ShardedCommandContext context, SocketGuildChannel channel)
        {
            var channelperms = channel.GetPermissionOverwrite(context.Guild.EveryoneRole);
            return channelperms ?? OverwritePermissions.InheritAll;
        }

        private static OverwritePermissions GetLockPerms(OverwritePermissions permissions)
            => permissions.Modify(sendMessages: PermValue.Deny);

        private static OverwritePermissions GetUnlockPerms(OverwritePermissions permissions)
            => permissions.Modify(sendMessages: PermValue.Allow);
    }
}