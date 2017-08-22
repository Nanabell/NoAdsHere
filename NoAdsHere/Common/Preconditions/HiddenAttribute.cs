using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Common.Preconditions
{
    public class HiddenAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services) => Task.FromResult(PreconditionResult.FromSuccess());
    }
}