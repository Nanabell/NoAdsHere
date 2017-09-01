using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ParameterInfo = Discord.Commands.ParameterInfo;

namespace NoAdsHere
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private readonly ILogger _logger;

        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = provider.GetService<DiscordShardedClient>();
            _commands = provider.GetService<CommandService>();
            _config = provider.GetService<IConfigurationRoot>();
            _logger = provider.GetService<ILoggerFactory>().CreateLogger<CommandHandler>();
        }

        public async Task LoadModulesAndStartAsync()
        {
            _client.MessageReceived += ProccessCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public Task StopHandler()
        {
            _client.MessageReceived -= ProccessCommandAsync;
            _logger.LogInformation(new EventId(200), "CommandHandler stopped successfully");
            return Task.CompletedTask;
        }

        private async Task ProccessCommandAsync(SocketMessage pMsg)
        {
            var message = pMsg as SocketUserMessage;
            if (message == null) return;

            var argPos = 0;
            if (!ParseTriggers(message, ref argPos)) return;

            var context = new ShardedCommandContext(_client, message);
            if (context.IsPrivate)
                return;

            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            string response = null;
            switch (result)
            {
                case SearchResult searchResult:
                    if (searchResult.Error == CommandError.UnknownCommand)
                    {
                        _logger.LogDebug(new EventId(404), $"User {context.User} tried to use a unknown command in {context.Guild}/{context.Channel}");
                        return;
                    }
                    response = searchResult.Error.ToString();
                    _logger.LogInformation(new EventId(501), $"Failed search result: {searchResult.ErrorReason}");
                    break;

                case ParseResult parseResult:
                    var command = _commands.Search(context, argPos).Commands.First();
                    response = $":warning: There was an error parsing your command: `{parseResult.ErrorReason}`";
                    response +=
                        $"\nCorrect Usage is: `{_config["Prefixes:Main"]} {command.Alias} {string.Join(" ", command.Command.Parameters.Select(FormatParam)).Replace("`", "")}`";
                    break;

                case PreconditionResult preconditionResult:
                    response = $":warning: A precondition of your command failed: `{preconditionResult.ErrorReason}`";

                    break;

                case ExecuteResult executeResult:
                    if (!executeResult.IsSuccess)
                    {
                        response = $":warning: Your command failed to execute. If this persists, contact the bot developer.\n`{executeResult.Exception?.Message ?? executeResult.ErrorReason}`";
                        _logger.LogError(new EventId(500), executeResult.Exception, executeResult.ErrorReason);
                    }
                    break;

                default:
                    _logger.LogWarning(new EventId(404), $"Unknown Result Type: {result?.Error}");
                    break;
            }

            if (response != null)
            {
                await context.Channel.SendMessageAsync(response);
            }
        }

        private bool ParseTriggers(IUserMessage message, ref int argPos)
            => message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(_config["Prefixes:Main"] + " ", ref argPos);

        private static string FormatParam(ParameterInfo parameter)
        {
            var sb = new StringBuilder();
            if (parameter.IsMultiple)
            {
                sb.Append($"`[{parameter.Name}...]`");
            }
            else if (parameter.IsRemainder)
            {
                sb.Append($"`<{parameter.Name}...>`");
            }
            else if (parameter.IsOptional)
            {
                sb.Append($"`[{parameter.Name}]`");
            }
            else
            {
                sb.Append($"`<{parameter.Name}>`");
            }

            if (!string.IsNullOrWhiteSpace(parameter.Summary))
            {
                sb.Append($" ({parameter.Summary})");
            }
            return sb.ToString();
        }
    }
}