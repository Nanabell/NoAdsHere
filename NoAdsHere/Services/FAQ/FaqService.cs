using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;

namespace NoAdsHere.Services.FAQ
{
    public static class FaqService
    {
        private static DiscordShardedClient _client;
        private static CommandService _commandService;
        private static IServiceProvider _provider;
        private static readonly Logger Logger = LogManager.GetLogger("FAQ");

        public static Task Install(IServiceProvider provider)
        {
            _provider = provider;
            _client = provider.GetService<DiscordShardedClient>();
            _commandService = new CommandService(new CommandServiceConfig {DefaultRunMode = RunMode.Async});
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
            if (!message.HasStringPrefix("?fa", ref argPos))
                return;
            var context = new ShardedCommandContext(_client, message);
            if (context.IsPrivate)
                return;
            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _commandService.ExecuteAsync(context, argPos, _provider);
            
            if (!result.IsSuccess)
                Logger.Warn(result);
        }
    }

    internal class FaqCommands : ModuleBase
    {
        private readonly FaqSystem _faqSystem;
        
        public FaqCommands(FaqSystem faqSystem)
        {
            _faqSystem = faqSystem;
        }
        
        [Command("q")]
        [RequirePermission(AccessLevel.User)]
        public async Task Faq([Remainder]string name = null)
        {
            if (name == null)
            {
                var globals = await _faqSystem.GetGlobalEntriesAsync();
                var locals = await _faqSystem.GetGuildEntriesAsync(Context.Guild.Id);
                var response = "**Frequently Asked Questions:**";

                if (globals.Any() || locals.Any())
                {
                    if (globals.Any())
                    {
                        response += "\n*Global FAQ's:*";
                        response += "\n`" + string.Join("`", globals.Select(f => f.Name)) + "`";
                        response += "\n";
                    }
                    if (locals.Any())
                    {
                        response += "\n*Guild FAQ's:*";
                        response += "\n`" + string.Join("`", locals.Select(f => f.Name)) + "`";
                    }
                    await ReplyAsync(response);
                }
                else
                {
                    await ReplyAsync("Currently no available FAQ's");
                }
            }
            else
            {
                var gEntry = await _faqSystem.GetGlobalFaqEntryAsync(name);
                var lEntry = await _faqSystem.GetGuildFaqEntryAsync(Context.Guild.Id, name);

                if (gEntry != null)
                {
                    await ReplyAsync(gEntry.Content);   
                    gEntry.LastUsed = DateTime.UtcNow;
                    gEntry.UseCount++;
                    await _faqSystem.SaveGlobalEntryAsync(gEntry);
                }
                else if (lEntry != null)
                {
                    await ReplyAsync(lEntry.Content);   
                    lEntry.LastUsed = DateTime.UtcNow;
                    lEntry.UseCount++;
                    await _faqSystem.SaveGuildEntryAsync(lEntry);
                }
                 
                else
                {
                    var globals = await _faqSystem.GetGlobalEntriesAsync();
                    var locals = await _faqSystem.GetGuildEntriesAsync(Context.Guild.Id);

                    if (globals.Any() || locals.Any())
                    {
                        var gSimilar = await _faqSystem.GetSimilarGlobalEntries(name);
                        var lSimilar = await _faqSystem.GetSimilarGuildEntries(Context.Guild.Id, name);

                        if (gSimilar.Any() || lSimilar.Any())
                        {
                            var response = $"No FAQ Entry with the name `{name}` found. Did you mean:";

                            if (gSimilar.Any())
                            {
                                response += "\n*Global FAQ's:*";
                                response += "\n`" + string.Join("`", gSimilar.Select(pair => pair.Key.Name)) + "`";
                            }
                            if (lSimilar.Any())
                            {
                                response += "\n*Guild FAQ's:*";
                                response += "\n`" + string.Join("`", lSimilar.Select(pair => pair.Key.Name)) + "`";
                            }
                            await ReplyAsync(response);
                        }
                        else
                            await ReplyAsync($"No FAQ entry with the name {name} found.");
                    }
                    else
                        await ReplyAsync($"No FAQ entry with the name {name} found.");
                }
            }
        }
    }
}