using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NoAdsHere.Database.Models.Guild;

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
            => ignores.Where(i => i.IgnoreType == type);

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

        public static bool CheckAllowedStrings(this IEnumerable<AllowString> allowStrings, ITextChannel channel, IGuildUser user, string message)
        {
            foreach (var allowString in allowStrings)
            {
                switch (allowString.IgnoreType)
                {
                    case IgnoreType.User:
                        if (user.Id == allowString.IgnoredId)
                            return message.CompareAllowedNoCase(allowString);
                        break;

                    case IgnoreType.Role:
                        if (user.RoleIds.Any(roleId => roleId == allowString.IgnoredId))
                            return message.CompareAllowedNoCase(allowString);
                        break;

                    case IgnoreType.Channel:
                        if (channel.Id == allowString.IgnoredId)
                            return message.CompareAllowedNoCase(allowString);
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