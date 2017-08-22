using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoAdsHere.Services.AntiAds
{
    public static class AntiAds
    {
        private static DiscordShardedClient _client;
        private static IUnitOfWork _unit;
        private static ILogger _logger;

        public static readonly Dictionary<ulong, List<BlockType>> ActiveBlocks = new Dictionary<ulong, List<BlockType>>(0);

        public static readonly Regex InstantInvite = new Regex(@"(?:discord(?:(?:\.|.?dot.?)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchStream = new Regex(@"twitch\.tv\/(#)?([a-zA-Z0-9][\w]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchVideo = new Regex(@"twitch\.tv\/videos\/(#)?([0-9]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchClip = new Regex(@"clips\.twitch\.tv\/(#)?([a-zA-Z0-9][\w]{4,50})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex YoutubeLink = new Regex(@"youtu(?:\.be|be\.com)\/(?:.*v(?:\/|=)|(?:.*\/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SteamScam = new Regex(@"steam(?:reward|special|summer)\.com/?\?id=\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Task Install(IServiceProvider provider)
        {
            _unit = provider.GetRequiredService<IUnitOfWork>();
            _client = provider.GetRequiredService<DiscordShardedClient>();
            _logger = provider.GetService<ILoggerFactory>().CreateLogger(typeof(AntiAds));
            return Task.CompletedTask;
        }

        public static Task StartAsync()
        {
            _logger.LogInformation(new EventId(100), "Hooking GuildAvailable event to Populate Active Blocks");
            _client.GuildAvailable += GuildLoader;
            _client.MessageReceived += AdsHandler;
            _client.MessageUpdated += MessageUpdateAntiAds;
            _logger.LogInformation(new EventId(200), "AntiAds service started.");
            return Task.CompletedTask;
        }

        private static async Task MessageUpdateAntiAds(Cacheable<IMessage, ulong> _, SocketMessage socketMessage, ISocketMessageChannel channel)
        {
            await AdsHandler(socketMessage).ConfigureAwait(false);
        }

        private static async Task GuildLoader(SocketGuild socketGuild)
        {
            if (!ActiveBlocks.ContainsKey(socketGuild.Id))
            {
                var blocks = (await _unit.Blocks.GetAllAsync(socketGuild)).ToList();
                ActiveBlocks.Add(socketGuild.Id, blocks.Select(block => block.BlockType).ToList());
                _logger.LogInformation(new EventId(200), $"Loaded Guild {socketGuild}'s({socketGuild.Id}) active blockings:\n- " +
                            $"{string.Join("\n- ", blocks.Select(block => block.BlockType))}");
            }
            else
            {
                _logger.LogDebug(new EventId(208), $"{socketGuild}({socketGuild.Id}) already loaded into Activeblocks");
            }
        }

        public static Task StopAsync()
        {
            _client.MessageReceived -= AdsHandler;
            _client.GuildAvailable -= GuildLoader;

            ActiveBlocks.Clear();
            _logger.LogInformation(new EventId(200), "AntiAds service stopped.");
            return Task.CompletedTask;
        }

        private static async Task AdsHandler(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            var context = new CommandContext(_client, message);
            if (context.IsPrivate) return;

            await Task.Run(async () =>
             {
                 var rawmsg = GetAsciiMessage(context.Message.Content);

                 if (IsActive(context.Guild, BlockType.InstantInvite))
                     if (IsRegexMatch(InstantInvite, rawmsg))
                         await TryDelete(context, BlockType.InstantInvite);

                 //TODO: Combine all 3 Twitch Regexes into one
                 if (IsActive(context.Guild, BlockType.TwitchClip))
                     if (IsRegexMatch(TwitchClip, rawmsg))
                         await TryDelete(context, BlockType.TwitchClip);

                 if (IsActive(context.Guild, BlockType.TwitchStream))
                     if (IsRegexMatch(TwitchStream, rawmsg))
                         await TryDelete(context, BlockType.TwitchStream);

                 if (IsActive(context.Guild, BlockType.TwitchVideo))
                     if (IsRegexMatch(TwitchVideo, rawmsg))
                         await TryDelete(context, BlockType.TwitchVideo);

                 if (IsActive(context.Guild, BlockType.YoutubeLink))
                     if (IsRegexMatch(YoutubeLink, rawmsg))
                         await TryDelete(context, BlockType.YoutubeLink);

                 if (IsActive(context.Guild, BlockType.SteamScam))
                     if (IsRegexMatch(SteamScam, rawmsg))
                         await TryDelete(context, BlockType.SteamScam);
             });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static string GetAsciiMessage(string input)
            => Regex.Replace(input, @"[\u005C\u007F-\uFFFF\s]+", string.Empty);

        public static bool IsRegexMatch(Regex regex, string input)
            => regex.IsMatch(input);

        public static bool IsActive(IGuild guild, BlockType type)
            => ActiveBlocks.ContainsKey(guild.Id) && ActiveBlocks[guild.Id].Contains(type);

        private static async Task TryDelete(ICommandContext context, BlockType type)
        {
            if (await IsToDelete(context.User as IGuildUser, context.Message.Content).ConfigureAwait(false))
            {
                await DeleteMessage(context, type.ToString()).ConfigureAwait(false);
                await Violations.Violations.Add(context, type).ConfigureAwait(false);
            }
        }

        public static async Task<bool> IsToDelete(IGuildUser user, string message)
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

        public static bool IsStringIgnore(Ignore ignore)
            => ignore.IgnoredString != null;

        public static bool CompareIgnoredString(Ignore ignore, string message)
            => ignore.IgnoredString.Equals(message, StringComparison.OrdinalIgnoreCase);

        private static async Task DeleteMessage(ICommandContext context, string reason)
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

        public static bool TryEnableGuild(IGuild guild, BlockType type)
        {
            if (ActiveBlocks.ContainsKey(guild.Id))
            {
                if (ActiveBlocks[guild.Id].Contains(type))
                    return false;
                ActiveBlocks[guild.Id].Add(type);
                UpdateDatabase(guild, type, true);
                _logger.LogInformation(new EventId(200), $"Enabled blocktype {type} in guild {guild}");
                return true;
            }
            ActiveBlocks.Add(guild.Id, new List<BlockType>() { type });
            return true;
        }

        public static bool TryDisableGuild(IGuild guild, BlockType type)
        {
            if (!ActiveBlocks.ContainsKey(guild.Id))
                return true;
            if (!ActiveBlocks[guild.Id].Contains(type))
                return false;
            ActiveBlocks[guild.Id].Remove(type);
            UpdateDatabase(guild, type, false);
            _logger.LogInformation(new EventId(200), $"Disabled blocktype {type} in guild {guild}");
            return true;
        }

        private static void UpdateDatabase(IGuild guild, BlockType type, bool isEnable)
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