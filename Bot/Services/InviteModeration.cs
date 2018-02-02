using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bot.Extensions;
using Database;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Bot.Services
{
    public class InviteModeration
    {
        private static readonly Regex InviteRegex =
            new Regex(
                @"(?:discord(?:(?:\.|.?dot.?)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789]{3,8})))",
                RegexOptions.Compiled);

        private static readonly Regex SanitizeRegex = new Regex(@"[\u005C\u007F-\uFFFF\s]+", RegexOptions.Compiled);

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DiscordShardedClient _client;
        private readonly DatabaseContext _dbContext;
        private readonly ICollection<IGuild> _activeGuilds;

        private bool _ready;

        public InviteModeration(DiscordShardedClient client, DatabaseContext dbContext)
        {
            _client = client;
            _dbContext = dbContext;
            _activeGuilds = new List<IGuild>();

            _client.GuildAvailable += GuildLoader;
            _client.MessageReceived += InviteHandler;
            _client.MessageUpdated += (cacheable, message, arg3) => InviteHandler(message);

            _logger.Info("Created new InviteProtector");
        }

        public void Start()
        {
            _ready = true;
            _logger.Info("Started InviteProtector Handler");
        }

        public void Stop()
        {
            _ready = false;
            _logger.Info("Stopped InviteProtector Handler");
        }

        public async Task<bool> AddGuildAsync(IGuild guild)
        {
            if (!_activeGuilds.Contains(guild))
            {
                _activeGuilds.Add(guild);
                _logger.Info("Added Guild {0} to the list of active guilds", guild);

                var gc = await _dbContext.GuildConfigs.FindAsync(guild.Id);
                if (gc == null || gc.InviteProtection)
                    return true;

                gc.InviteProtection = true;
                await _dbContext.SaveChangesAsync();

                _logger.Debug("Enabled InviteModeration in GuildConfig Entry for guild {0}", guild);
                return true;
            }

            _logger.Info("Attempted to add Guild {0} to list of active guilds but was already present", guild);
            return false;
        }

        public async Task<bool> RemoveGuildAsync(IGuild guild)
        {
            if (_activeGuilds.Contains(guild))
            {
                _activeGuilds.Remove(guild);
                _logger.Info("Removed Guild {0} to the list of active guilds", guild);

                var gc = await _dbContext.GuildConfigs.FindAsync(guild.Id);
                if (gc == null || !gc.InviteProtection)
                    return true;

                gc.InviteProtection = false;
                _logger.Debug("Disabled InviteModeration in GuildConfig for guild {0}", guild);
                return true;
            }

            _logger.Info("Attempted to remove Guild {0} from list of active guilds but was not present", guild);
            return false;
        }

        private async Task InviteHandler(SocketMessage socketMessage)
        {
            if (!_ready)
            {
                _logger.Trace("Recieved Dispatch for message {0} but service is not ready yet", socketMessage.Id);
                return;
            }

            if (!(socketMessage is SocketUserMessage message))
            {
                _logger.Trace("SocketMessage {0} is not a User Message", socketMessage.Id);
                return;
            }

            var context = new ShardedCommandContext(_client, message);
            var guildUser = (SocketGuildUser) context.User;

            if (context.IsPrivate)
            {
                _logger.Trace("Message {0} is Private in {1}'s DM", context.Message.Id, context.User);
                return;
            }

            if (context.User.IsBot)
            {
                _logger.Trace("Message {0} was sent by a Bot", context.Message.Id);
                return;
            }

            if (!_activeGuilds.Contains(context.Guild))
            {
                _logger.Trace("Guild {0} is not in the list of acitve InviteModerations", context.Guild);
                return;
            }

            if (!context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages, context.Guild.CurrentUser))
            {
                _logger.Debug("Missing MANAGE_MESSAGES Permissions in guild {0}", context.Guild);
                return;
            }

            var asciiMessage = SanitizeRegex.Replace(context.Message.Content, string.Empty);
            var inviteMatch = InviteRegex.Match(asciiMessage);
            if (!inviteMatch.Success)
            {
                _logger.Trace("Message {0} does not match InviteRegex", context.Message.Id);
                return;
            }

            var invite = await _client.GetInviteAsync(inviteMatch.Groups["id"].Value);
            if (invite == null)
            {
                _logger.Debug("Message {0} has valid Invite Pattern but invite returned null", context.Message.Id);
                return;
            }

            _logger.Debug("Found Valid invite '{0}' for guild '{1}' in Message '{2}'",
                invite.Id,
                invite.GuildName,
                context.Message.Id);

            var cWhitelist = await _dbContext.ChannelWhitelists.FindAsync(context.Guild.Id, context.Channel.Id);
            if (cWhitelist != null)
            {
                if (!cWhitelist.InviteIds.Any() || cWhitelist.InviteIds.Any(inviteId => inviteId == invite.Id))
                {
                    _logger.Debug("Channel '{0}/{1}' is whitelisted for invite {2}",
                        context.Guild,
                        context.Channel,
                        invite.Id);
                    return;
                }
            }

            var uWhitelist = await _dbContext.UserWhitelists.FindAsync(context.Guild.Id, context.User.Id);
            if (uWhitelist != null)
            {
                if (!uWhitelist.InviteIds.Any() || uWhitelist.InviteIds.Any(inviteId => inviteId == invite.Id))
                {
                    if (uWhitelist.ChannelId == 0 || uWhitelist.ChannelId == context.Channel.Id)
                    {
                        _logger.Debug("User {0} is whitelisted for invite {1} in channel {3}/{4}",
                            context.User,
                            invite.Id,
                            context.Guild,
                            context.Channel);
                        return;
                    }
                }
            }

            var rWhitelists =
                _dbContext.RoleWhitelists.Where(whitelist => guildUser.Roles.Any(role => whitelist.RoleId == role.Id));
            if (rWhitelists.Any()) { }

            _logger.Info("Message {0} is not whitelisted in any way. Attempting deletion...", context.Message.Id);

            var dbUser = await _dbContext.DiscordUsers.FindAsync(context.User.Id);
            try
            {
                await context.Message.DeleteAsync();
                dbUser.RemovedInvites.Add(invite.Id);
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "Failed to delte invite '{0}' in message {1}", invite.Id, context.Message.Id);
            }

            dbUser.Violations++;
            
            
        }

        private async Task GuildLoader(SocketGuild socketGuild)
        {
            if (!_activeGuilds.Contains(socketGuild))
            {
                _logger.Debug("Checking guild {0} for active InviteProtection", socketGuild);

                var gc = await _dbContext.GuildConfigs.FindAsync(socketGuild.Id);
                if (gc != null && gc.InviteProtection)
                {
                    await AddGuildAsync(socketGuild);
                    _logger.Info("Added guild {0} to list of active InviteProtections", socketGuild);
                }
            }
        }
    }
}