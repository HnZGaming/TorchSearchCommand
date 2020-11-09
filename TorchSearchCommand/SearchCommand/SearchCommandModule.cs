using System.Linq;
using System.Text;
using SearchCommand.Search;
using Torch.Commands;
using Torch.Commands.Permissions;
using TorchUtils;
using VRage.Game.ModAPI;
using VRageMath;
using Torch.API.Managers;

namespace SearchCommand
{
    public sealed class SearchCommandModule : CommandModule
    {
        [Command("sc", "Searches for commands by keywords. --limit=N to set the number of search results.")]
        [Permission(MyPromoteLevel.None)]
        public void SearchCommands()
        {
            var searcher = new StringSimilaritySearcher<Command>(5);

            var limit = 3; //default

            foreach (var arg in Context.Args)
            {
                if (arg.TryParseOption(out var optionKey, out var optionValue))
                {
                    if (optionKey == "limit" && int.TryParse(optionValue, out limit))
                    {
                        // got limit option
                    }
                    else
                    {
                        Context.Respond($"Unknown option: {arg}", Color.Red);
                        return;
                    }
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

            var scores = searcher.CalcSimilarity();
            var resultCommands = scores
                .OrderByDescending(md => md.Similarity)
                .Where(md => md.Similarity > 0)
                .Take(limit)
                .Select(md => md.Key)
                .ToArray();

            if (!resultCommands.Any())
            {
                Context.Respond($"Command not found by keyword(s): \"{Context.RawArgs}\"");
                return;
            }

            var msg = new StringBuilder();
            msg.AppendLine("Commands found:");
            foreach (var resultCommand in resultCommands)
            {
                msg.AppendLine($"{resultCommand.SyntaxHelp}");
                msg.AppendLine($" -- {resultCommand.Description}");
            }

            Context.Respond(msg.ToString());
        }
    }
}