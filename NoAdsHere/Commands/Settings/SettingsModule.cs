using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace NoAdsHere.Commands.Settings
{
    [Name("Guild")]
    public class GuildSettingsCommands : ModuleBase
    {
        private readonly MongoClient _mongo;

        public GuildSettingsCommands(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Settings")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Settings()
        {
            //TODO: Redo this
        }
    }
}