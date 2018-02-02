using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bot.Extensions;
using Database;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NLog;

namespace Bot.Services.AntiAds
{
    public sealed class AntiAdsService
    {
        private static readonly Regex InviteRegex = new Regex(@"(?:discord(?:(?:\.|.?dot.?)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789]{4,8})))", RegexOptions.Compiled);
        private static readonly Regex SanitizeRegex = new Regex(@"[\u005C\u007F-\uFFFF\s]+", RegexOptions.Compiled);
        
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DiscordShardedClient _client;
        private readonly DatabaseContext _dbContext;
        private readonly ICollection<IGuild> _activeGuilds;

        public AntiAdsService(DiscordShardedClient client, DatabaseContext dbContext)
        {
            _logger.Info("Creating new");
            _client = client;
            _dbContext = dbContext;
            _activeGuilds = new List<IGuild>();

            _client.GuildAvailable += GuildLoader;
            _client.MessageReceived += AdsHandler;
            _client.MessageUpdated += (cacheable, message, arg3) => AdsHandler(message);
        }

        private async Task AdsHandler(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message))
                return;
            
            var context = new ShardedCommandContext(_client, message);
            var guildUser = (SocketGuildUser) context.User;
            
            if (context.IsPrivate || context.User.IsBot)
                return;
            
            if (!context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages, context.Guild.CurrentUser))
                return;

            var asciiMessage = SanitizeRegex.Replace(context.Message.Content, string.Empty);
            var inviteMatch = InviteRegex.Match(asciiMessage);
            if (!inviteMatch.Success)
                return;

            var invite = await _client.GetInviteAsync(inviteMatch.Groups["id"].Value);
            
/*            
            var whitelists = _whitelists.Where(whitelist => whitelist.GuildId == context.Guild.Id).ToList();
            var activeWl = whitelists.FirstOrDefault(whitelist => whitelist.SnowflakeId == context.User.Id) 
                       ?? whitelists.FirstOrDefault(whitelist => 
                           guildUser.Roles.Any(role => role.Id == whitelist.SnowflakeId));

            if (activeWl != null)
            {
                if (activeWl.TargetChannelId != 0 && context.Channel.Id != activeWl.TargetChannelId)
                    return;

                if (activeWl.TargetInvite != null && inviteMatch.Value != activeWl.TargetInvite)
                    return;
            }
            */
        }

        private async Task GuildLoader(SocketGuild socketGuild)
        {
            var guildConfig = await _dbContext.GuildConfigs.FindAsync(socketGuild.Id);
            if (!guildConfig.InviteProtection)
                return;

            if (!_activeGuilds.Contains(socketGuild))
                return;

            _logger.Info("Adding Guild {0] to Colleciton of Active AntiAds Guilds", socketGuild);
            _activeGuilds.Add(socketGuild);
        }
    }
}