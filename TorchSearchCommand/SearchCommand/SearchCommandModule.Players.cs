using System.Linq;
using System.Text;
using Sandbox.Game.World;
using SearchCommand.Core;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace SearchCommand
{
    public sealed partial class SearchCommandModule
    {
        [Command("sp", "Searches for players by keywords." +
                       " Supports player names, Steam ID, faction name, faction tag." +
                       " -limit=N the number of search results." +
                       " Display -gps for 1st result.")]
        [Permission(MyPromoteLevel.None)]
        public void SearchPlayers() => this.CatchAndReport(() =>
        {
            var searcher = new StringSimilaritySearcher<MyPlayer>(5);

            var limit = Config.DefaultResultLength;
            var showGps = false;

            foreach (var arg in Context.Args)
            {
                if (CommandOption.TryGetOption(arg, out var option))
                {
                    if (option.TryParse("limit", out var optionValue)
                        && int.TryParse(optionValue, out limit))
                    {
                        continue;
                    }

                    if (option.IsParameterless("gps"))
                    {
                        var player = Context.Player;
                        if (player == null)
                        {
                            Context.Respond("GPS option requires player.", Color.Red);
                            return;
                        }

                        if (player.PromoteLevel < MyPromoteLevel.Moderator)
                        {
                            Context.Respond("GPS option is permitted to Moderators only.", Color.Red);
                            return;
                        }

                        showGps = true;
                        continue;
                    }

                    Context.Respond($"Unknown option: {arg}", Color.Red);
                    return;
                }

                searcher.AddKeyword(arg);
            }

            var players = MySession.Static.Players.GetOnlinePlayers().ToArray();
            foreach (var player in players)
            {
                searcher.AddDictionaryWord(player, player.DisplayName);
                searcher.AddDictionaryWord(player, $"{player.SteamId()}");

                var faction = MySession.Static.Factions.GetPlayerFaction(player.PlayerId());
                if (faction != null)
                {
                    searcher.AddDictionaryWord(player, faction.Name);
                    searcher.AddDictionaryWord(player, faction.Tag);
                }
            }

            var results = searcher.CalcSimilarityAndOrder(limit);
            if (!results.Any())
            {
                Context.Respond($"Player not found by keyword(s): \"{Context.RawArgs}\"");
                return;
            }

            var msg = new StringBuilder();
            msg.AppendLine($"Players found ({results.Length}):");
            foreach (var (player, i) in results.Select((r, i) => (r, i)))
            {
                var gpsReport = "";
                if (i == 0)
                {
                    if (showGps)
                    {
                        DisplayGps(player.Character);
                        gpsReport = "[gps]";
                    }
                }

                msg.AppendLine($"> {player.DisplayName} ({player.SteamId()}) {gpsReport}");
            }

            Context.Respond(msg.ToString());
        });
    }
}