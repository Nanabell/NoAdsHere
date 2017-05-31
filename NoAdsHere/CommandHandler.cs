using Discord.Commands;
using Discord.WebSocket;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using System.Linq;
using NoAdsHere.Common;
using NoAdsHere.Services.Configuration;

namespace NoAdsHere
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly Config _config;
        private readonly Logger _logger = LogManager.GetLogger("CommandHandler");
        private readonly Logger _discordLogger = LogManager.GetLogger("Command");

        private IEnumerable<ulong> Whitelist => _config.ChannelWhitelist;

        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _client.MessageReceived += ProccessCommandAsync;
            _commands = _provider.GetService<CommandService>();
            _config = _provider.GetService<Config>();

            _commands.Log += CommandLogger;
        }

        private Task CommandLogger(LogMessage message)
        {
            if (message.Exception == null)
                _discordLogger.Log(Program.LogLevelParser(message.Severity), message.Message);
            else
                _discordLogger.Log(Program.LogLevelParser(message.Severity), message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public async Task ConfigureAsync()
        {
            _logger.Info("CommandSerive Started");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task ProccessCommandAsync(SocketMessage pMsg)
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
                    _logger.Debug($"SearchResult: {searchResult.ErrorReason}");
                    break;

                case ParseResult parseResult:
                    response = $":warning: There was an error parsing your command: `{parseResult.ErrorReason}`";
                    break;

                case PreconditionResult preconditionResult:
                    response = $":warning: A precondition of your command failed: `{preconditionResult.ErrorReason}`";
                    break;

                case ExecuteResult executeResult:
                    response = $":warning: Your command failed to execute. If this persists, contact the Bot Developer.\n`{executeResult.Exception.Message}`";
                    _logger.Error(executeResult.Exception);
                    break;

                default:
                    _logger.Debug($"Unknown Result Type: {result?.Error}");
                    break;
            }

            if (response != null)
                await context.Channel.SendMessageAsync(response);
        }

        private bool ParseTriggers(SocketUserMessage message, ref int argPos)
        {
            bool flag = false;
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos)) flag = true;
            else
            {
                foreach (var prefix in _config.CommandStrings)
                {
                    if (message.HasStringPrefix(prefix, ref argPos))
                    {
                        flag = true;
                        break;
                    }
                }
            }
            return flag /*? Whitelist.Any(id => id == message.Channel.Id) : false*/;
        }
    }
}