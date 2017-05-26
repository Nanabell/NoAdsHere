using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;

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

        [Command("Invites")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Invites(bool setting)
        {
            var collection = _mongo.GetCollection<Block>(Context.Client);
            var inviteBlock = await collection.GetBlockAsync(Context.Guild.Id, BlockTypes.Invites);

            if (inviteBlock.IsEnabled != setting)
            {
                inviteBlock.IsEnabled = setting;
                await collection.SaveAsync(inviteBlock);
                await ReplyAsync($":white_check_mark: Discord Invite Blockings have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Discord Invite Blocks already set to {setting} :exclamation:");
            }
        }

        [Command("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Youtube(bool setting)
        {
            var collection = _mongo.GetCollection<Block>(Context.Client);
            var youtubeBlock = await collection.GetBlockAsync(Context.Guild.Id, BlockTypes.Youtube);

            if (youtubeBlock.IsEnabled != setting)
            {
                youtubeBlock.IsEnabled = setting;
                await collection.SaveAsync(youtubeBlock);
                await ReplyAsync($":white_check_mark: Youtube Blocks have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Youtube Blocks already set to {setting} :exclamation:");
            }
        }

        [Command("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Twitch(bool setting)
        {
            var collection = _mongo.GetCollection<Block>(Context.Client);
            var twitchBlock = await collection.GetBlockAsync(Context.Guild.Id, BlockTypes.Twitch);

            if (twitchBlock.IsEnabled != setting)
            {
                twitchBlock.IsEnabled = setting;
                await collection.SaveAsync(twitchBlock);
                await ReplyAsync($":white_check_mark: Twitch Blocks have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Twitch Blocks already set to {setting} :exclamation:");
            }
        }
    }
}