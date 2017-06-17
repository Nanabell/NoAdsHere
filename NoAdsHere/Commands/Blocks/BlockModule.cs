using System.Threading.Tasks;
using Discord.Commands;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.AntiAds;

namespace NoAdsHere.Commands.Blocks
{
    [Name("Blocks"), Group("Blocks")]
    public class BlockModule : ModuleBase
    {
        [Command("Invite")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Invites(bool setting)
        {
            bool success;
            if (setting)
                success = await AntiAds.TryEnableGuild(BlockType.InstantInvite, Context.Guild.Id);
            else
                success = await AntiAds.TryDisableGuild(BlockType.InstantInvite, Context.Guild.Id);

            if (success)
                if (setting)
                {
                    await ReplyAsync(
                        $":white_check_mark: Now blocking Discord server invites. Please ensure that the bot has the 'Manage Messages' permission in the required channels. :white_check_mark:");
                }
                else
                {
                    await ReplyAsync($":white_check_mark: No longer blocking Discord server invites. :white_check_mark:");
                }
            else
                await ReplyAsync($":exclamation: Status of Discord server invite blocks already set to {setting}! :exclamation:");
        }

        [Command("Twitch")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Twitch(bool setting)
        {
            bool success;
            if (setting)
            {
                var _1 = await AntiAds.TryEnableGuild(BlockType.TwitchClip, Context.Guild.Id);
                var _2 = await AntiAds.TryEnableGuild(BlockType.TwitchStream, Context.Guild.Id);
                var _3 = await AntiAds.TryEnableGuild(BlockType.TwitchVideo, Context.Guild.Id);
                if (_1 && _2 && _3)
                    success = true;
                else
                    success = false;
            }
            else
            {
                var _1 = await AntiAds.TryDisableGuild(BlockType.TwitchClip, Context.Guild.Id);
                var _2 = await AntiAds.TryDisableGuild(BlockType.TwitchStream, Context.Guild.Id);
                var _3 = await AntiAds.TryDisableGuild(BlockType.TwitchVideo, Context.Guild.Id);
                if (_1 && _2 && _3)
                    success = true;
                else
                    success = false;
            }

            if (success)
                await ReplyAsync($":white_check_mark: Twitch Link Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            else
                await ReplyAsync($":exclamation: Twitch Link Blocks already set to {setting} :exclamation:");
        }

        [Command("Twitch Stream")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task TwitchStream(bool setting)
        {
            bool success;
            if (setting)
                success = await AntiAds.TryEnableGuild(BlockType.TwitchStream, Context.Guild.Id);
            else
                success = await AntiAds.TryDisableGuild(BlockType.TwitchStream, Context.Guild.Id);

            if (success)
                await ReplyAsync($":white_check_mark: Twitch Stream Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            else
                await ReplyAsync($":exclamation: Twitch Stream Blocks already set to {setting} :exclamation:");
        }

        [Command("Twitch Clip")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task TwitchClip(bool setting)
        {
            bool success;
            if (setting)
                success = await AntiAds.TryEnableGuild(BlockType.TwitchClip, Context.Guild.Id);
            else
                success = await AntiAds.TryDisableGuild(BlockType.TwitchClip, Context.Guild.Id);

            if (success)
                await ReplyAsync($":white_check_mark: Twitch Clip Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            else
                await ReplyAsync($":exclamation: Twitch Clip Blocks already set to {setting} :exclamation:");
        }

        [Command("Twitch Video")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task TwitchVideo(bool setting)
        {
            bool success;
            if (setting)
                success = await AntiAds.TryEnableGuild(BlockType.TwitchVideo, Context.Guild.Id);
            else
                success = await AntiAds.TryDisableGuild(BlockType.TwitchVideo, Context.Guild.Id);

            if (success)
                await ReplyAsync($":white_check_mark: Twitch Viode Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            else
                await ReplyAsync($":exclamation: Twitch Video Blocks already set to {setting} :exclamation:");
        }
    }
}