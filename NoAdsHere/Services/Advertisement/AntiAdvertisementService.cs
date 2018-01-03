using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NoAdsHere.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NoAdsHere.Database;
using NoAdsHere.Database.Entities;

namespace NoAdsHere.Services.Advertisement
{
    public class AntiAdvertisementService
    {
        private readonly DiscordShardedClient _client;
        private readonly ILogger<AntiAdvertisementService> _logger;

        public event Func<SocketCommandContext, AntiAd, Task> AdvertisementReceived;
        
        public AntiAdvertisementService(DiscordShardedClient client, ILogger<AntiAdvertisementService> logger)
        {
            _client = client;
            _logger = logger;

            _client.MessageReceived += AdHandler;
            _client.MessageUpdated += AdUpdateHandler;
            
            _logger.LogInformation("Started Ad Handlers");
            _logger.LogInformation("Started AntiAdvertisement Service");
        }

        private Task AdUpdateHandler(Cacheable<IMessage, ulong> cacheable, SocketMessage socketMessage, ISocketMessageChannel arg3)
            => AdHandler(socketMessage);

        private async Task AdHandler(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message))
                return;

            var context = new ShardedCommandContext(_client, message);
            if (context.IsPrivate || context.User.IsBot)
                return;
            var guildUser = (SocketGuildUser) context.User;
            
            if (!context.Channel.CheckChannelPermission(ChannelPermission.ManageMessages, context.Guild.CurrentUser))
                return;

            using (var dbcContext = new DatabaseContext(loadGuildConfig: true, guildId: context.Guild.Id, createNewFile: false))
            {
                if (!dbcContext.GuildConfig.AntiAds.Any())
                    return;

                if (CheckGuildUser(guildUser, dbcContext.GuildConfig.GlobalWhitelist))
                    return;
                
                var rawMessage = Regex.Replace(context.Message.Content, @"[\u005C\u007F-\uFFFF\s]+", string.Empty);

                foreach (var ad in dbcContext.GuildConfig.AntiAds.OrderByDescending(ad => ad.Priority))   
                {
                    if (!ad.AdRegex.IsMatch(rawMessage)) 
                        continue;
                    
                    if (CheckGuildUser(guildUser, ad.Whitelist))
                        return;

                    try
                    {
                        await context.Message.DeleteAsync();
                        _logger.LogInformation(
                            $"Deleted Advertisement by {context.User} in {context.Guild}/{context.Channel}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e,
                            $"Failed to delete Advertisement in message {context.Message} in {context.Guild}/{context.Channel}");
                    }
                    finally
                    {
                        if (AdvertisementReceived != null) 
                            await AdvertisementReceived(context, ad);
                    }
                }
            }
        }

        private static bool CheckGuildUser(IGuildUser guildUser, ICollection<ulong> list) 
            => list.Contains(guildUser.Id) || guildUser.RoleIds.Any(list.Contains);


    }
}