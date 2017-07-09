using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.LogService;

namespace NoAdsHere.Commands.Blocks
{
    [Name("Blocks"), Alias("Block"), Group("Blocks")]
    public class BlockModule : ModuleBase<SocketCommandContext>
    {
        private readonly LogChannelService _logChannelService;
        private readonly DiscordShardedClient _client;

        public BlockModule(LogChannelService logChannelService, DiscordShardedClient client)
        {
            _logChannelService = logChannelService;
            _client = client;
        }

        [Command("Enable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Enable(string type)
        {
            var success = new List<bool>(0);
            var blocktype = ParseBlockType(type);

            switch (blocktype)
            {
                case BlockType.InstantInvite:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.InstantInvite, Context.Guild.Id));
                    break;

                case BlockType.YoutubeLink:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.YoutubeLink, Context.Guild.Id));
                    break;

                case BlockType.TwitchStream:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchStream, Context.Guild.Id));
                    break;

                case BlockType.TwitchVideo:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchVideo, Context.Guild.Id));
                    break;

                case BlockType.TwitchClip:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchClip, Context.Guild.Id));
                    break;

                case BlockType.All:
                    success.Add(await AntiAds.TryEnableGuild(BlockType.InstantInvite, Context.Guild.Id));
                    success.Add(await AntiAds.TryEnableGuild(BlockType.YoutubeLink, Context.Guild.Id));
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchStream, Context.Guild.Id));
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchVideo, Context.Guild.Id));
                    success.Add(await AntiAds.TryEnableGuild(BlockType.TwitchClip, Context.Guild.Id));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (success.All(f => f))
            {
                await ReplyAsync(
                    $"Now blocking {blocktype}. Please ensure that the bot has the 'Manage Messages' permission in the required channels.");
                await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} enabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("Some settings were already enabled.");
            }
        }

        [Command("Disable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Disable(string type)
        {
            var success = new List<bool>(0);
            var blocktype = ParseBlockType(type);

            switch (blocktype)
            {
                case BlockType.InstantInvite:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.InstantInvite, Context.Guild.Id));
                    break;

                case BlockType.YoutubeLink:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.YoutubeLink, Context.Guild.Id));
                    break;

                case BlockType.TwitchStream:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchStream, Context.Guild.Id));
                    break;

                case BlockType.TwitchVideo:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchVideo, Context.Guild.Id));
                    break;

                case BlockType.TwitchClip:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchClip, Context.Guild.Id));
                    break;

                case BlockType.All:
                    success.Add(await AntiAds.TryDisableGuild(BlockType.InstantInvite, Context.Guild.Id));
                    success.Add(await AntiAds.TryDisableGuild(BlockType.YoutubeLink, Context.Guild.Id));
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchStream, Context.Guild.Id));
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchVideo, Context.Guild.Id));
                    success.Add(await AntiAds.TryDisableGuild(BlockType.TwitchClip, Context.Guild.Id));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (success.All(f => f))
            {
                await ReplyAsync(
                    $"No longer blocking {blocktype}.");
                await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} disabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("Some settings were already disabled.");
            }
        }

        private static BlockType ParseBlockType(string type)
        {
            switch (type)
            {
                case "instantinvites":
                case "invite":
                case "invites":
                case "inv":
                    return BlockType.InstantInvite;

                case "youtube":
                case "yt":
                    return BlockType.YoutubeLink;

                case "twitchstream":
                case "stream":
                case "tstream":
                case "twitch":
                    return BlockType.TwitchStream;

                case "twitchvideo":
                case "video":
                case "tvideo":
                    return BlockType.TwitchVideo;

                case "twitchclip":
                case "clip":
                case "tclip":
                    return BlockType.TwitchClip;

                default:
                    return BlockType.All;
            }
        }
    }
}