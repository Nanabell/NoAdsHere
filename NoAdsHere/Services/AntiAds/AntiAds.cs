using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Services.AntiAds
{
    public static class AntiAds
    {
        private static readonly Logger Logger = LogManager.GetLogger("AntiAds");
        private static DiscordShardedClient _client;
        private static DatabaseService _database;
        private static readonly Dictionary<BlockType, List<ulong>> ActiveGuilds = new Dictionary<BlockType, List<ulong>>(0);

        private static readonly Regex InstantInvite = new Regex(@"(?:(?i)discord(?:(?:\.|.?dot.?)(?i)gg|app(?:\.|.?dot.?)com\/invite)\/(?<id>([\w]{10,16}|[a-zA-Z1-9]{4,8})))", RegexOptions.Compiled);

        private static readonly Regex TwitchStream = new Regex(@"(?i)twitch\.(?i)tv\/(#)?([a-zA-Z0-9][\w]{2,24})", RegexOptions.Compiled);

        private static readonly Regex TwitchVideo = new Regex(@"(?i)twitch\.(?i)tv\/(?i)videos\/(#)?([0-9]{2,24})", RegexOptions.Compiled);

        private static readonly Regex TwitchClip = new Regex(@"(?i)clips\.(?i)twitch\.(?i)tv\/(#)?([a-zA-Z0-9][\w]{4,50})", RegexOptions.Compiled);

        private static readonly Regex YoutubeLink = new Regex(@"(?i)youtu(?:\.(?i)be|be\.com)\/(?:.*v(?:\/|=)|(?:.*\/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        public static Task Install(IServiceProvider provider)
        {
            _database = provider.GetRequiredService<DatabaseService>();
            _client = provider.GetService<DiscordShardedClient>();
            return Task.CompletedTask;
        }

        public static Task StartAsync()
        {
            Logger.Info("Loading Blocktypes into ActiveGuilds Dictionary");
            PopulateDictionary();
            Logger.Info("Hooking GuildAvailable event to Populate Active Guilds Dictionary");
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

        private static void PopulateDictionary()
        {
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
                ActiveGuilds.Add(type, new List<ulong>(0));
        }

        private static async Task GuildLoader(SocketGuild socketGuild)
        {
            if (ActiveGuilds.Keys.All(blocktype => !ActiveGuilds[blocktype].Contains(socketGuild.Id)))
            {
                var blocks = await _database.GetBlocksAsync(socketGuild.Id);
                foreach (var block in blocks.OrderBy(block => block.GuildId))
                {
                    ActiveGuilds[block.BlockType].Add(block.GuildId);
                    Logger.Info($"Guild {socketGuild}({socketGuild.Id}) added active list {block.BlockType}.");
                }
            }
            else
            {
                Logger.Debug($"Guild {socketGuild} already active AntiAds");
            }
        }

        public static Task StopAsync()
        {
            _client.MessageReceived -= AdsHandler;
            _client.GuildAvailable -= GuildLoader;
            ActiveGuilds.Clear();
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
                var rawmsg = Regex.Replace(context.Message.Content, @"[\u005C\u007F-\uFFFF\s]+", string.Empty);

                if (ActiveGuilds[BlockType.InstantInvite].Contains(context.Guild.Id))
                {
                    if (InstantInvite.IsMatch(rawmsg))
                        if (await IsToDelete(context, BlockType.InstantInvite).ConfigureAwait(false))
                        {
                            await TryDelete(context, BlockType.InstantInvite).ConfigureAwait(false);
                            await Violations.Violations.Add(context, BlockType.InstantInvite);
                        }
                }
                if (ActiveGuilds[BlockType.TwitchClip].Contains(context.Guild.Id))
                {
                    if (TwitchClip.IsMatch(rawmsg))
                        if (await IsToDelete(context, BlockType.TwitchClip).ConfigureAwait(false))
                        {
                            await TryDelete(context, BlockType.TwitchClip).ConfigureAwait(false);
                            await Violations.Violations.Add(context, BlockType.TwitchClip);
                        }
                }
                if (ActiveGuilds[BlockType.TwitchStream].Contains(context.Guild.Id))
                {
                    if (TwitchStream.IsMatch(rawmsg))
                        if (await IsToDelete(context, BlockType.TwitchStream).ConfigureAwait(false))
                        {
                            await TryDelete(context, BlockType.TwitchStream).ConfigureAwait(false);
                            await Violations.Violations.Add(context, BlockType.TwitchStream);
                        }
                }
                if (ActiveGuilds[BlockType.TwitchVideo].Contains(context.Guild.Id))
                {
                    if (TwitchVideo.IsMatch(rawmsg))
                        if (await IsToDelete(context, BlockType.TwitchVideo).ConfigureAwait(false))
                        {
                            await TryDelete(context, BlockType.TwitchVideo).ConfigureAwait(false);
                            await Violations.Violations.Add(context, BlockType.TwitchVideo);
                        }
                }
                if (ActiveGuilds[BlockType.YoutubeLink].Contains(context.Guild.Id))
                {
                    if (YoutubeLink.IsMatch(rawmsg))
                        if (await IsToDelete(context, BlockType.YoutubeLink).ConfigureAwait(false))
                        {
                            await TryDelete(context, BlockType.YoutubeLink).ConfigureAwait(false);
                            await Violations.Violations.Add(context, BlockType.YoutubeLink);
                        }
                }
            });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public static async Task<bool> TryEnableGuild(BlockType type, ulong guildId)
        {
            if (ActiveGuilds[type].Contains(guildId)) return false;
            ActiveGuilds[type].Add(guildId);
            await UpdateBlockEntry(type, guildId, true).ConfigureAwait(false);
            Logger.Info($"Enabling AntiAds type {type} for guild {_client.GetGuild(guildId)}.");
            return true;
        }

        public static async Task<bool> TryDisableGuild(BlockType type, ulong guildId)
        {
            if (!ActiveGuilds[type].Contains(guildId)) return false;
            ActiveGuilds[type].Remove(guildId);
            await UpdateBlockEntry(type, guildId, false).ConfigureAwait(false);
            Logger.Info($"Disabling AntiAds type {type} for guild {_client.GetGuild(guildId)}.");
            return true;
        }

        private static async Task UpdateBlockEntry(BlockType type, ulong guildId, bool isEnabled)
        {
            var block = await _database.GetBlockAsync(guildId, type);
            await block.UpdateAsync();
        }

        private static async Task<bool> IsToDelete(ICommandContext context, BlockType blockType)
        {
            var masters = await _database.GetMastersAsync();
            if (masters.Any(m => m.UserId == context.User.Id)) return false;

            var guildUser = context.User as IGuildUser;

            var channelIgnores = await _database.GetChannelIgnoresAsync(context.Guild.Id, blockType);
            if (channelIgnores.Any(c => c.IgnoredId == context.Channel.Id)) return false;

            var userIgnores = await _database.GetUserIgnoresAsync(context.Guild.Id, blockType);
            if (userIgnores.Any(u => u.IgnoredId == context.User.Id)) return false;

            var roleIgnores = await _database.GetRoleIgnoresAsync(context.Guild.Id, blockType);
            if (guildUser != null && guildUser.RoleIds.Any(roleId => roleIgnores.Any(r => r.IgnoredId == roleId))) return false;

            var aStrings = await _database.GetIgnoreStringsAsync(context.Guild.Id);
            return !aStrings.CheckAllowedStrings(context);
        }

        private static async Task TryDelete(ICommandContext context, BlockType blockType)
        {
            if (context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages,
                await context.Guild.GetCurrentUserAsync()))
            {
                try
                {
                    await context.Message.DeleteAsync();
                    Logger.Info($"Deleted message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}. Reason: {blockType}.");
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Failed to delete message {context.Message.Id} by {context.User} in {context.Guild}/{context.Channel}.");
                }
            }
        }
    }
}