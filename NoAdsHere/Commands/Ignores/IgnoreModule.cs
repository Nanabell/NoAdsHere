using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.GuildSettings;

namespace NoAdsHere.Commands.Ignores
{
    [Name("Ignore"), Group("Ignore")]
    public class IgnoreModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public IgnoreModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string type, IGuildUser user)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var userIgnores = ignores.GetIgnoreType(IgnoreType.User);

            if (userIgnores.All(ignore => ignore.IgnoredId != user.Id))
            {
                var userIgnore = new Ignore(Context.Guild.Id, IgnoreType.User,
                    user.Id, blockType);
                await collection.InsertOneAsync(userIgnore);
                await ReplyAsync(
                    $":white_check_mark: User {user}`({user.Id})` will now be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User already Ignored :exclamation:");
            }
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string type, IRole role)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var roleIgnores = ignores.GetIgnoreType(IgnoreType.Role);

            if (roleIgnores.All(ignore => ignore.IgnoredId != role.Id))
            {
                var roleIgnore = new Ignore(Context.Guild.Id, IgnoreType.Role,
                    role.Id, blockType);
                await collection.InsertOneAsync(roleIgnore);
                await ReplyAsync(
                    $":white_check_mark: User {role}`({role.Id})` will now be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role already Ignored :exclamation:");
            }
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string type, ITextChannel channel)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var channelIgnores = ignores.GetIgnoreType(IgnoreType.Channel);

            if (channelIgnores.All(ignore => ignore.IgnoredId != channel.Id))
            {
                var channelIgnore = new Ignore(Context.Guild.Id, IgnoreType.Channel,
                    channel.Id, blockType);
                await collection.InsertOneAsync(channelIgnore);
                await ReplyAsync(
                    $":white_check_mark: User {channel}`({channel.Id})` will now be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel already Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(string type, IGuildUser user)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var userIgnores = ignores.GetIgnoreType(IgnoreType.User);

            var first = userIgnores.FirstOrDefault(ignore => ignore.IgnoredId == user.Id);

            if (first != null)
            {
                await collection.DeleteAsync(first);
                await ReplyAsync(
                    $":white_check_mark: User {user}`({user.Id})` will no longer be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User not Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(string type, IRole role)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var roleIgnores = ignores.GetIgnoreType(IgnoreType.Role);

            var first = roleIgnores.FirstOrDefault(ignore => ignore.IgnoredId == role.Id);

            if (first != null)
            {
                await collection.DeleteAsync(first);
                await ReplyAsync(
                    $":white_check_mark: Role {role}`({role.Id})` will no longer be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role not Ignored :exclamation:");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(string type, ITextChannel channel)
        {
            var collection = _mongo.GetCollection<Ignore>(Context.Client);
            var blockType = ParseBlockType(type);
            var ignores = await collection.GetIgnoresAsync(Context.Guild.Id, blockType);
            var channelIgnores = ignores.GetIgnoreType(IgnoreType.Channel);

            var first = channelIgnores.FirstOrDefault(ignore => ignore.IgnoredId == channel.Id);

            if (first != null)
            {
                await collection.DeleteAsync(first);
                await ReplyAsync(
                    $":white_check_mark: Channel {channel}`({channel.Id})` will no longer be Ignored for Invites :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel not Ignored :exclamation:");
            }
        }

        private static BlockType ParseBlockType(string type)
        {
            switch (type)
            {
                case "instantinvites":
                case "invite":
                case "inv":
                    return BlockType.InstantInvite;

                case "youtube":
                case "yt":
                    return BlockType.YoutubeLink;

                case "twitchstream":
                case "stream":
                case "tstream":
                    return BlockType.TwitchStream;

                case "twitchvideo":
                case "video":
                case "tvideo":
                    return BlockType.TwitchVideo;

                case "twitchclip":
                case "clip":
                case "tclip":
                    return BlockType.TwitchClip;

                case "all":
                    return BlockType.All;

                default:
                    return BlockType.All;
            }
        }
    }
}