using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using NoAdsHere.Services.FAQ;

namespace NoAdsHere.Commands.FAQ
{
    [Name("FAQ"), Group("FAQ")]
    public class FaqModule : ModuleBase
    {
        private readonly InteractiveService _interactiveService;
        private readonly FaqSystem _faqSystem;

        public FaqModule(FaqSystem faqSystem, InteractiveService interactiveService)
        {
            _faqSystem = faqSystem;
            _interactiveService = interactiveService;
        }

        [Command("Add", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task Add([Remainder] string stuff = null)
        {
            var header = await ReplyAsync("**FAQ Entry Setup:** *(reply with cancel to cancel at any time)*");

            var msg = await ReplyAsync($"{Context.User.Mention} Please give a name for the new FAQ entry!");
            var nameMessage = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));
            var name = nameMessage.Content;
            if (name.ToLower() == "cancel")
            {
                await header.DeleteAsync();
                await msg.DeleteAsync();
                return;
            }

            await msg.ModifyAsync(properties => properties.Content = $"{Context.User.Mention} Please reply with the whole Message for the FAQ response!");
            var contentMessage = await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));
            var content = contentMessage.Content;
            if (content.ToLower() == "cancel")
            {
                await header.DeleteAsync();
                await msg.DeleteAsync();
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
                await _faqSystem.AddGuildEntryAsync(Context.Guild.Id, Context.User.Id, name.ToLower(), content);
                await header.DeleteAsync();
                await msg.ModifyAsync(p => p.Content = ":ok_hand:");
            }
            else
            {
                await header.DeleteAsync();
                await msg.DeleteAsync();
            }
        }

        [Command("Remove", RunMode = RunMode.Async)]
        [RequirePermission(AccessLevel.HighModerator)]
        [Priority(1)]
        public async Task Remove(string name)
        {
            var gEntry = await _faqSystem.GetGuildFaqEntryAsync(Context.Guild.Id, name.ToLower());

            if (gEntry != null)
            {
                var msg = await ReplyAsync(
                    $"Please confirm removal of FAQ entry `{gEntry.Name}` *(yes to accept, annything else to cancel)*");
                var responseMsg =
                    await _interactiveService.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));

                if (responseMsg.Content.ToLower() == "yes")
                {
                    await _faqSystem.RemoveGuildEntryAsync(gEntry);
                    await msg.ModifyAsync(p => p.Content = ":ok_hand:");
                }
                else
                {
                    await msg.DeleteAsync();
                }
            }
            else
            {
                var gSimilar = await _faqSystem.GetSimilarGuildEntries(Context.Guild.Id, name.ToLower());
                if (gSimilar.Any())
                    await ReplyAsync($"No FAQ Entry with the name `{name}` found. Did you mean:\n`" +
                                   string.Join("`", gSimilar.Select(pair => pair.Key.Name)) + "`");
                else
                    await ReplyAsync($"No FAQ entry with the name {name} found.");
            }
        }
    }
}