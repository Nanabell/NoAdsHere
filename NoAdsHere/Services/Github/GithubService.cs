using Discord;
using Discord.WebSocket;
using NoAdsHere.Database.UnitOfWork;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Github
{
    public class GithubService
    {
        private readonly Regex _issueRegex = new Regex(@"##([0-9]+)", RegexOptions.Compiled);
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _unit;

        public GithubService(DiscordShardedClient client, IUnitOfWork unit)
        {
            _client = client;
            _unit = unit;
        }

        internal Task StartAsync()
        {
            _client.MessageReceived += ParseMessage;
            return Task.CompletedTask;
        }

        internal Task StopAsync()
        {
            _client.MessageReceived -= ParseMessage;
            return Task.CompletedTask;
        }

        private async Task ParseMessage(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id) return;

            if (message.Author is IGuildUser user)
            {
                var matches = _issueRegex.Matches(message.Content);
                if (matches.Count > 0)
                {
                    var settings = await _unit.Settings.GetAsync(user.Guild);

                    if (settings?.GithubRepo == null) return;

                    var outStr = new StringBuilder();
                    foreach (Match match in matches)
                    {
                        outStr.AppendLine($"{match.Value} - {settings.GithubRepo}/issues/{match.Value.Substring(2)}");
                    }
                    await message.Channel.SendMessageAsync(outStr.ToString());
                }
            }
        }
    }
}