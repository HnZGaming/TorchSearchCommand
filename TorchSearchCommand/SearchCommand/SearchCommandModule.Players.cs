using System;
using System.Linq;
using System.Text;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SearchCommand.Core;
using Torch.Commands;
using Torch.Commands.Permissions;
using TorchUtils;
using VRage.Game.ModAPI;
using VRageMath;

namespace SearchCommand
{
    public sealed partial class SearchCommandModule
    {
        [Command("sp", "Searches for players by keywords." +
                       " Supports player names, Steam ID, faction name, faction tag." +
                       " -limit=N the number of search results." +
                       " -copy 1st result to clipboard." +
                       " Display -gps for 1st result.")]
        [Permission(MyPromoteLevel.None)]
        public void SearchPlayers() => this.CatchAndReport(() =>
        {
            var searcher = new StringSimilaritySearcher<MyPlayer>(10);

            var limit = 6;
            var copyToClipboard = false;
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

                    if (option.IsParameterless("copy"))
                    {
                        copyToClipboard = true;
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
            msg.AppendLine($"Commands found ({results.Length}):");
            foreach (var (player, i) in results.Select((r, i) => (r, i)))
            {
                var copyReport = "";
                var gpsReport = "";
                if (i == 0)
                {
                    if (copyToClipboard)
                    {
                        ViewUtils.CopyToClipboard(player.DisplayName);
                        copyReport = "[clipboard]";
                    }

                    if (showGps)
                    {
                        DisplayGps(player);
                        gpsReport = "[gps]";
                    }
                }

                msg.AppendLine($"> {player.DisplayName} ({player.SteamId()}) {copyReport} {gpsReport}");
            }

            Context.Respond(msg.ToString());
        });

        void DisplayGps(MyPlayer player)
        {
            var gpsCollection = (MyGpsCollection) MyAPIGateway.Session?.GPS;
            if (gpsCollection == null)
            {
                Context.Respond("GPS not available.", Color.Red);
                return;
            }

            var gps = new MyGps
            {
                Coords = player.GetPosition(),
                Name = $"!sp {player.DisplayName}",
                GPSColor = Color.Green,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = null,
            };

            gps.UpdateHash();
            gps.SetEntity(player.Character);

            gpsCollection.SendAddGps(Context.Player.IdentityId, ref gps, player.Character.EntityId);
        }
    }
}