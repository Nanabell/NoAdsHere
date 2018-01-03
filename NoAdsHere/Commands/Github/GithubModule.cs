using Discord.Commands;
using NoAdsHere.Common.Preconditions;
using System;
using System.Threading.Tasks;
using NoAdsHere.Database;

namespace NoAdsHere.Commands.Github
{
    [Name("GithubService"), Alias("Github"), Group("GithubService")]
    public class GithubModule : ModuleBase
    {
        [Command("Add")]
        [RequirePermission(Common.AccessLevel.HighModerator)]
        public async Task Setup([Remainder] string repo)
        {
            if (repo.StartsWith("https://github.com/", StringComparison.Ordinal))
            {
                using (var dbContext = new DatabaseContext(false, true, Context.Guild.Id))
                {
                    dbContext.GuildConfig.GithubRepository = repo.Split(" ")[0];
                    dbContext.SaveChanges();
                    await ReplyAsync($"Repo has been set to <{dbContext.GuildConfig.GithubRepository}>");                    
                }
                
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
            using (var dbContext = new DatabaseContext(false, true, Context.Guild.Id))
            {
                if (dbContext.GuildConfig.GithubRepository != null)
                {
                    dbContext.GuildConfig.GithubRepository = null;
                    dbContext.SaveChanges();
                    await ReplyAsync(":ok_hand:");
                }
                else
                    await ReplyAsync("No Github Repo set");
            }
        }
    }
}