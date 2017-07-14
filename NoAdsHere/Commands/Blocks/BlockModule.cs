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

            if (blocktype == BlockType.All)
            {
                foreach (BlockType block in Enum.GetValues(typeof(BlockType)))
                {
                    success.Add(await AntiAds.TryEnableGuild(block, Context.Guild.Id));
                }
            }
            success.Add(await AntiAds.TryEnableGuild(blocktype, Context.Guild.Id));

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

            if (blocktype == BlockType.All)
            {
                foreach (BlockType block in Enum.GetValues(typeof(BlockType)))
                {
                    success.Add(await AntiAds.TryDisableGuild(block, Context.Guild.Id));
                }
            }
            success.Add(await AntiAds.TryDisableGuild(blocktype, Context.Guild.Id));

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

                case "steam":
                    return BlockType.SteamScam;

                default:
                    return BlockType.All;
            }
        }
    }
}