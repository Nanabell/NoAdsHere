using Discord.Commands;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.LogService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Blocks
{
    /*
    [Name("Blocks"), Alias("Block"), Group("Blocks")]
    public class BlockModule : ModuleBase<SocketCommandContext>
    {
        private readonly AntiAdsService _adsService;
        private readonly WebhookLogService _logChannelService;
        private readonly IUnitOfWork _unit;

        public BlockModule(AntiAdsService adsService, WebhookLogService logChannelService, IUnitOfWork unit)
        {
            _adsService = adsService;
            _logChannelService = logChannelService;
            _unit = unit;
        }

        [Command("Enable All")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task EnableAll()
        {
            var sb = new StringBuilder();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                sb.AppendLine(!_adsService.TryEnableGuild(Context.Guild, type)
                    ? $":exclamation: Failed to enable `{type}`. Already enabled!"
                    : $":white_check_mark: Enabled `{type}`.");
            }
            sb.AppendLine($"Please ensure that the bot has `MANAGE_MESSAGES` permission in the required channels");
            await ReplyAsync(sb.ToString());
            await _logChannelService.LogActionAsync(Context.Guild,
                $"{Context.User} enabled All Blocks in {Context.Guild}");
        }

        [Command("Disable All")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task DisableAll()
        {
            var sb = new StringBuilder();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                sb.AppendLine(!_adsService.TryDisableGuild(Context.Guild, type)
                    ? $":exclamation: Failed to disable `{type}`. Already disabled!"
                    : $":white_check_mark: Disabled `{type}`.");
            }
            await ReplyAsync(sb.ToString());
            await _logChannelService.LogActionAsync(Context.Guild,
                $"{Context.User} disabled All Blocks in {Context.Guild}");
        }

        [Command("Enable")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Enable(string type)
        {
            try
            {
                var blocktype = ParseBlockType(type.ToLower());
                var success = _adsService.TryEnableGuild(Context.Guild, blocktype);

                if (success)
                {
                    await ReplyAsync($":white_check_mark: Now blocking {blocktype}. Please ensure that the bot has the `Manage Messages` permission in the required channels.");
                    await _logChannelService.LogActionAsync(Context.Guild,
                        $"{Context.User} enabled {blocktype} in {Context.Guild}");
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
                var success = _adsService.TryDisableGuild(Context.Guild, blocktype);

                if (success)
                {
                    await ReplyAsync($":white_check_mark: No longer blocking {blocktype}.");
                    await _logChannelService.LogActionAsync(Context.Guild,
                        $"{Context.User} disabled {blocktype} in {Context.Guild}");
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

        [Command("List")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task List()
        {
            var sb = new StringBuilder();
            var blocks = (await _unit.Blocks.GetAllAsync(Context.Guild)).ToList();

            foreach (BlockType block in Enum.GetValues(typeof(BlockType)))
                sb.AppendLine($"{(blocks.Any(b => b.BlockType == block) ? ":white_check_mark:" : ":x:")} {block}");

            await ReplyAsync(sb.ToString());
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

                default:
                    throw new KeyNotFoundException($"Blocktype `{type}` does not Exist!");
            }
        }
    }
    */
}