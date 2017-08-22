using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.LogService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        [Command("Enable All")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task EnableAll()
        {
            var sb = new StringBuilder();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                if (!AntiAds.TryEnableGuild(Context.Guild, type))
                    sb.AppendLine($":exclamation: Failed to enable `{type}`. Already enabled!");
                else
                    sb.AppendLine($":white_check_mark: Enabled `{type}`.");
            }
            sb.AppendLine($"Please ensure that the bot has `MANAGE_MESSAGES` permission in the required channels");
            await ReplyAsync(sb.ToString());
            await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} enabled All Blocks in {Context.Guild}").ConfigureAwait(false);
        }

        [Command("Disable All")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task DisableAll()
        {
            var sb = new StringBuilder();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                if (!AntiAds.TryDisableGuild(Context.Guild, type))
                    sb.AppendLine($":exclamation: Failed to disable `{type}`. Already disabled!");
                else
                    sb.AppendLine($":white_check_mark: Disabled `{type}`.");
            }
            await ReplyAsync(sb.ToString());
            await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                    $"{Context.User} disabled All Blocks in {Context.Guild}").ConfigureAwait(false);
        }

        [Command("Enable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Enable(string type)
        {
            try
            {
                var blocktype = ParseBlockType(type.ToLower());
                var success = AntiAds.TryEnableGuild(Context.Guild, blocktype);

                if (success)
                {
                    await ReplyAsync($":white_check_mark: Now blocking {blocktype}. Please ensure that the bot has the `Manage Messages` permission in the required channels.");
                    await _logChannelService.LogMessageAsync(_client, Context.Client, Emote.Parse("<:Action:333712615731888129>"),
                        $"{Context.User} enabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync($":exclamation: {blocktype} is already enabled!");
                }
            }
            catch (KeyNotFoundException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("Disable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Disable(string type)
        {
            try
            {
                var blocktype = ParseBlockType(type.ToLower());
                var success = AntiAds.TryDisableGuild(Context.Guild, blocktype);

                if (success)
                {
                    await ReplyAsync($":white_check_mark: No longer blocking {blocktype}.");
                    await _logChannelService.LogMessageAsync(_client, Context.Client,
                        Emote.Parse("<:Action:333712615731888129>"),
                        $"{Context.User} disabled {blocktype} in {Context.Guild}").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync($":exclamation: {blocktype} is already disabled!");
                }
            }
            catch (KeyNotFoundException ex)
            {
                await ReplyAsync(ex.Message);
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

                default:
                    throw new KeyNotFoundException($"Blocktype `{type}` does not Exist!");
            }
        }
    }
}