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
using NoAdsHere.Services.Configuration;

namespace NoAdsHere.Commands.Ignores
{
    [Name("Ignores"), Alias("Ignore"), Group("Ignores")]
    public class IgnoreModule : ModuleBase<SocketCommandContext>
    {
        private readonly MongoClient _mongo;
        private readonly Config _config;

        public IgnoreModule(IServiceProvider provider, Config config)
        {
            _config = config;
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(-2)]
        public async Task AddHelp([Remainder] string test = null)
        {
            await ReplyAsync($"Correct Usage is: `{_config.Prefix.First()}Ignore Add <Type> <Target>`");
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(-2)]
        public async Task RemoveHelp([Remainder] string test = null)
        {
            await ReplyAsync($"Correct Usage is: `{_config.Prefix.First()}Ignore Remove <Type> <Target>`");
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
                    $":white_check_mark: User {user}`(ID: {user.Id})` will now be whitelisted for {blockType}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User is already whitelisted! :exclamation:");
            }
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(IgnoreType ignoreType, ulong ignoreId, [Remainder] string allowedString)
        {
            var collection = _mongo.GetCollection<AllowString>(Context.Client);
            var allows = await collection.GetIgnoresAsync(Context.Guild.Id);

            if (!allows.Any(a =>
                a.GuildId == Context.Guild.Id && a.IgnoreType == ignoreType && a.IgnoredId == ignoreId &&
                a.AllowedString.Equals(allowedString, StringComparison.OrdinalIgnoreCase)))
            {
                if (await ValidateUlong(Context, ignoreType, ignoreId))
                {
                    var newEntry = new AllowString(Context.Guild.Id, ignoreType, ignoreId, allowedString);
                    await collection.InsertOneAsync(newEntry);
                    await ReplyAsync(
                        $":white_check_mark: String `{allowedString}` will now be whitelisted for `{ignoreType} {ignoreId}`");
                }
                else
                {
                    await ReplyAsync($"Input {ignoreId} is not a valid {ignoreType}");
                }
            }
            else
            {
                await ReplyAsync("A matching entry is already existent");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(IgnoreType ignoreType, ulong ignoreId, [Remainder] string allowedString)
        {
            var collection = _mongo.GetCollection<AllowString>(Context.Client);
            var allows = await collection.GetIgnoresAsync(Context.Guild.Id);

            if (!allows.Any(a =>
                a.GuildId == Context.Guild.Id && a.IgnoreType == ignoreType && a.IgnoredId == ignoreId &&
                a.AllowedString.Equals(allowedString, StringComparison.OrdinalIgnoreCase)))
            {
                if (await ValidateUlong(Context, ignoreType, ignoreId))
                {
                    var allow = allows.First(a =>
                        a.GuildId == Context.Guild.Id && a.IgnoreType == ignoreType && a.IgnoredId == ignoreId &&
                        a.AllowedString.Equals(allowedString, StringComparison.OrdinalIgnoreCase));
                    await collection.DeleteAsync(allow);
                    await ReplyAsync(
                        $":white_check_mark: String `{allowedString}` will no longer be whitelisted for `{ignoreType} {ignoreId}`");
                }
                else
                {
                    await ReplyAsync($"Input {ignoreId} is not a valid {ignoreType}");
                }
            }
            else
            {
                await ReplyAsync("A matching entry is not existent");
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
                    $":white_check_mark: Role {role}`(ID: {role.Id})` will now be whitelisted for {blockType} :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role is already whitelisted. :exclamation:");
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
                    $":white_check_mark: Channel {channel}`(ID: {channel.Id})` will now be whitelisted for {blockType} :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel is already whitelisted. :exclamation:");
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
                    $":white_check_mark: User {user}`(ID: {user.Id})` will no longer be whitelisted for {blockType}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: User is not whitelisted. :exclamation:");
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
                    $":white_check_mark: Role {role}`(ID: {role.Id})` will no longer be whitelisted for {blockType}. :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Role is not already whitelisted. :exclamation:");
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
                    $":white_check_mark: Channel {channel}`(ID: {channel.Id})` will no longer be whitelisted for {blockType} :white_check_mark:");
            }
            else
            {
                await ReplyAsync(":exclamation: Channel is not already whitelisted :exclamation:");
            }
        }

        private async Task<bool> ValidateUlong(ICommandContext context, IgnoreType ignoreType, ulong ignoreId)
        {
            switch (ignoreType)
            {
                case IgnoreType.User:
                    var user = await context.Guild.GetUserAsync(ignoreId);
                    return user != null;

                case IgnoreType.Channel:
                    var channel = await context.Guild.GetChannelAsync(ignoreId);
                    return channel != null;

                case IgnoreType.Role:
                    var role = context.Guild.GetRole(ignoreId);
                    return role != null;

                case IgnoreType.All:
                    var auser = await context.Guild.GetUserAsync(ignoreId);
                    var achannel = await context.Guild.GetChannelAsync(ignoreId);
                    var arole = context.Guild.GetRole(ignoreId);
                    return auser != null || achannel != null || arole != null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ignoreType), ignoreType, null);
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
                case "twitch":
                    return BlockType.TwitchStream;

                case "twitchvideo":
                case "video":
                case "tvideo":
                    return BlockType.TwitchVideo;

                case "twitchclip":
                case "clip":
                case "tclip":
                    return BlockType.TwitchClip;

                default:
                    return BlockType.All;
            }
        }
    }
}