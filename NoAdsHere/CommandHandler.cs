using Discord.Commands;
using Discord.WebSocket;
using NLog;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NLog.Fluent;
using NoAdsHere.Common;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.Events;
using ParameterInfo = Discord.Commands.ParameterInfo;

namespace NoAdsHere
{
    public static class CommandHandler
    {
        private static IServiceProvider _provider;
        private static CommandService _commands;
        private static DiscordShardedClient _client;
        private static Config _config;
        private static readonly Logger Logger = LogManager.GetLogger("CommandHandler");

        public static Task Install(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordShardedClient>();
            _commands = _provider.GetService<CommandService>();
            _config = _provider.GetService<Config>();

            _commands.Log += EventHandlers.CommandLogger;

            return Task.CompletedTask;
        }

        public static async Task ConfigureAsync()
        {
            Logger.Info("Started MessageReceived Handler");
            _client.MessageReceived += ProccessCommandAsync;
            Logger.Info("Loading Command-Modules from Assembly");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Logger.Info("Started CommandHandler");
        }

        public static Task StopHandler()
        {
            Logger.Info("Unloading Message Handler");
            _client.MessageReceived -= ProccessCommandAsync;

            Logger.Info("Stopped CommandHandler");
            return Task.CompletedTask;
        }

        private static async Task ProccessCommandAsync(SocketMessage pMsg)
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
                        Logger.Debug($"User {context.User} tried to use a unknown command in {context.Guild}/{context.Channel}");
                        return;
                    }
                    response = searchResult.Error.ToString();
                    Logger.Debug($"Failed search result: {searchResult.ErrorReason}");
                    break;

                case ParseResult parseResult:
                    var command = _commands.Search(context, argPos).Commands.First();
                    response = $":warning: There was an error parsing your command: `{parseResult.ErrorReason}`";
                    response +=
                        $"\nCorrect Usage is: `{_config.Prefix.First()}{command.Alias} {string.Join(" ", command.Command.Parameters.Select(FormatParam)).Replace("`", "")}`";
                    break;

                case PreconditionResult preconditionResult:
                    response = $":warning: A precondition of your command failed: `{preconditionResult.ErrorReason}`";

                    break;

                case ExecuteResult executeResult:
                    if (!executeResult.IsSuccess)
                    {
                        response = $":warning: Your command failed to execute. If this persists, contact the bot developer.\n`{executeResult.Exception.Message}`";
                        Logger.Error(executeResult.Exception);
                    }
                    break;

                default:
                    Logger.Debug($"Unknown Result Type: {result?.Error}");
                    break;
            }

            if (response != null)
            {
                await context.Channel.SendMessageAsync(response);
            }
        }

        private static bool ParseTriggers(IUserMessage message, ref int argPos)
            => message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(_config.Prefix, ref argPos);

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