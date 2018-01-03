using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Entities;

namespace NoAdsHere.Commands.Advertisement
{
    [Name("Advertisement"), Alias("Ad")]
    public class AdvertisementModule : ModuleBase<SocketCommandContext>
    {
        [Command("New Ad", RunMode = RunMode.Async)]
        public async Task AddAdvertisement(string name, string regexmsg)
        {
            var regex = new Regex(regexmsg);
            
            var inter = new InteractiveService(Context.Client);
            var msg = await ReplyAsync("Is this corretct?\n*(Discord escapes `\\\\` so you need to use `\\\\\\`\n" +
                             "```" +
                             $"Name =  {name}\n" +
                             $"Regex = {regex}```");
            var response = await inter.WaitForMessage(Context.User, Context.Channel, TimeSpan.FromSeconds(30));

            if (!response.Content.ParseStringToBool())
            {
                await msg.ModifyAsync(msgprop => msgprop.Content = "*Canceled*");                
            }

            var ad = new Ad(name, regex);
            using (var dbcontext = new DatabaseContext(true))
            {
                dbcontext.AddAd(ad);
            }

            await msg.ModifyAsync(msgprop => msgprop.Content = $":ok_hand: New Ad with the name {name} is now available");
        }
    }
}