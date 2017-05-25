using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services;

namespace NoAdsHere.Commands.Blocks
{
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
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Blockings.Invites != setting)
            {
                guildSetting.Blockings.Invites = setting;
                await collection.SaveAsync(guildSetting);
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
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Blockings.Youtube != setting)
            {
                guildSetting.Blockings.Youtube = setting;
                await collection.SaveAsync(guildSetting);
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
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Blockings.Twitch != setting)
            {
                guildSetting.Blockings.Twitch = setting;
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: Twitch Blocks have been set to {setting}. {(setting ? "Please ensure that the bot can ManageMessages in the required channels" : "")} :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Twitch Blocks already set to {setting} :exclamation:");
            }
        }
    }
}