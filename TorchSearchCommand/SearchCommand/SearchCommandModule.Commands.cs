using System.Linq;
using System.Text;
using SearchCommand.Core;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;
using Torch.API.Managers;
using Utils.Torch;

namespace SearchCommand
{
    public sealed partial class SearchCommandModule : CommandModule
    {
        [Command("sc", "Searches for commands by keywords. -limit=N the number of search results.")]
        [Permission(MyPromoteLevel.None)]
        public void SearchCommands() => this.CatchAndReport(() =>
        {
            var searcher = new StringSimilaritySearcher<Command>(5);

            var limit = Config.DefaultResultLength; //default

            foreach (var arg in Context.Args)
            {
                if (CommandOption.TryGetOption(arg, out var option))
                {
                    if (option.TryParse("limit", out var optionValue) &&
                        int.TryParse(optionValue, out limit))
                    {
                        // got limit option
                        continue;
                    }

                    Context.Respond($"Unknown option: {arg}", Color.Red);
                    return;
                }

                searcher.AddKeyword(arg);
            }

            if (!searcher.HasAnyKeywords) return;

            var commandManager = Context.Torch.CurrentSession?.Managers.GetManager<CommandManager>();
            if (commandManager == null)
            {
                Context.Respond("Must have an attached session to list commands");
                return;
            }

            var player = Context.Player;
            var playerPromoteLevel = player?.PromoteLevel ?? MyPromoteLevel.Admin;

            foreach (var commandNode in commandManager.Commands.WalkTree())
            {
                var command = commandNode.Command;
                if (command == null) continue; // idk why this is a thing

                if (command.MinimumPromoteLevel > playerPromoteLevel) continue;

                searcher.AddDictionaryWord(command, command.Name);
                searcher.AddDictionaryWord(command, command.Description ?? "");
            }

            var results = searcher.CalcSimilarityAndOrder(limit);
            if (!results.Any())
            {
                Context.Respond($"Command not found by keyword(s): \"{Context.RawArgs}\"");
                return;
            }

            var msg = new StringBuilder();
            msg.AppendLine($"Commands found ({results.Length}):");
            foreach (var resultCommand in results)
            {
                msg.AppendLine($"{resultCommand.SyntaxHelp}");
                msg.AppendLine($" -- {resultCommand.Description}");
            }

            Context.Respond(msg.ToString());
        });
    }
}