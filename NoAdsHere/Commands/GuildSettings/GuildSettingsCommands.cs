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

        [Name("Ignore"), Group("Ignore")]
        public class Ignoreings : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Ignoreings(IServiceProvider provider)
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
                    await ReplyAsync($":white_check_mark: User {user}({user.Id}) will now be Ignored :white_check_mark:");
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
                    await ReplyAsync($":white_check_mark: Role {role.Name}({role.Id}) will now be Ignored :white_check_mark:");
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
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}({channel.Id}) will now be Ignored :white_check_mark:");
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
                    await ReplyAsync($":white_check_mark: User {user}({user.Id}) will no longer be Ignored :white_check_mark:");
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
                    await ReplyAsync($":white_check_mark: Role {role.Name}({role.Id}) will no longer be Ignored :white_check_mark:");
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
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}({channel.Id}) will no longer be Ignored :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
                }
            }
        }

        [Name("Block"), Group("Block")]
        public class Blockings : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Blockings(IServiceProvider provider)
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

        [Name("Penalties"), Group("Penalties")]
        public class Penalties : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Penalties(IServiceProvider provider)
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
}