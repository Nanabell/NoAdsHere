using Discord.Commands;
using Microsoft.Extensions.Configuration;
using NoAdsHere.Common;
using NoAdsHere.Common.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Commands
{
    [Name("Help"), Group]
    public class HelpCommand : ModuleBase
    {
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;
        private readonly IConfigurationRoot _config;

        public HelpCommand(IServiceProvider provider, CommandService service, IConfigurationRoot config)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config;
            _provider = provider;
        }

        [Command("Help")]
        [Summary("Display what commands you can use"), Hidden]
        public async Task Help()
        {
            var cmdgrps = (await _service.Commands.CheckConditionsAsync(Context, _provider))
                .Where(c => !c.Preconditions.Any(p => p is HiddenAttribute))
                .GroupBy(c => (c.Module.IsSubmodule ? c.Module.Parent.Name : c.Module.Name));

            var sb = new StringBuilder();
            sb.AppendLine("You can use the following commands:\n");

            foreach (var group in cmdgrps)
            {
                var commands = group.Select(commandInfo => commandInfo.Module.IsSubmodule
                        ? $"`{commandInfo.Module.Name}*`"
                        : $"`{commandInfo.Name}`")
                    .ToList();

                sb.AppendLine($"**{group.Key}**: {string.Join(" ", commands.Distinct())}");
            }
            sb.AppendLine(
                $"\nTo use commands do `{_config["Prefixes:Main"]} <group> <command>`.");

            await ReplyAsync($"{sb}");
        }

        [Command("Help")]
        [Summary("Display how you can use a command"), Hidden]
        public async Task Help(string commandName)
        {
            var commands = (await _service.Commands.CheckConditionsAsync(Context, _provider)).Where(
                c => (c.Aliases.FirstOrDefault().Equals(commandName, StringComparison.OrdinalIgnoreCase)) ||
                     (c.Module.IsSubmodule && c.Module.Aliases.FirstOrDefault()
                          .Equals(commandName, StringComparison.OrdinalIgnoreCase)) &&
                     !c.Preconditions.Any(p => p is HiddenAttribute));

            var sb = new StringBuilder();
            var commandInfos = commands as IList<CommandInfo> ?? commands.ToList();
            if (commandInfos.Any())
            {
                sb.AppendLine(
                    $"{commandInfos.Count} {(commandInfos.Count > 1 ? "entries" : "entry")} for `{commandName}`");
                sb.AppendLine("```cs");

                foreach (var command in commandInfos)
                {
                    sb.AppendLine("Usage");
                    sb.AppendLine(
                        $"\t{_config["Prefixes:Main"]} {(command.Module.IsSubmodule ? $"{command.Module.Name} " : "")}{command.Name} " +
                        string.Join(" ", command.Parameters.Select(FormatParam)).Replace("`", ""));
                    sb.AppendLine("Summary");
                    sb.AppendLine($"\t{command.Summary ?? "No Summary"}");
                }
                sb.AppendLine("```");
                await ReplyAsync($"{sb}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Unable to find any comamnd matching `{commandName}`");
            }
        }

        private static string FormatParam(ParameterInfo parameter)
        {
            var sb = new StringBuilder();
            if (parameter.IsMultiple)
            {
                sb.Append($"`[({parameter.Type.Name}): {parameter.Name}...]`");
            }
            else if (parameter.IsRemainder)
            {
                sb.Append($"`<({parameter.Type.Name}): {parameter.Name}...>`");
            }
            else if (parameter.IsOptional)
            {
                sb.Append($"`[({parameter.Type.Name}): {parameter.Name}]`");
            }
            else
            {
                sb.Append($"`<({parameter.Type.Name}): {parameter.Name}>`");
            }

            if (!string.IsNullOrWhiteSpace(parameter.Summary))
            {
                sb.Append($" ({parameter.Summary})");
            }
            return sb.ToString();
        }
    }
}