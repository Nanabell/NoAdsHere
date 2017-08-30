using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Services.FAQ
{
    public static class FaqService
    {
        private static DiscordShardedClient _client;
        private static CommandService _commandService;
        private static IServiceProvider _provider;
        private static IConfigurationRoot _config;
        private static ILogger _logger;

        public static Task Install(IServiceProvider provider)
        {
            _provider = provider;
            _config = provider.GetService<IConfigurationRoot>();
            _client = provider.GetService<DiscordShardedClient>();
            _commandService = new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async });
            _logger = provider.GetService<ILoggerFactory>().CreateLogger(typeof(FaqService));
            return Task.CompletedTask;
        }

        public static async Task LoadFaqs()
        {
            _client.MessageReceived += FaqProccesser;
            await _commandService.AddModuleAsync<FaqCommands>();
        }

        private static async Task FaqProccesser(SocketMessage socketMessage)
        {
            var argPos = 0;
            var message = socketMessage as SocketUserMessage;
            if (message == null)
                return;
            if (!message.HasStringPrefix(_config["Prefixes:Faq"], ref argPos, StringComparison.OrdinalIgnoreCase))
                return;
            var context = new ShardedCommandContext(_client, message);
            if (context.IsPrivate)
                return;
            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _commandService.ExecuteAsync(context, argPos, _provider);

            if (!result.IsSuccess)
                _logger.LogWarning(new EventId(515), result.ErrorReason);
        }
    }

    [DontAutoLoad]
    internal class FaqCommands : ModuleBase
    {
        private readonly IUnitOfWork _unit;
        private readonly IConfigurationRoot _config;

        public FaqCommands(IUnitOfWork unit, IConfigurationRoot config)
        {
            _unit = unit;
            _config = config;
        }

        [Command("faq"), Alias("faqs")]
        [RequirePermission(AccessLevel.User)]
        public async Task Faq([Remainder]string name = null)
        {
            if (name == null)
            {
                var faqs = (await _unit.Faqs.GetAllAsync(Context.Guild)).ToList();
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
                var faq = await _unit.Faqs.GetAsync(Context.Guild, name);

                if (faq != null)
                {
                    await ReplyAsync(faq.Content);
                    faq.LastUsed = DateTime.UtcNow;
                    faq.UseCount++;
                    _unit.SaveChanges();
                }
                else
                {
                    var faqs = await _unit.Faqs.GetAllAsync(Context.Guild);

                    if (faqs.Any())
                    {
                        var similarFaqs = await _unit.Faqs.GetSimilarAsync(_config, Context.Guild, name);

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
    }
}