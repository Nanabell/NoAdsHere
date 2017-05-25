using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.GuildSettings
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
            var guildSetting = await _mongo.GetCollection<GuildSetting>(Context.Client).GetGuildAsync(Context.Guild.Id);
            var sb = new StringBuilder();

            sb.AppendLine($"Current Settings for {Context.Guild}");
            sb.AppendLine("```");
            sb.AppendLine($"Role Ignores:");
            sb.AppendLine("Which Roles to Ignore");
            foreach (var role in guildSetting.Ignorings.Roles)
                sb.Append($"\t{role}, ");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"User Ignores:");
            sb.AppendLine("Which Users to Ignore");
            foreach (var user in guildSetting.Ignorings.Users)
                sb.Append($"\t{user}, ");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"Channel Ignores:");
            sb.AppendLine("Which Channels to Ignore");
            foreach (var channel in guildSetting.Ignorings.Channels)
                sb.Append($"\t{channel}, ");
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("Blocks:");
            sb.AppendLine("What kind of Advertisement will be blocked.");
            sb.AppendLine($"\tDiscord Invites = {guildSetting.Blockings.Invites}");
            sb.AppendLine($"\tYoutube = {guildSetting.Blockings.Youtube}");
            sb.AppendLine($"\tTwitch = {guildSetting.Blockings.Twitch}");
            sb.AppendLine();

            sb.AppendLine("Penalties:");
            sb.AppendLine("The Ammount of times a User needs to Violate to get a Penalty. (0 for Disabled)");
            sb.AppendLine($"\tInfoMessage = {guildSetting.Penaltings.InfoMessage}");
            sb.AppendLine($"\tWarnMessage = {guildSetting.Penaltings.WarnMessage}");
            sb.AppendLine($"\tKick = {guildSetting.Penaltings.Kick}");
            sb.AppendLine($"\tBan = {guildSetting.Penaltings.Ban}");
            sb.AppendLine("```");

            await ReplyAsync(sb.ToString());
        }
    }
}