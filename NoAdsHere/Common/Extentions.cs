using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NoAdsHere.Common
{
    public static class Extentions
    {
        public static bool CheckChannelPermission(this IMessageChannel channel, ChannelPermission permission, IGuildUser guildUser)
        {
            var guildchannel = channel as IGuildChannel;

            ChannelPermissions perms;
            perms = guildchannel != null ? guildUser.GetPermissions(guildchannel) : ChannelPermissions.All(null);

            return perms.Has(permission);
        }

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

        /// <summary>
        /// Parse a string into an <see cref="BooleanAliases"/> and that into a bool
        /// </summary>
        /// <param name="str">The string to be parsed</param>
        /// <returns>True or false. Falls back to false if parse fails!</returns>
        public static bool ParseStringToBool(this string str)
        {
            try
            {
                return Convert.ToBoolean(Enum.Parse(typeof(BooleanAliases), str));
            }
            catch
            {
                return false;
            }
        }
    }
}