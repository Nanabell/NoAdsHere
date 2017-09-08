using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using NoAdsHere.Commands.Blocks;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Ignores
{
    [Name("Ignores"), Alias("Ignore"), Group("Ignores")]
    public class IgnoreModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;
        private readonly IUnitOfWork _unit;

        public IgnoreModule(IConfigurationRoot config, IUnitOfWork unit)
        {
            _config = config;
            _unit = unit;
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(-2)]
#pragma warning disable RECS0154 // Parameter is never used
        public async Task AddHelp([Remainder] string remainder = null)

        {
            await ReplyAsync($"Correct Usage is: `{_config["Prefixes:Main"]} Ignore Add <Type> <Target>`");
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(-2)]
        public async Task RemoveHelp([Remainder] string remainder = null)
#pragma warning restore RECS0154 // Parameter is never used
        {
            await ReplyAsync($"Correct Usage is: `{_config["Prefixes:Main"]} Ignore Remove <Type> <Target>`");
        }

        [Command("Add All")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task AddAll(IGuildUser guildUser)
        {
            var newignores = new List<Ignore>();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                newignores.Add(new Ignore(Context.Guild, guildUser, type));
            }

            var ignores = _unit.Ignores.Get(guildUser);
            newignores = newignores.ExceptBy(ignores, ignore => ignore.BlockType).ToList();

            if (newignores.Count != 0)
            {
                await _unit.Ignores.AddRangeAsync(newignores);
                _unit.SaveChanges();

                await ReplyAsync($":white_check_mark: Added missing whitelist entries for User {guildUser}`({guildUser.Id})`" +
                    $"\n`{string.Join(", ", newignores.Select(ignore => ignore.BlockType))}`");
            }
            else
            {
                await ReplyAsync($":exclamation: User {guildUser}`({guildUser.Id})` is already whitelisted for all blocktypes!");
            }
        }

        [Command("Add All")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task AddAll(IRole role)
        {
            var newignores = new List<Ignore>();
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                newignores.Add(new Ignore(Context.Guild, role, type));
            }

            var ignores = _unit.Ignores.Get(role);
            newignores = newignores.ExceptBy(ignores, ignore => ignore.BlockType).ToList();

            if (newignores.Count != 0)
            {
                await _unit.Ignores.AddRangeAsync(newignores);
                _unit.SaveChanges();

                await ReplyAsync($":white_check_mark: Added missing whitelist entries for Role {role}`(ID: {role.Id})`" +
                    $"\n`{string.Join(", ", newignores.Select(ignore => ignore.BlockType))}`");
            }
            else
            {
                await ReplyAsync($":exclamation: Role {role}`(ID: {role.Id})` is already whitelisted for all blocktypes!");
            }
        }

        [Command("Remove All")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task RemoveAll(IGuildUser guildUser)
        {
            var ignores = _unit.Ignores.Get(guildUser).ToList();

            if (ignores.Any())
            {
                _unit.Ignores.RemoveRange(ignores);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Removed User {guildUser}`(ID: {guildUser.Id})` from whitelist for" +
                    $"\n`{string.Join(", ", ignores.Select(ignore => ignore.BlockType))}`");
            }
            else
            {
                await ReplyAsync($":exclamation: User {guildUser}`(ID: {guildUser.Id})` is not whitelisted anywhere!");
            }
        }

        [Command("Remove All")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task RemoveAll(IRole role)
        {
            var ignores = _unit.Ignores.Get(role).ToList();

            if (ignores.Any())
            {
                _unit.Ignores.RemoveRange(ignores);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Removed Role {role}`(ID: {role.Id})` from whitelist for" +
                    $"\n`{string.Join(", ", ignores.Select(ignore => ignore.BlockType))}`");
            }
            else
            {
                await ReplyAsync($":exclamation: Role {role}`(ID: {role.Id})` is not whitelisted anywhere!");
            }
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string blocktype, IGuildUser guildUser)
        {
            var type = BlockModule.ParseBlockType(blocktype.ToLower());
            var ignores = _unit.Ignores.Get(guildUser);

            if (ignores.All(ignore => ignore.BlockType != type))
            {
                var ignore = new Ignore(Context.Guild, guildUser, type);
                await _unit.Ignores.AddAsync(ignore);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Added User {guildUser}`(ID: {guildUser.Id})` to whitelist for blocktype `{type}`");
            }
            else
            {
                await ReplyAsync($":exclamation: User {guildUser}`(ID: {guildUser.Id})` is already whitelisted for blocktype `{type}`!");
            }
        }

        [Command("Add")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Add(string blocktype, IRole role)
        {
            var type = BlockModule.ParseBlockType(blocktype.ToLower());
            var ignores = _unit.Ignores.Get(role);

            if (ignores.All(ignore => ignore.BlockType != type))
            {
                var ignore = new Ignore(Context.Guild, role, type);
                await _unit.Ignores.AddAsync(ignore);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Added Role {role}`(ID: {role.Id})` to whitelist for blocktype `{type}`");
            }
            else
            {
                await ReplyAsync($":exclamation: Role {role}`(ID: {role.Id})` is already whitelisted for blocktype `{type}`!");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(string blocktype, IGuildUser guildUser)
        {
            var type = BlockModule.ParseBlockType(blocktype.ToLower());
            var ignore = _unit.Ignores.Get(guildUser).FirstOrDefault(ig => ig.BlockType == type);

            if (ignore != null)
            {
                _unit.Ignores.Remove(ignore);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Removed User {guildUser}`(ID: {guildUser.Id})` from whitelist for blocktype `{type}`");
            }
            else
            {
                await ReplyAsync($":exclamation: User {guildUser}`(ID: {guildUser.Id})` is not whitelisted for blocktype `{type}`!");
            }
        }

        [Command("Remove")]
        [RequirePermission(AccessLevel.HighModerator)]
        public async Task Remove(string blocktype, IRole role)
        {
            var type = BlockModule.ParseBlockType(blocktype.ToLower());
            var ignore = _unit.Ignores.Get(role).FirstOrDefault(ig => ig.BlockType == type);

            if (ignore != null)
            {
                _unit.Ignores.Remove(ignore);
                _unit.SaveChanges();
                await ReplyAsync($":white_check_mark: Removed User {role}`(ID: {role.Id})` from whitelist for blocktype `{type}`");
            }
            else
            {
                await ReplyAsync($":exclamation: User {role}`(ID: {role.Id})` is not whitelisted for blocktype `{type}`!");
            }
        }

        [Command("List")]
        [RequirePermission(AccessLevel.Moderator)]
        public async Task List()
        {
            var sb = new StringBuilder();
            var ignores = await _unit.Ignores.GetAllAsync(Context.Guild);
            sb.AppendLine("```");

            foreach (var ignore in ignores.OrderBy(i => i.IgnoreType).GroupBy(i => i.IgnoredId))
            {
                switch (ignore.First().IgnoreType)
                {
                    case IgnoreType.User:
                        sb.Append("USER: ");

                        var user = Context.Guild.GetUser(ignore.Key);
                        sb.Append(user != null ? user.ToString() : "USER LEFT");

                        sb.Append(" => ");
                        sb.AppendLine($"{string.Join(", ", ignore.Select(i => i.BlockType))} ");
                        break;

                    case IgnoreType.Role:

                        sb.Append("USER: ");

                        var role = Context.Guild.GetRole(ignore.Key);
                        sb.Append(role != null ? role.ToString() : "USER LEFT" + " ");

                        sb.Append(" => ");
                        sb.AppendLine($"{string.Join(", ", ignore.Select(i => i.BlockType))} ");
                        break;
                }
            }
            sb.AppendLine("```");
            if (sb.Length > 6)
                await ReplyAsync(sb.ToString());
            else
                await ReplyAsync("Currently no ignores");
        }
    }
}