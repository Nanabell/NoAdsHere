using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoAdsHere.Database;
using NoAdsHere.Database.Entities;

namespace NoAdsHere.Services.FAQ
{
    public class FaqService
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _commandService;
        private readonly IConfigurationRoot _config;
        private readonly ILogger<FaqService> _logger;

        public FaqService(DiscordShardedClient client, IConfigurationRoot configuration, ILogger<FaqService> logger)
        {
            _client = client;
            _commandService = new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async });
            _config = configuration;
            _logger = logger;
            
            _client.MessageReceived += FaqParser;
            _commandService.AddModuleAsync<FaqCommands>().GetAwaiter().GetResult();
            logger.LogInformation("Started Faq Service");
        }

        private async Task FaqParser(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (!(socketMessage is SocketUserMessage message))
                return;
            
            if (!message.HasStringPrefix(_config.Get<Config>().Prefix.Faq, ref argPos, StringComparison.OrdinalIgnoreCase))
                return;
            
            var context = new ShardedCommandContext(_client, message);
            if (context.IsPrivate)
                return;
            
            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _commandService.ExecuteAsync(context, argPos);

            if (!result.IsSuccess)
            {
                if (message.HasStringPrefix(_config.Get<Config>().Prefix.Main, ref argPos, StringComparison.OrdinalIgnoreCase))
                    return;
                _logger.LogWarning(result.ErrorReason);
            }
        }
    }

    [DontAutoLoad]
    internal class FaqCommands : ModuleBase
    {
        [Command("faq"), Alias("faqs")]
        [RequirePermission(AccessLevel.User)]
        public async Task Faq([Remainder]string name = null)
        {
            var dbContext = new DatabaseContext(loadGuildConfig: true, guildId: Context.Guild.Id, createNewFile: false);
            
            if (name == null)
            {
                var faqs = dbContext.GuildConfig.Faqs;
                var sb = new StringBuilder();
                sb.AppendLine("**Frequently Asked Questions:**");

                if (faqs.Any())
                {
                    sb.AppendLine(string.Join(" ", faqs.Select(f => "`" + f.Name + "`")));
                    await ReplyAsync(sb.ToString());
                }
                else
                {
                    await ReplyAsync("Currently no FAQ's available");
                }
            }
            else
            {
                name = name.ToLower();
                var faq = dbContext.GuildConfig.Faqs.FirstOrDefault(faqEntry =>
                    faqEntry.GuildId == Context.Guild.Id && faqEntry.Name == name);

                if (faq != null)
                {
                    await ReplyAsync(faq.Content);
                    faq.LastUsed = DateTime.UtcNow;
                    faq.Uses++;
                    dbContext.SaveChanges();
                }
                else
                {
                    var faqs = dbContext.GuildConfig.Faqs;

                    if (faqs.Any())
                    {
                        var similarFaqs = GetSimilarFaqs(faqs, name);

                        if (similarFaqs.Any())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"No FAQ Entry with the name `{name}` found. Did you mean:");
                            if (similarFaqs.Any())
                                sb.AppendLine(string.Join(" ", similarFaqs.Select(pair => "`" + pair.Key.Name + "`")));

                            await ReplyAsync(sb.ToString());
                        }
                        else
                            await ReplyAsync($"No FAQ entry with the name {name} found.");
                    }
                    else
                        await ReplyAsync($"No FAQ entries existing.");
                }
            }
        }

        private static Dictionary<Faq, int> GetSimilarFaqs(IEnumerable<Faq> faqs, string name)
        {
            var faqDictionary = faqs.ToDictionary(faq => faq, faq => LevenshteinDistance.Compute(name, faq.Name));
            return faqDictionary
                .Where(pair => pair.Value <= 4)
                .OrderBy(pair => pair.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}