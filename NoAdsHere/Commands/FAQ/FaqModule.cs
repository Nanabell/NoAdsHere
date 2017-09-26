using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.FAQ
{
    [Name("FAQ"), Group("FAQ")]
    public class FaqModule : ModuleBase
    {
        private readonly DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private InteractiveService _interactiveService;
        private readonly IUnitOfWork _unit;

        public FaqModule(IUnitOfWork unit, DiscordShardedClient client, IConfigurationRoot config)
        {
            _unit = unit;
            _client = client;
            _config = config;
        }

        [Command("Add", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task Add([Remainder] string name = null)
        {
            _interactiveService = new InteractiveService(_client.GetShardFor(Context.Guild));
            var toDelete = new List<IMessage>();
            var header = await ReplyAsync("**FAQ Entry Setup:** *(reply with cancel to cancel at any time)*");
            IUserMessage msg = await ReplyAsync(".");
            IUserMessage nameMessage = null;
            if (name == null)
            {
                await msg.ModifyAsync(properties => properties.Content = $"{Context.User.Mention} Please give a name for the new FAQ entry!");
                nameMessage = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));
                name = nameMessage.Content;
                if (name.ToLower() == "cancel")
                {
                    toDelete.Add(Context.Message);
                    toDelete.Add(header);
                    toDelete.Add(msg);
                    toDelete.Add(nameMessage);
                    await TryDeleteBatchAsync(Context.Channel, Context.User as IGuildUser, toDelete);
                    return;
                }
            }

            await msg.ModifyAsync(properties => properties.Content = $"{Context.User.Mention} Please reply with the whole Message for the FAQ response!");
            var contentMessage = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(90));
            var content = contentMessage.Content;
            if (content.ToLower() == "cancel")
            {
                toDelete.Add(Context.Message);
                toDelete.Add(header);
                toDelete.Add(msg);
                if (nameMessage != null)
                    toDelete.Add(nameMessage);
                toDelete.Add(contentMessage);
                await TryDeleteBatchAsync(Context.Channel, Context.User as IGuildUser, toDelete);
                return;
            }

            await msg.ModifyAsync(properties =>
            {
                properties.Content =
                    $"{Context.User.Mention} Is this alright ? *(yes to accept, annything else to cancel)*";
                properties.Embed = new EmbedBuilder { Title = $"New FAQ Entry: {name}", Description = content }.Build();
            });
            var confirmMsg = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));
            if (confirmMsg.Content.ToLower() == "yes")
            {
                toDelete.Add(header);
                if (nameMessage != null)
                    toDelete.Add(nameMessage);
                toDelete.Add(contentMessage);
                toDelete.Add(confirmMsg);
                await TryDeleteBatchAsync(Context.Channel, Context.User as IGuildUser, toDelete);

                var faq = await _unit.Faqs.GetAsync(Context.Guild, name.ToLower());
                if (faq == null)
                {
                    await _unit.Faqs.AddAsync(new Faq
                    {
                        GuildId = Context.Guild.Id,
                        Name = name.ToLower(),
                        Content = content,
                        CreatorId = Context.User.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastUsed = DateTime.MinValue,
                        UseCount = 0
                    });
                    _unit.SaveChanges();

                    await msg.ModifyAsync(p => p.Content = ":ok_hand:");
                }
                else
                {
                    await msg.ModifyAsync(p => p.Content = $":exclamation: Faq entry with the name `{name}` already existing");
                }
            }
            else
            {
                toDelete.Add(Context.Message);
                toDelete.Add(header);
                toDelete.Add(msg);
                if (nameMessage != null)
                    toDelete.Add(nameMessage);
                toDelete.Add(contentMessage);
                toDelete.Add(confirmMsg);
                await TryDeleteBatchAsync(Context.Channel, Context.User as IGuildUser, toDelete);
            }
        }

        [Command("Remove", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task Remove([Remainder] string name)
        {
            _interactiveService = new InteractiveService(_client.GetShardFor(Context.Guild));
            var faq = await _unit.Faqs.GetAsync(Context.Guild, name.ToLower());

            if (faq != null)
            {
                var msg = await ReplyAsync(
                    $"Please confirm removal of FAQ entry `{faq.Name}` *(yes to accept, annything else to cancel)*");
                var responseMsg = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));

                if (responseMsg.Content.ToLower() == "yes")
                {
                    _unit.Faqs.Remove(faq);
                    _unit.SaveChanges();

                    await TryDeleteAsync(responseMsg);
                    await msg.ModifyAsync(p => p.Content = ":ok_hand:");
                }
                else
                {
                    await msg.DeleteAsync();
                }
            }
            else
            {
                var similar = await _unit.Faqs.GetSimilarAsync(_config, Context.Guild, name);
                if (similar.Any())
                    await ReplyAsync($"No FAQ Entry with the name `{name}` found. Did you mean:\n" + string.Join(" ", similar.Select(pair => "`" + pair.Key.Name + "`")));
                else
                    await ReplyAsync($"No FAQ entry with the name {name} found.");
            }
        }

        private static async Task TryDeleteAsync(IMessage message)
        {
            try
            {
                await message.DeleteAsync();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("FAQ").Warn(e, $"Unable to delete message {message.Id} from {message.Author} in {(message.Author as IGuildUser)?.Guild}/{message.Channel}");
            }
        }

        private static async Task TryDeleteBatchAsync(IMessageChannel channel, IGuildUser guildUser, IEnumerable<IMessage> messages)
        {
            try
            {
                await channel.DeleteMessagesAsync(messages);
            }
            catch (Exception e)
            {
                LogManager.GetLogger("FAQ").Warn(e, $"Unable to delete messages {string.Join(", ", messages.Select(message => message.Id))} from {guildUser} in {guildUser.Guild}/{channel}");
            }
        }
    }
}