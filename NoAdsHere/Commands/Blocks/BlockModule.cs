using System;
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
            var success = false;
            var blocktype = ParseBlockType(type.ToLower());

            if (blocktype == BlockType.All)
            {
                foreach (BlockType block in Enum.GetValues(typeof(BlockType)))
                {
                    if (block == BlockType.All)
                        continue;
                    success = await AntiAds.TryEnableGuild(block, Context.Guild.Id);
                }
            }
            else
            {
                success = await AntiAds.TryEnableGuild(blocktype, Context.Guild.Id);
            }

            if (success)
            {
                await ReplyAsync(
                    $"Now blocking {blocktype}. Please ensure that the bot has the 'Manage Messages' permission in the required channels.");
                await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} enabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"{blocktype} is already enabled!");
            }
        }

        [Command("Disable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Disable(string type)
        {
            var success = false;
            var blocktype = ParseBlockType(type.ToLower());

            if (blocktype == BlockType.All)
            {
                foreach (BlockType block in Enum.GetValues(typeof(BlockType)))
                {
                    if (block == BlockType.All)
                        continue;
                    success = await AntiAds.TryDisableGuild(block, Context.Guild.Id);
                }
            }
            else
            {
                success = await AntiAds.TryDisableGuild(blocktype, Context.Guild.Id);
            }

            if (success)
            {
                await ReplyAsync(
                    $"No longer blocking {blocktype}.");
                await _logChannelService.LogMessageAsync(_client, Context.Client,
                    Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} disabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"{blocktype} is already enabled!");
            }
        }

        public static BlockType ParseBlockType(string type)
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

                case "all":
                    return BlockType.All;

                default:
                    throw new ArgumentException("Invalid Blocktype");
            }
        }
    }
}