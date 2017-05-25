using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services;

namespace NoAdsHere.Commands.Ignore
{
    public class IgnoreModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public IgnoreModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Add")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Add(IGuildUser user)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (!guildSetting.Ignorings.Users.Contains(user.Id))
            {
                guildSetting.Ignorings.Users.Add(user.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will now be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User already Ignored :exclamation:");
            }
        }

        [Command("Add")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Add(IRole role)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (!guildSetting.Ignorings.Roles.Contains(role.Id))
            {
                guildSetting.Ignorings.Roles.Add(role.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will now be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role already Ignored :exclamation:");
            }
        }

        [Command("Add")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Add(ITextChannel channel)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (!guildSetting.Ignorings.Channels.Contains(channel.Id))
            {
                guildSetting.Ignorings.Channels.Add(channel.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will now be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel already Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Remove(IGuildUser user)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Ignorings.Users.Contains(user.Id))
            {
                guildSetting.Ignorings.Users.Remove(user.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will no longer be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User not Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Remove(IRole role)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Ignorings.Roles.Contains(role.Id))
            {
                guildSetting.Ignorings.Roles.Remove(role.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will no longer be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role not Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Remove(ITextChannel channel)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Ignorings.Channels.Contains(channel.Id))
            {
                guildSetting.Ignorings.Channels.Remove(channel.Id);
                await collection.SaveAsync(guildSetting);
                await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will no longer be Ignored :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
            }
        }
    }
}