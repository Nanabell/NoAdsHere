using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services;

namespace NoAdsHere.Commands.Penalties
{
    [Name("Penalties")]
    public class PenaltiesModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public PenaltiesModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }
        
        [Command("Info")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Info(int at)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Penaltings.InfoMessage != at)
            {
                guildSetting.Penaltings.InfoMessage = at;
                await collection.SaveAsync(guildSetting);
                if (at == 0)
                    await ReplyAsync(":white_check_mark: Info Message for Penalties have been Disabled :white_check_mark:");
                else
                    await ReplyAsync($":white_check_mark: Info Message for Penalties have been set to {at}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Info Message already set to {at} :exclamation:");
            }
        }

        [Command("Warn")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Warn(int at)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Penaltings.WarnMessage != at)
            {
                guildSetting.Penaltings.WarnMessage = at;
                await collection.SaveAsync(guildSetting);
                if (at == 0)
                    await ReplyAsync(":white_check_mark: Warn Message for Penalties have been Disabled :white_check_mark:");
                else
                    await ReplyAsync($":white_check_mark: Warn Message for Penalties have been set to {at}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Warn Message already set to {at} :exclamation:");
            }
        }

        [Command("Kick")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Kick(int at)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Penaltings.Kick != at)
            {
                guildSetting.Penaltings.Kick = at;
                await collection.SaveAsync(guildSetting);
                if (at == 0)
                    await ReplyAsync(":white_check_mark: Kick for Penalties have been Disabled :white_check_mark:");
                else
                    await ReplyAsync($":white_check_mark: Kick for Penalties have been set to {at}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Kick already set to {at} :exclamation:");
            }
        }

        [Command("Ban")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(int at)
        {
            var collection = _mongo.GetCollection<GuildSetting>(Context.Client);
            var guildSetting = await collection.GetGuildAsync(Context.Guild.Id);

            if (guildSetting.Penaltings.Ban != at)
            {
                guildSetting.Penaltings.Ban = at;
                await collection.SaveAsync(guildSetting);
                if (at == 0)
                    await ReplyAsync(":white_check_mark: Ban for Penalties have been Disabled :white_check_mark:");
                else
                    await ReplyAsync($":white_check_mark: Ban for Penalties have been set to {at}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync($":exclamation: Ban already set to {at} :exclamation:");
            }
        }
    }
}