using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NoAdsHere.Database.Entities;

namespace NoAdsHere.Common.Preconditions
{
    public class RequirePermissionAttribute : PreconditionAttribute
    {
        private readonly AccessLevel _level;

        public RequirePermissionAttribute(AccessLevel level)
        {
            _level = level;
        }
        
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var level = await GetLevel(context, services.GetRequiredService<IConfiguration>()).ConfigureAwait(false);
            return level >= _level ? PreconditionResult.FromSuccess() : PreconditionResult.FromError($"Insufficient permissions! Required level: {_level}");        
        }

        private static async Task<AccessLevel> GetLevel(ICommandContext context, IConfiguration config)
        {
            if (context.User.IsBot)
                return AccessLevel.Blocked;

            var application = await context.Client.GetApplicationInfoAsync();
            if (application.Owner.Id == context.User.Id)
                return AccessLevel.God;

            if (config.Get<Config>().Masters.Contains(context.User.Id))
                return AccessLevel.Master;

            if (!(context.User is IGuildUser guildUser)) return AccessLevel.Private;

            if (context.Guild.OwnerId == context.User.Id)
                return AccessLevel.Owner;

            if (guildUser.GuildPermissions.Administrator)
                return AccessLevel.Admin;

            if (guildUser.GuildPermissions.BanMembers)
                return AccessLevel.HighModerator;

            return guildUser.GuildPermissions.ManageMessages ? AccessLevel.Moderator : AccessLevel.User;
        }
    }
}