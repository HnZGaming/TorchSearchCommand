using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParallelTasks;
using Sandbox.Game.Entities;
using SearchCommand.Core;
using SpaceEngineers.Game.Entities.Blocks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;
using VRageMath;

namespace SearchCommand
{
    public sealed partial class SearchCommandModule
    {
        [Command("sg", "Searches for grids by keywords." +
                       " Supports grid names and faction tag." +
                       " --limit=N the number of search results." +
                       " Display --gps for 1st result." +
                       " Use --regex for advanced search." +
                       " Filter --distance=N from your character.")]
        [Permission(MyPromoteLevel.None)]
        public void SearchGrids() => this.CatchAndReport(() =>
        {
            var limit = Config.DefaultResultLength;
            var gpsCount = 0;
            var distance = 0d;
            var useRegex = false;
            var keywords = new List<string>();

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
                        if (Context.Player == null)
                        {
                            Context.Respond("GPS option requires player.", Color.Red);
                            return;
                        }

                        gpsCount = 1;
                        continue;
                    }

                    if (option.TryParseInt("gps", out var gpsCountLocal))
                    {
                        if (Context.Player == null)
                        {
                            Context.Respond("GPS option requires player.", Color.Red);
                            return;
                        }

                        gpsCount = gpsCountLocal;
                        continue;
                    }

                    if (option.TryParseDouble("distance", out distance))
                    {
                        continue;
                    }

                    if (option.IsParameterless("regex"))
                    {
                        useRegex = true;
                        continue;
                    }

                    Context.Respond($"Unknown option: {arg}", Color.Red);
                    return;
                }

                keywords.Add(arg);
            }

            if (distance > 0 && Context.Player == null)
            {
                throw new Exception("Distance parameter is available for in-game players only");
            }

            var searcher = useRegex
                ? (IStringSearcher<MyCubeGrid>)new RegexStringSearcher<MyCubeGrid>()
                : new StringSimilaritySearcher<MyCubeGrid>(5);

            foreach (var keyword in keywords)
            {
                searcher.AddKeyword(keyword);
            }

            Context.Respond("Searching...");

            var grids = MyCubeGridGroups.Static.Logical.Groups
                .SelectMany(g => g.Nodes)
                .Select(g => g.NodeData)
                .ToArray();

            Parallel.ForEach(grids, grid =>
            {
                if (grid == null) return;

                if (distance > 0)
                {
                    var callerPosition = Context.Player.Character.GetPosition();
                    var gridPosition = grid.PositionComp.GetPosition();
                    if (Vector3D.Distance(callerPosition, gridPosition) > distance) return;
                }

                var callerIsOwner = grid.BigOwners.Contains(Context.Player?.IdentityId ?? 0);
                var callerIsAdmin = Context.Player == null || Context.Player?.PromoteLevel > MyPromoteLevel.None;
                var gridIsAdmin = grid.IsAdminGrid();
                var gridIsOfNobody = !grid.BigOwners.Any();
                var isSearchable = callerIsOwner || callerIsAdmin || gridIsAdmin || gridIsOfNobody;
                if (!isSearchable) return;

                searcher.AddDictionaryWord(grid, grid.DisplayName);

                foreach (var block in grid.CubeBlocks)
                {
                    var fatBlock = block?.FatBlock;
                    if (fatBlock == null) continue;

                    if (fatBlock is MyMedicalRoom medicalRoom)
                    {
                        var spawnName = medicalRoom.SpawnName.ToString();
                        searcher.AddDictionaryWord(grid, spawnName);
                    }

                    if (fatBlock is MySurvivalKit survivalKit)
                    {
                        var spawnName = survivalKit.SpawnName.ToString();
                        searcher.AddDictionaryWord(grid, spawnName);
                    }
                }
            });

            var results = searcher.OrderSimilarWords(limit);
            if (!results.Any())
            {
                Context.Respond($"Grid not found: \"{searcher.Keywords}\"");
                return;
            }

            var msg = new StringBuilder();
            msg.AppendLine($"Grids found ({results.Length}):");
            foreach (var (grid, i) in results.Select((r, i) => (r, i)))
            {
                var gpsReport = "";
                if (i + 1 < gpsCount)
                {
                    DisplayGps(grid);
                    gpsReport = "[gps]";
                }

                var owners = grid.GetBigOwnerPlayers();
                var ownersStr = owners.Select(o => $"\"{o.DisplayName}\"").ToStringSeq();

                msg.AppendLine($"> \"{grid.DisplayName}\" ({ownersStr}) {gpsReport}");
            }

            Context.Respond(msg.ToString());
        });
    }
}