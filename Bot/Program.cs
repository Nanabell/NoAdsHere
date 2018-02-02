using System;
using System.IO;
using System.Threading.Tasks;
using Bot.Services;
using Database;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Bot
{
    internal class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static void Main() => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            Logger.Info("Hello World!");

            Logger.Info("Loading Configuration");
            var config = CreateConfiguration();

            Logger.Info("Loading Service Provider");
            var provider = CreateProvider(config);

            var client = provider.GetRequiredService<DiscordShardedClient>();
            await client.LoginAsync(TokenType.Bot, config.Get<Config>().Token);
            await client.StartAsync();

            var cmdHandler = provider.GetService<CommandHandler>();
            await cmdHandler.StartAsync();

            var inviteService = provider.GetService<InviteModeration>();
            inviteService.Start();
            
            await Task.Delay(-1);
        }

        private static IServiceProvider CreateProvider(IConfiguration configuration)
        {
            var config = configuration.Get<Config>();
            return new ServiceCollection()
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
                {
                    TotalShards = config.Shards,
                    MessageCacheSize = 50,
                    LogLevel = LogSeverity.Debug
                    
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
#if DEBUG
                    DefaultRunMode = RunMode.Sync,
#else
                    DefaultRunMode = RunMode.Async,
#endif
                    
                    LogLevel = LogSeverity.Debug,
                    ThrowOnError = true
                }))
                .AddSingleton(new DatabaseContext(config.DatabaseType, config.ConnectionString))
                .AddSingleton<CommandHandler>()
                .AddSingleton<InviteModeration>()
                .AddSingleton(configuration)
                .BuildServiceProvider();
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile("config.yaml", false, true)
                .Build();
        }
    }
}