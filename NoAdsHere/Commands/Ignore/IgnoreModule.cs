using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;

namespace NoAdsHere.Commands.Ignore
{
    [Name("Ignore"), Group("Ignore")]
    public class IgnoreModule : ModuleBase
    {
        [Name("Invite"), Group("Invite")]
        public class Invite : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Invite(IServiceProvider provider)
            {
                _mongo = provider.GetService<MongoClient>();
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var userIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.User);

                if (userIgnores.All(ignore => ignore.IgnoredId != user.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.User,
                        user.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will now be Ignored for Invites :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var userIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Role);

                if (userIgnores.All(ignore => ignore.IgnoredId != role.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        role.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will now be Ignored for Invites :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(ITextChannel channel)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var userIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Channel);

                if (userIgnores.All(ignore => ignore.IgnoredId != channel.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        channel.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will now be Ignored for Invites :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Remove")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Remove(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var userIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.User);

                var first = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will no longer be Ignored for Invites :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var roleIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Role);

                var first = roleIgnores.FirstOrDefault(ignore => ignore.IgnoredId == role.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Role {role}`({role.Id})` will no longer be Ignored for Invites :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var inviteIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Invites);
                var channelIgnores = inviteIgnores.GetIgnoreType(IgnoreTypes.Channel);

                var first = channelIgnores.FirstOrDefault(ignore => ignore.IgnoredId == channel.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Channel {channel}`({channel.Id})` will no longer be Ignored for Invites :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
                }
            }
        }

        [Name("Youtube"), Group("Youtube")]
        public class Youtube : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Youtube(IServiceProvider provider)
            {
                _mongo = provider.GetService<MongoClient>();
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var userIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.User);

                if (userIgnores.All(ignore => ignore.IgnoredId != user.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.User,
                        user.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will now be Ignored for Youtube Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var roleIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Role);

                if (roleIgnores.All(ignore => ignore.IgnoredId != role.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        role.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will now be Ignored for Youtube Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(ITextChannel channel)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var channelIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Channel);

                if (channelIgnores.All(ignore => ignore.IgnoredId != channel.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        channel.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will now be Ignored for Youtube Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Remove")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Remove(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var userIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.User);

                var first = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will no longer be Ignored for Youtube Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var roleIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Role);

                var first = roleIgnores.FirstOrDefault(ignore => ignore.IgnoredId == role.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Role {role}`({role.Id})` will no longer be Ignored for Youtube Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var youtubeIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Youtube);
                var channelIgnores = youtubeIgnores.GetIgnoreType(IgnoreTypes.Channel);

                var first = channelIgnores.FirstOrDefault(ignore => ignore.IgnoredId == channel.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Channel {channel}`({channel.Id})` will no longer be Ignored for Youtube Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
                }
            }
        }

        [Name("Twitch"), Group("Twitch")]
        public class Twitch : ModuleBase
        {
            private readonly MongoClient _mongo;

            public Twitch(IServiceProvider provider)
            {
                _mongo = provider.GetService<MongoClient>();
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var userIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.User);

                if (userIgnores.All(ignore => ignore.IgnoredId != user.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.User,
                        user.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will now be Ignored  for Twitch Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var roleIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Role);

                if (roleIgnores.All(ignore => ignore.IgnoredId != role.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        role.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will now be Ignored for Twitch Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(ITextChannel channel)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var channelIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Channel);

                if (channelIgnores.All(ignore => ignore.IgnoredId != channel.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        channel.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will now be Ignored  for Twitch Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Remove")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Remove(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var userIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.User);

                var first = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will no longer be Ignored for Twitch Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var roleIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Role);

                var first = roleIgnores.FirstOrDefault(ignore => ignore.IgnoredId == role.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Role {role}`({role.Id})` will no longer be Ignored for Twitch Links :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var twitchIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.Twitch);
                var channelIgnores = twitchIgnores.GetIgnoreType(IgnoreTypes.Channel);

                var first = channelIgnores.FirstOrDefault(ignore => ignore.IgnoredId == channel.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Channel {channel}`({channel.Id})` will no longer be Ignored for Twitch Links :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
                }
            }
        }

        [Name("All"), Group("All")]
        public class All : ModuleBase
        {
            private readonly MongoClient _mongo;

            public All(IServiceProvider provider)
            {
                _mongo = provider.GetService<MongoClient>();
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var userIgnores = allIgnores.GetIgnoreType(IgnoreTypes.User);

                if (userIgnores.All(ignore => ignore.IgnoredId != user.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.User,
                        user.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will now be Ignored for Anything :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var roleIgnores = allIgnores.GetIgnoreType(IgnoreTypes.Role);

                if (roleIgnores.All(ignore => ignore.IgnoredId != role.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        role.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Role {role.Name}`({role.Id})` will now be Ignored for Anything :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Add")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Add(ITextChannel channel)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var channelIgnores = allIgnores.GetIgnoreType(IgnoreTypes.Channel);

                if (channelIgnores.All(ignore => ignore.IgnoredId != channel.Id))
                {
                    var userIgnore = new Database.Models.GuildSettings.Ignore(Context.Guild.Id, IgnoreTypes.Role,
                        channel.Id, IgnoreingTypes.Invites);
                    await collection.InsertOneAsync(userIgnore);
                    await ReplyAsync($":white_check_mark: Channel {channel.Name}`({channel.Id})` will now be Ignored for Anything :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: User already Ignored :exclamation:");
                }
            }

            [Command("Remove")]
            [RequireBotPermission(ChannelPermission.SendMessages)]
            [RequireUserPermission(GuildPermission.BanMembers)]
            public async Task Remove(IGuildUser user)
            {
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var userIgnores = allIgnores.GetIgnoreType(IgnoreTypes.User);

                var first = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: User {user}`({user.Id})` will no longer be Ignored for Anything :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var roleIgnores = allIgnores.GetIgnoreType(IgnoreTypes.Role);

                var first = roleIgnores.FirstOrDefault(ignore => ignore.IgnoredId == role.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Role {role}`({role.Id})` will no longer be Ignored for Anything :white_check_mark:");
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
                var collection = _mongo.GetCollection<Database.Models.GuildSettings.Ignore>(Context.Client);
                var allIgnores = await collection.GetIgnoresAsync(Context.Guild.Id, IgnoreingTypes.All);
                var channelIgnores = allIgnores.GetIgnoreType(IgnoreTypes.Channel);

                var first = channelIgnores.FirstOrDefault(ignore => ignore.IgnoredId == channel.Id);

                if (first != null)
                {
                    await collection.DeleteAsync(first);
                    await ReplyAsync($":white_check_mark: Channel {channel}`({channel.Id})` will no longer be Ignored for Anything :white_check_mark:");
                }
                else
                {
                    await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
                }
            }
        }
    }
}