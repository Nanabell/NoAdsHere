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
    }
}