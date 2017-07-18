using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoAdsHere.Services.AntiAds
{
    public static class AntiAds
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");
        private static DiscordShardedClient _client;
        private static DatabaseService _database;

        public static readonly Dictionary<ulong, List<BlockType>> ActiveBlocks = new Dictionary<ulong, List<BlockType>>(0);

        public static readonly Regex InstantInvite = new Regex(@"(?:discord(?:(?:\.|.?dot.?)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchStream = new Regex(@"twitch\.tv\/(#)?([a-zA-Z0-9][\w]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchVideo = new Regex(@"twitch\.tv\/videos\/(#)?([0-9]{2,24})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TwitchClip = new Regex(@"clips\.twitch\.tv\/(#)?([a-zA-Z0-9][\w]{4,50})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex YoutubeLink = new Regex(@"youtu(?:\.be|be\.com)\/(?:.*v(?:\/|=)|(?:.*\/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SteamScam = new Regex(@"steam(?:reward|special|summer)\.com/?\?id=\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Task Install(IServiceProvider provider)
        {
            _database = provider.GetRequiredService<DatabaseService>();
            _client = provider.GetRequiredService<DiscordShardedClient>();
            return Task.CompletedTask;
        }

        public static Task StartAsync()
        {
            Logger.Info("Hooking GuildAvailable event to Populate Active Blocks");
            _client.GuildAvailable += GuildLoader;
            _client.MessageReceived += AdsHandler;
            _client.MessageUpdated += MessageUpdateAntiAds;
            Logger.Info("AntiAds service started.");
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
                var blocks = await _database.GetBlocksAsync(socketGuild.Id);
                ActiveBlocks.Add(socketGuild.Id, blocks.Select(block => block.BlockType).ToList());
                Logger.Info($"Loaded Guild {socketGuild}'s({socketGuild.Id}) active blockings:\n- " +
                            $"{string.Join("\n- ", blocks.Select(block => block.BlockType))}");
            }
            else
            {
                Logger.Debug($"{socketGuild}({socketGuild.Id}) already loaded into Activeblocks");
            }
        }

        public static Task StopAsync()
        {
            _client.MessageReceived -= AdsHandler;
            _client.GuildAvailable -= GuildLoader;

            ActiveBlocks.Clear();
            Logger.Info("AntiAds service stopped.");
            return Task.CompletedTask;
        }

        private static async Task AdsHandler(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            var context = new CommandContext(_client, message);
            if (context.IsPrivate) return;

            var _ = Task.Run(async () =>
            {
                var rawmsg = GetAsciiMessage(context.Message.Content);

                if (IsActive(context.Guild.Id, BlockType.InstantInvite))
                    if (IsRegexMatch(InstantInvite, rawmsg))
                        await TryDelete(context, BlockType.InstantInvite);

                //TODO: Combine all 3 Twitch Regexes into one
                if (IsActive(context.Guild.Id, BlockType.TwitchClip))
                    if (IsRegexMatch(TwitchClip, rawmsg))
                        await TryDelete(context, BlockType.TwitchClip);

                if (IsActive(context.Guild.Id, BlockType.TwitchStream))
                    if (IsRegexMatch(TwitchStream, rawmsg))
                        await TryDelete(context, BlockType.TwitchStream);

                if (IsActive(context.Guild.Id, BlockType.TwitchVideo))
                    if (IsRegexMatch(TwitchVideo, rawmsg))
                        await TryDelete(context, BlockType.TwitchVideo);

                if (IsActive(context.Guild.Id, BlockType.YoutubeLink))
                    if (IsRegexMatch(YoutubeLink, rawmsg))
                        await TryDelete(context, BlockType.YoutubeLink);

                if (IsActive(context.Guild.Id, BlockType.SteamScam))
                    if (IsRegexMatch(SteamScam, rawmsg))
                        await TryDelete(context, BlockType.SteamScam);
            });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static string GetAsciiMessage(string input)
            => Regex.Replace(input, @"[\u005C\u007F-\uFFFF\s]+", string.Empty);

        public static bool IsRegexMatch(Regex regex, string input)
            => regex.IsMatch(input);

        public static bool IsActive(ulong guildId, BlockType type)
            => ActiveBlocks.ContainsKey(guildId) && ActiveBlocks[guildId].Contains(type);

        private static async Task TryDelete(ICommandContext context, BlockType type)
        {
            if (await IsToDelete(context.Channel as ITextChannel, context.User as IGuildUser, context.Message.Content).ConfigureAwait(false))
            {
                await DeleteMessage(context, type.ToString()).ConfigureAwait(false);
                await Violations.Violations.Add(context, type).ConfigureAwait(false);
            }
        }

        public static async Task<bool> IsToDelete(ITextChannel channel, IGuildUser user, string message)
        {
            var masters = await _database.GetMastersAsync();
            if (masters.Any(m => m.UserId == user.Id)) return false;

            var channelIgnores = await _database.GetChannelIgnoresAsync(channel.GuildId);
            if (channelIgnores.Any(c => c.IgnoredId == channel.Id)) return false;

            var userIgnores = await _database.GetUserIgnoresAsync(channel.GuildId);
            if (userIgnores.Any(u => u.IgnoredId == user.Id)) return false;

            var roleIgnores = await _database.GetRoleIgnoresAsync(channel.GuildId);
            if (user.RoleIds.Any(roleId => roleIgnores.Any(r => r.IgnoredId == roleId))) return false;

            var aStrings = await _database.GetIgnoreStringsAsync(channel.GuildId);
            return !aStrings.CheckAllowedStrings(channel, user, message);
        }

        public static async Task DeleteMessage(ICommandContext context, string reason)
        {
            if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                    await context.Guild.GetCurrentUserAsync()))
            {
                try
                {
                    await context.Message.DeleteAsync();
                    Logger.Info($"Deleted message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}. Reason: {reason}.");
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Failed to delete message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}.");
                }
            }
        }

        public static async Task<bool> TryEnableGuild(ulong guildId, BlockType type)
        {
            if (ActiveBlocks.ContainsKey(guildId))
            {
                if (ActiveBlocks[guildId].Contains(type))
                    return false;
                ActiveBlocks[guildId].Add(type);
                await UpdateDatabase(guildId, type, true);
                Logger.Info($"Enabled blocktype {type} in guild {_client.GetGuild(guildId)}");
                return true;
            }
            ActiveBlocks.Add(guildId, new List<BlockType>() { type });
            return true;
        }

        public static async Task<bool> TryDisableGuild(ulong guildId, BlockType type)
        {
            if (!ActiveBlocks.ContainsKey(guildId))
                return true;
            if (!ActiveBlocks[guildId].Contains(type))
                return false;
            ActiveBlocks[guildId].Remove(type);
            await UpdateDatabase(guildId, type, false);
            Logger.Info($"Disabled blocktype {type} in guild {_client.GetGuild(guildId)}");
            return true;
        }

        private static async Task UpdateDatabase(ulong guildId, BlockType type, bool isEnable)
        {
            try
            {
                var block = await _database.GetBlockAsync(guildId, type, isEnable);
                if (!isEnable && block != null)
                    await block.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                Logger.Warn($"Unable to save changes to Database");
            }
        }
    }
}