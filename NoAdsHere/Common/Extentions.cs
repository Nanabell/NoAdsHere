using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NoAdsHere.Database.Models.GuildSettings;

namespace NoAdsHere.Common
{
    public static class Extentions
    {
        public static string RemoveWhitespace(this string input)
        {
            return new string(input
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
        
        public static bool CheckChannelPermission(this IMessageChannel channel, ChannelPermission permission, IGuildUser guildUser)
        {
            var guildchannel = channel as IGuildChannel;

            ChannelPermissions perms;
            perms = guildchannel != null ? guildUser.GetPermissions(guildchannel) : ChannelPermissions.All(null);

            return perms.Has(permission);
        }
        
        public static IEnumerable<Ignore> GetIgnoreType(this IEnumerable<Ignore> ignores, IgnoreType type)
            => ignores.Where(i => i.IgnoreType == type || i.IgnoreType == IgnoreType.All);
        
        public static async Task<IEnumerable<CommandInfo>> CheckConditionsAsync(this IEnumerable<CommandInfo> commandInfos,
            ICommandContext context, IServiceProvider map = null)
        {
            var ret = new List<CommandInfo>();
            foreach (var commandInfo in commandInfos)
            {
                if (!(await commandInfo.CheckPreconditionsAsync(context, map)).IsSuccess) continue;
                ret.Add(commandInfo);
            }
            return ret;
        }

        public static bool CheckAllowedStrings(this IEnumerable<AllowString> allowStrings, ICommandContext context)
        {
            var guildUser = context.User as IGuildUser;
            foreach (var allowString in allowStrings)
            {
                switch (allowString.IgnoreType)
                {
                    case IgnoreType.User:
                        if (context.User.Id == allowString.IgnoredId)
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        break;
                    case IgnoreType.Role:
                        if (guildUser != null && guildUser.RoleIds.Any(roleId => roleId == allowString.IgnoredId))
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        break;
                    case IgnoreType.Channel:
                        if (context.Channel.Id == allowString.IgnoredId)
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        break;
                    case IgnoreType.All:
                        if (context.User.Id == allowString.IgnoredId)
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        if (context.Channel.Id == allowString.IgnoredId)
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        if (guildUser != null && guildUser.RoleIds.Any(roleId => roleId == allowString.IgnoredId))
                            return context.Message.Content.CompareAllowedNoCase(allowString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return false;
        }

        public static bool CompareAllowedNoCase(this string str, AllowString allowString)
            => str.Equals(allowString.AllowedString, StringComparison.OrdinalIgnoreCase);
    }
}