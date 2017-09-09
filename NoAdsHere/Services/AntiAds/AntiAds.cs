using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NoAdsHere.Services.Violations;

namespace NoAdsHere.Services.AntiAds
{
    public class AntiAdsService
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _unit;
        private readonly ViolationsService _violationsService;
        private readonly ILogger _logger;

        private readonly Dictionary<ulong, List<BlockType>> _activeBlocks = new Dictionary<ulong, List<BlockType>>(0);

        private readonly List<KeyValuePair<Regex, BlockType>> _regexes = new List<KeyValuePair<Regex, BlockType>>
        {
            new KeyValuePair<Regex, BlockType>(new Regex(@"(?:discord(?:(?:\.|.?dot.?)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled | RegexOptions.IgnoreCase), BlockType.InstantInvite),
            new KeyValuePair<Regex, BlockType>(new Regex(@"twitch\.tv\/(#)?([a-zA-Z0-9][\w]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase), BlockType.TwitchStream),
            new KeyValuePair<Regex, BlockType>(new Regex(@"twitch\.tv\/videos\/(#)?([0-9]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase), BlockType.TwitchVideo),
            new KeyValuePair<Regex, BlockType>(new Regex(@"clips\.twitch\.tv\/(#)?([a-zA-Z0-9][\w]{4,50})", RegexOptions.Compiled | RegexOptions.IgnoreCase), BlockType.TwitchClip),
            new KeyValuePair<Regex, BlockType>(new Regex(@"youtu(?:\.be|be\.com)\/(?:.*v(?:\/|=)|(?:.*\/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase), BlockType.YoutubeLink)
        };

        public AntiAdsService(ILoggerFactory factory, DiscordShardedClient client, IUnitOfWork unit, ViolationsService violationsService)
        {
            _client = client;
            _unit = unit;
            _violationsService = violationsService;
            _logger = factory.CreateLogger<AntiAdsService>();
        }

        public void StartService()
        {
            _logger.LogInformation(new EventId(100), "Starting up AntiAds service...");

            _client.GuildAvailable += GuildLoader;
            _client.MessageReceived += AdsHandler;
            _client.MessageUpdated += MessageUpdateAntiAds;

            _logger.LogInformation(new EventId(200), "AntiAds service started.");
        }

        private async Task MessageUpdateAntiAds(Cacheable<IMessage, ulong> _, SocketMessage socketMessage, ISocketMessageChannel channel)
        { await AdsHandler(socketMessage).ConfigureAwait(false); }

        private async Task GuildLoader(SocketGuild socketGuild)
        {
            if (!_activeBlocks.ContainsKey(socketGuild.Id))
            {
                var blocks = (await _unit.Blocks.GetAllAsync(socketGuild)).ToList();
                _activeBlocks.Add(socketGuild.Id, blocks.Select(block => block.BlockType).ToList());

                _logger.LogInformation(new EventId(200),
                    $"Loaded Guild {socketGuild}'s({socketGuild.Id}) active blockings:\n- " +
                    $"{string.Join("\n- ", blocks.Select(block => block.BlockType))}");
            }
            else { _logger.LogDebug(new EventId(208), $"{socketGuild}({socketGuild.Id}) already loaded into Activeblocks"); }
        }

        public void StopAsync()
        {
            _logger.LogInformation(new EventId(100), "Shutting down AntiAds service...");

            _client.MessageReceived -= AdsHandler;
            _client.MessageUpdated -= MessageUpdateAntiAds;
            _client.GuildAvailable -= GuildLoader;
            _activeBlocks.Clear();

            _logger.LogInformation(new EventId(200), "AntiAds service stopped.");
        }

        private async Task AdsHandler(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            var context = new CommandContext(_client, message);
            if (context.IsPrivate) return;

            await Task.Run(async () =>
             {
                 var rawmsg = GetAsciiMessage(context.Message.Content);

                 foreach (var regex in _regexes)
                 {
                     if (!IsActive(context.Guild, regex.Value)) continue;
                     if (!IsRegexMatch(regex.Key, rawmsg)) continue;

                     await TryDelete(context, regex.Value);
                 }
             });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private string GetAsciiMessage(string input)
            => Regex.Replace(input, @"[\u005C\u007F-\uFFFF\s]+", string.Empty);

        public bool IsRegexMatch(Regex regex, string input)
            => regex.IsMatch(input);

        public bool IsActive(IGuild guild, BlockType type)
            => _activeBlocks.ContainsKey(guild.Id) && _activeBlocks[guild.Id].Contains(type);

        private async Task TryDelete(ICommandContext context, BlockType type)
        {
            if (await IsToDelete(context.User as IGuildUser, context.Message.Content).ConfigureAwait(false))
            {
                await DeleteMessage(context, type.ToString()).ConfigureAwait(false);
                await _violationsService.Add(context, type).ConfigureAwait(false);
            }
        }

        public async Task<bool> IsToDelete(IGuildUser user, string message)
        {
            if (user == null)
                return false;
            if (string.IsNullOrEmpty(message))
                return false;

            var master = await _unit.Masters.GetAsync(user);
            if (master != null)
                return false;

            var ignores = (await _unit.Ignores.GetAllAsync(user.Guild)).ToList();

            var userIgnores = ignores.Where(ignore => ignore.IgnoreType == IgnoreType.User);
            var userIgnore = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

            if (userIgnore != null)
            {
                if (!IsStringIgnore(userIgnore))
                    return false;
                return !CompareIgnoredString(userIgnore, message);
            }

            var roleIgnores = ignores.Where(ignore => ignore.IgnoreType == IgnoreType.Role).ToList();
            var roleIgnore = roleIgnores.FirstOrDefault(ignore =>
                ignore.IgnoredId == user.RoleIds.FirstOrDefault(roleId =>
                    roleIgnores.Any(r => r.IgnoredId == roleId)));

            if (roleIgnore == null)
                return true;
            if (!IsStringIgnore(roleIgnore))
                return false;

            return !CompareIgnoredString(roleIgnore, message);
        }

        public bool IsStringIgnore(Ignore ignore)
            => ignore.IgnoredString != null;

        public bool CompareIgnoredString(Ignore ignore, string message)
            => ignore.IgnoredString.Equals(message, StringComparison.OrdinalIgnoreCase);

        private async Task DeleteMessage(ICommandContext context, string reason)
        {
            if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                    await context.Guild.GetCurrentUserAsync()))
            {
                try
                {
                    await context.Message.DeleteAsync();
                    _logger.LogInformation(new EventId(200), $"Deleted message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}. Reason: {reason}.");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(new EventId(403), e, $"Failed to delete message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}.");
                }
            }
        }

        public bool TryEnableGuild(IGuild guild, BlockType type)
        {
            if (_activeBlocks.ContainsKey(guild.Id))
            {
                if (_activeBlocks[guild.Id].Contains(type))
                    return false;
                _activeBlocks[guild.Id].Add(type);
                UpdateDatabase(guild, type, true);
                _logger.LogInformation(new EventId(200), $"Enabled blocktype {type} in guild {guild}");
                return true;
            }
            _activeBlocks.Add(guild.Id, new List<BlockType>() { type });
            return true;
        }

        public bool TryDisableGuild(IGuild guild, BlockType type)
        {
            if (!_activeBlocks.ContainsKey(guild.Id))
                return true;
            if (!_activeBlocks[guild.Id].Contains(type))
                return false;
            _activeBlocks[guild.Id].Remove(type);
            UpdateDatabase(guild, type, false);
            _logger.LogInformation(new EventId(200), $"Disabled blocktype {type} in guild {guild}");
            return true;
        }

        private void UpdateDatabase(IGuild guild, BlockType type, bool isEnable)
        {
            try
            {
                var block = _unit.Blocks.Get(guild, type);
                if (!isEnable && block != null)
                    _unit.Blocks.Remove(block);
                else
                    _unit.Blocks.Add(new Block(guild, type));
                _unit.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(new EventId(500), ex, $"Unable to save changes to Database");
            }
        }
    }
}