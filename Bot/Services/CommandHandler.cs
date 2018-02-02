using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bot.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Bot.Services
{
    public class CommandHandler
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        
        private readonly IServiceProvider _provider;
        private readonly DiscordShardedClient _client;
        private readonly CommandService _cmdService;
        private readonly Config _config;

        public CommandHandler(IServiceProvider provider, DiscordShardedClient client, CommandService cmdService, IConfiguration config)
        {
            _provider = provider;
            _client = client;
            _cmdService = cmdService;
            _config = config.Get<Config>();
        }

        public async Task StartAsync()
        {
            

            var modules = await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly());
            _logger.Info("Discovered {0} module/s", modules.Count());
            
            _logger.Info("Starting Command Message Handler");
            _client.MessageReceived += MessageHandler;
            
            _logger.Info("Starting Command Result Handler");
            _cmdService.CommandExecuted += ExecutedHandler;
        }

        private async Task MessageHandler(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message))
                return;
            
            var context = new ShardedCommandContext(_client, message);
            var argPos = 0;
            
            if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos) && !message.HasStringPrefix(_config.Prefix, ref argPos))
                return;
            
            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _cmdService.ExecuteAsync(context, argPos, _provider, MultiMatchHandling.Best);

            if (!result.IsSuccess)
            {
                _logger.Debug("Execution of command failed. guild/channel:{0}/{1} user:{2} reason:{3}", 
                    context.Guild, 
                    context.Channel, 
                    context.User, 
                    result);

                var sResult = _cmdService.Search(context, argPos);
                if (sResult.Commands.Count > 0)
                    await ExecutedHandler(sResult.Commands.First().Command, context, result);
            }
        }
        
        private async Task ExecutedHandler(CommandInfo commandInfo, ICommandContext commandContext, IResult result)
        {
            switch (result)
            {
                case ExecuteResult executeResult:
                    //TODO: Add a metrics kind of thing once database is in place
                    
                    if (executeResult.Exception != null)
                    {
                        _logger.Error(executeResult.Exception, "Execution of {0} erroed in guild {1}/{2] by {3}", 
                            commandInfo.Name, 
                            commandContext.Guild, 
                            commandContext.Channel, 
                            commandContext.User);

                        await commandContext.Channel.SendMessageAsync(
                            $"*The Command Failed to execute {new Emoji(":anger:")}*\n"
                            + $"{executeResult.Exception.StackTrace.LimitLines(5)}");
                    }
                    
                    break;
                case ParseResult parseResult:
                    _logger.Debug("Parsing of {0} failed in guild {1}/{2} by {3}. {4}",
                        commandInfo.Name,
                        commandContext.Guild,
                        commandContext.Channel,
                        commandContext.User,
                        parseResult.ErrorReason);

                    await commandContext.Channel.SendMessageAsync($"The Command Parsing failed {new Emoji(":anger:")}\n"
                                                                  + $"{parseResult.ErrorReason}");
                    
                    break;
                case PreconditionResult preconditionResult:
                    _logger.Debug("Precondition '{0}' of {1} failed in guild {2]/{3} by {4}",
                        preconditionResult.ErrorReason,
                        commandInfo.Name,
                        commandContext.Guild,
                        commandContext.Channel,
                        commandContext.User);

                    await commandContext.Channel.SendMessageAsync($"A Precondition to this Command failed\n"
                                                                  + $"{preconditionResult.ErrorReason}");
                    
                    break;
                case RuntimeResult runtimeResult:
                    break;
                case SearchResult searchResult:
                    break;
                case TypeReaderResult typeReaderResult:
                    break;
            }
        }
    }
}