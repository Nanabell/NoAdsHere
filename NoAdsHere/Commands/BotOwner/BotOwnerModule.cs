using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NLog;
using NoAdsHere.Database.Entities.Global;
using NoAdsHere.Database.UnitOfWork;
using NoAdsHere.Services.Events;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NoAdsHere.Commands.BotOwner
{
    [Name("Bot Owner")]
    public class BotOwnerModule : ModuleBase
    {
        private readonly IUnitOfWork _unit;

        public BotOwnerModule(IUnitOfWork unit)
        {
            _unit = unit;
        }

        [Command("Shutdown")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("*Shutting down..*");
            var client = Context.Client as DiscordShardedClient;
            if (client != null)
            {
                var _ = StopAsync(client);
            }
        }

        private static async Task StopAsync(DiscordShardedClient client)
        {
            await EventHandlers.StopCommandHandlerAsync();

            await client.LogoutAsync();
            await client.StopAsync();
            var logger = LogManager.GetLogger("Discord");
            logger.Info("Bot was shut down via command.");
            Environment.Exit(0);
        }

        [Command("Add Master")]
        [RequireOwner]
        public async Task Add_Master(IUser user)
        {
            var master = await _unit.Masters.GetAsync(user);

            if (master == null)
            {
                master = new Master() { UserId = user.Id };
                await _unit.Masters.AddAsync(master);
                _unit.SaveChanges();
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
            var master = await _unit.Masters.GetAsync(user);

            if (master != null)
            {
                _unit.Masters.Remove(master);
                _unit.SaveChanges();
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
            if (code.StartsWith("```", StringComparison.Ordinal))
            {
                var cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
                cs1 = code.IndexOf('\n', cs1) + 1;
                var cs2 = code.IndexOf("```", cs1, StringComparison.Ordinal);
                cs = code.Substring(cs1, cs2 - cs1);
            }
            else
                cs = code;

            var msg = await SendEmbedAsync(BuildEmbed("Evaluating...", null, 2)).ConfigureAwait(false);

            try
            {
                var globals = new Globals
                {
                    Context = Context,
                    Message = Context.Message as SocketUserMessage,
                    Client = Context.Client as DiscordShardedClient,
                    Unit = _unit
                };

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Linq", "Discord", "Discord.WebSocket");
                sopts = sopts.WithReferences(Assembly.GetEntryAssembly());

                var script = CSharpScript.Create(cs, sopts, typeof(Globals));
                script.Compile();
                var result = await script.RunAsync(globals);

                if (!string.IsNullOrWhiteSpace(result?.ReturnValue?.ToString()))
                    await SendEmbedAsync(BuildEmbed("Evaluation Result", result.ReturnValue.ToString(), 2), msg)
                        .ConfigureAwait(false);
                else
                    await SendEmbedAsync(BuildEmbed("Evaluation Successful", "No result was returned.", 2), msg).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await SendEmbedAsync(
                    BuildEmbed("Evaluation Failure", string.Concat("**", e.GetType().ToString(), "**: ", e.Message), 1),
                    msg).ConfigureAwait(false);
            }
        }

        public class Globals
        {
            public ICommandContext Context { get; set; }
            public SocketUserMessage Message { get; set; }
            public SocketTextChannel Channel => Message.Channel as SocketTextChannel;
            public SocketGuild Guild => Channel.Guild;
            public SocketUser User => Message.Author;
            public DiscordShardedClient Client { get; set; }
            public IUnitOfWork Unit { get; set; }
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
                message = await message.Channel.SendMessageAsync(string.Concat(message.Author.Mention, ": ", content), false, embed.Build());
            else
                message = await message.Channel.SendMessageAsync(message.Author.Mention, false, embed.Build());

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