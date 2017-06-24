using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;
using NoAdsHere.Commands.Penalties;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;

namespace NoAdsHere.Commands.BotOwner
{
    [Name("Bot Owner")]
    public class BotOwnerModule : ModuleBase
    {
        private readonly MongoClient _mongo;

        public BotOwnerModule(IServiceProvider provider)
        {
            _mongo = provider.GetService<MongoClient>();
        }

        [Command("Shutdown")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("*Shutting down..*");
            var client = Context.Client as DiscordSocketClient;
            if (client != null)
            {
                var _ = StopAsync(client);
            }
        }

        private static async Task StopAsync(DiscordSocketClient client)
        {
            await CommandHandler.StopHandler();
            
            await client.LogoutAsync();
            await client.StopAsync();
            var logger = LogManager.GetLogger("Discord");
            logger.Info("Bot was shut down via command.");
            Environment.Exit(0);
        }

        [Command("Reset all guilds.")]
        [RequireOwner]
        public async Task Reset()
        {
            var database = _mongo.GetDatabase(Context.Client.CurrentUser.Username.Replace(" ", ""));
            database.DropCollection("Penalty");

            var counter = 0;
            foreach (var guild in await Context.Client.GetGuildsAsync())
            {
                counter++;
                await PenaltyModule.Restore(_mongo, Context.Client as DiscordSocketClient, guild as SocketGuild);

            }
            await ReplyAsync($"{counter} guilds have been reset.");
        
        }

        [Command("Add Master")]
        [RequireOwner]
        public async Task Add_Master(IUser user)
        {
            var newMaster = new Master(user.Id);
            var collection = _mongo.GetCollection<Master>(Context.Client);

            var masters = await collection.GetMastersAsync();

            if (masters.All(m => m.UserId != user.Id))
            {
                await collection.InsertOneAsync(newMaster);
                await ReplyAsync($"{user} added to global Masters!");
            }
            else
            {
                await ReplyAsync($"{user} is already a Master.");
            }
            
        }

        [Command("Remove Master")]
        [RequireOwner]
        public async Task Remove_Master(IUser user)
        {
            var collection = _mongo.GetCollection<Master>(Context.Client);
            var masters = await collection.GetMastersAsync();

            if (masters.Any(m => m.UserId == user.Id))
            {
                await collection.DeleteAsync(masters.First(m => m.UserId == user.Id));
                await ReplyAsync($"{user} removed from global Masters!");
            }
            else
            {
                await ReplyAsync($"{user} is not a Master.");
            }
        }

        [Command("Eval", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Eval([Remainder] string code)
        {
            string cs;
            if (code.StartsWith("```"))
            {
                var cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
                cs1 = code.IndexOf('\n', cs1) + 1;
                var cs2 = code.IndexOf("```", cs1, StringComparison.Ordinal);
                cs = code.Substring(cs1, cs2 - cs1);
            }
            else
                cs = code;
            
            var msg = await SendEmbedAsync(BuildEmbed("Evaluating...", null, 2));

            try
            {
                var globals = new Globals
                {
                    Context = Context,
                    Message = Context.Message as SocketUserMessage,
                    Client = Context.Client as DiscordSocketClient,
                    Mongo = _mongo
                };

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Linq", "Discord", "Discord.WebSocket");
                sopts = sopts.WithReferences(Assembly.GetEntryAssembly());

                var script = CSharpScript.Create(cs, sopts, typeof(Globals));
                script.Compile();
                var result = await script.RunAsync(globals);
                
                if (!string.IsNullOrWhiteSpace(result?.ReturnValue?.ToString()))
                    await SendEmbedAsync(BuildEmbed("Evaluation Result", result.ReturnValue.ToString(), 2), msg);
                else
                    await SendEmbedAsync(BuildEmbed("Evaluation Successful", "No result was returned.", 2), msg);
            }
            catch (Exception e)
            {
                await SendEmbedAsync(
                    BuildEmbed("Evaluation Failure", string.Concat("**", e.GetType().ToString(), "**: ", e.Message), 1),
                    msg);
            }
        }
        
        public class Globals
        {
            public ICommandContext Context { get; set; }
            public SocketUserMessage Message { get; set; }
            public SocketTextChannel Channel => Message.Channel as SocketTextChannel;
            public SocketGuild Guild => Channel.Guild;
            public SocketUser User => Message.Author;
            public DiscordSocketClient Client { get; set; }
            public MongoClient Mongo { get; set; }
        }




        
        private Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, IUserMessage nmsg)
            => SendEmbedAsync(embed, null, nmsg);
        private Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed)
            => SendEmbedAsync(embed, null, Context.Message);

        private async Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed, string content, IUserMessage message)
        {
            var mod = message.Author.Id == Context.Client.CurrentUser.Id;

            if (mod)
                await message.ModifyAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Content = !string.IsNullOrWhiteSpace(content) ? content : message.Content;
                });
            else if (!string.IsNullOrWhiteSpace(content))
                message = await message.Channel.SendMessageAsync(string.Concat(message.Author.Mention, ": ", content), false, embed);
            else
                message = await message.Channel.SendMessageAsync(message.Author.Mention, false, embed);

            return message;
        }
        
        private static EmbedBuilder BuildEmbed(string title, string desc, int type)
        {
            var embed = new EmbedBuilder
            {
                Title = title,
                Description = desc
            };
            switch (type)
            {
                default:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case 1:
                    embed.Color = new Color(255, 0, 0);
                    break;

                case 2:
                    embed.Color = new Color(127, 255, 0);
                    break;
            }
            return embed;
        }
    }
}