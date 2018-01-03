using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace NoAdsHere.Services.Events
{
    public class EventLogger
    {
        private readonly DiscordShardedClient _client;
        private readonly ILogger<EventLogger> _logger;

        public EventLogger(DiscordShardedClient client, CommandService service, ILogger<EventLogger> logger)
        {
            _client = client;
            _logger = logger;

            _client.ShardReady += ShardReady;
            _client.Log += ClientLog;
            
            service.Log += CommandServiceLog;
            
            _logger.LogInformation("Started EventLogger");
        }
        
        private Task ClientLog(LogMessage logMessage)
        {
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(logMessage.Exception, logMessage.Message);
                    break;

                case LogSeverity.Error:
                    _logger.LogError(logMessage.Exception, logMessage.Message);
                    break;

                case LogSeverity.Warning:
                    _logger.LogWarning(logMessage.Exception, logMessage.Message);
                    break;

                case LogSeverity.Info:
                    _logger.LogInformation(logMessage.Exception, logMessage.Message);
                    break;

                case LogSeverity.Verbose:
                    _logger.LogDebug(logMessage.Exception, logMessage.Message);
                    break;

                case LogSeverity.Debug:
                    _logger.LogTrace(logMessage.Exception, logMessage.Message);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return Task.CompletedTask;
        }

        private Task CommandServiceLog(LogMessage logMessage)
        {
            _logger.LogInformation(logMessage.Exception, logMessage.Message);
            return Task.CompletedTask;
        }

        private Task ShardReady(DiscordSocketClient client)
        {
            //TODO: IMPLEMENT WEBHOOK LOGGING
            _logger.LogInformation($"[{client.ShardId}/{_client.Shards.Count}] Ready");
            return Task.CompletedTask;
        }
    }
}