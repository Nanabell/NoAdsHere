using Discord.Commands;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.Github
{
    [Name("GithubService"), Alias("Github"), Group("GithubService")]
    public class GithubModule : ModuleBase
    {
        private readonly IUnitOfWork _unit;

        public GithubModule(IUnitOfWork unit)
        {
            _unit = unit;
        }

        [Command("Add")]
        [RequirePermission(Common.AccessLevel.HighModerator)]
        public async Task Setup([Remainder] string repo)
        {
            if (repo.StartsWith("https://github.com/", StringComparison.Ordinal))
            {
                var settings = await _unit.Settings.GetOrCreateAsync(Context.Guild);
                settings.GithubRepo = repo.Split(" ")[0];
                _unit.SaveChanges();
                await ReplyAsync($"Repo has been set to <{settings.GithubRepo}>");
            }
            else
            {
                await ReplyAsync("This does not seem like a valid Github Repo");
            }
        }

        [Command("Remove")]
        [RequirePermission(Common.AccessLevel.HighModerator)]
        public async Task Remove()
        {
            var settings = await _unit.Settings.GetAsync(Context.Guild);
            if (settings != null)
            {
                settings.GithubRepo = null;
                _unit.SaveChanges();
                await ReplyAsync(":ok_hand:");
            }
            else
                await ReplyAsync("No Github Repo set");
        }
    }
}