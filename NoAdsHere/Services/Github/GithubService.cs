using Discord;
using Discord.WebSocket;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoAdsHere.Database;

namespace NoAdsHere.Services.Github
{
    public class GithubService
    {
        private readonly Regex _issueRegex = new Regex(@"##([0-9]+)", RegexOptions.Compiled);
        private readonly DiscordShardedClient _client;

        public GithubService(DiscordShardedClient client, ILogger<GithubService> logger)
        {
            _client = client;
            _client.MessageReceived += ParseMessage;
            logger.LogInformation("Started Github Service");
        }

        private async Task ParseMessage(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id) 
                return;

            if (message.Author is IGuildUser user)
            {
                var matches = _issueRegex.Matches(message.Content);
                if (matches.Count > 0)
                {
                    using (var context = new DatabaseContext(false, true, user.GuildId))
                    {
                        if (context.GuildConfig.GithubRepository == null)
                            return;
                        
                        var outStr = new StringBuilder();
                        foreach (Match match in matches)
                        {
                            outStr.AppendLine($"{match.Value} - {context.GuildConfig.GithubRepository}/issues/{match.Value.Substring(2)}");
                        }
                        await message.Channel.SendMessageAsync(outStr.ToString());
                    }
                }
            }
        }
    }
}