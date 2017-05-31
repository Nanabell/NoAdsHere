using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.AntiAds;

namespace NoAdsHere.Commands.Blocks
{
    [Name("Blocks"), Group("Blocks")]
    public class BlockModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public BlockModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Invites"), Alias("Invite")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Invites(bool setting)
        {
            bool success;
            if (setting)
                success = await AntiAds.TryEnableGuild(BlockType.InstantInvite, Context.Guild.Id);
            else
                success = await AntiAds.TryDisableGuild(BlockType.InstantInvite, Context.Guild.Id);

            if (success)
                await ReplyAsync($":white_check_mark: Discord Invite Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            else
                await ReplyAsync($":exclamation: Discord Invite Blocks already set to {setting} :exclamation:");
        }
    }
}