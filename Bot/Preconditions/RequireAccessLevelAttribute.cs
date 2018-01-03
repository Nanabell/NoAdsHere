using System;
using System.Threading.Tasks;
using Bot.Common;
using Discord;
using Discord.Commands;

namespace Bot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAccessLevelAttribute : PreconditionAttribute
    {
        private readonly AccessLevel _level;

        public RequireAccessLevelAttribute(AccessLevel level)
        {
            _level = level;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var level = await GetAccessLevel(context);

            return level >= _level
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"Insufficient AccessLevel: Required: {_level}");
        }

        private static async Task<AccessLevel> GetAccessLevel(ICommandContext context)
        {
            var app = await context.Client.GetApplicationInfoAsync();

            if (app.Owner.Id == context.User.Id)
                return AccessLevel.BotOwner;

            // TODO: Implement Blocked Level
            if (false)
                return AccessLevel.Blocked;
            
            if (context.User.IsBot)
                return AccessLevel.Bot;

            if (!(context.User is IGuildUser guildUser)) 
                return AccessLevel.User;
            
            if (guildUser.Id == context.Guild.OwnerId)
                return AccessLevel.Owner;

            if (guildUser.GuildPermissions.BanMembers)
                return AccessLevel.Admin;

            if (guildUser.GuildPermissions.KickMembers)
                return AccessLevel.Moderator;

            return AccessLevel.User;
        }
        
    }
}