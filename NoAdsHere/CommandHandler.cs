using Discord.Commands;
using Discord.WebSocket;
using NLog;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Common;
using NoAdsHere.Services.Configuration;

namespace NoAdsHere
{
    public static class CommandHandler
    {
        private static IServiceProvider _provider;
        private static CommandService _commands;
        private static DiscordSocketClient _client;
        private static Config _config;
        private static readonly Logger Logger = LogManager.GetLogger("CommandHandler");

        public static Task Install(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _client.MessageReceived += ProccessCommandAsync;
            _commands = _provider.GetService<CommandService>();
            _config = _provider.GetService<Config>();

            _commands.Log += CommandLogger;

            return Task.CompletedTask;
        }

        private static Task CommandLogger(LogMessage message)
        {
            var logger = LogManager.GetLogger("Command");
            if (message.Exception == null)
                logger.Info(message.Message);
            else
                logger.Warn(message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public static async Task ConfigureAsync()
        {
            Logger.Info("Command service started.");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static Task StopHandler()
        {
            _client.MessageReceived -= ProccessCommandAsync;
            return Task.CompletedTask;
        }

        private static async Task ProccessCommandAsync(SocketMessage pMsg)
        {
            var message = pMsg as SocketUserMessage;
            if (message == null) return;

            var argPos = 0;
            if (!ParseTriggers(message, ref argPos)) return;

            var context = new SocketCommandContext(_client, message);

            if (context.IsPrivate) return;

            if (!context.Channel.CheckChannelPermission(ChannelPermission.SendMessages, context.Guild.CurrentUser))
                return;

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            string response = null;
            switch (result)
            {
                case SearchResult searchResult:
                    if (searchResult.ErrorReason == "Unknown command")
                    {
                        Logger.Debug($"User {context.User} tried to use a unknown command in {context.Guild}/{context.Channel}");
                        return;
                    }
                    response = searchResult.Error.ToString();
                    Logger.Debug($"Failed search result: {searchResult.ErrorReason}");
                    break;

                case ParseResult parseResult:
                    response = $":warning: There was an error parsing your command: `{parseResult.ErrorReason}`";
                    break;

                case PreconditionResult preconditionResult:
                    response = $":warning: A precondition of your command failed: `{preconditionResult.ErrorReason}`";
                    break;

                case ExecuteResult executeResult:
                    response = $":warning: Your command failed to execute. If this persists, contact the bot developer.\n`{executeResult.Exception.Message}`";
                    Logger.Error(executeResult.Exception);
                    break;

                default:
                    Logger.Debug($"Unknown Result Type: {result?.Error}");
                    break;
            }

            if (response != null)
                await context.Channel.SendMessageAsync(response);
        }

        private static bool ParseTriggers(IUserMessage message, ref int argPos)
        {
            var flag = false;
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos)) flag = true;
            else
            {
                foreach (var prefix in _config.CommandStrings)
                {
                    if (!message.HasStringPrefix(prefix, ref argPos)) continue;
                    flag = true;
                    break;
                }
            }
            return flag;
        }
    }
}