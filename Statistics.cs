using System.Reflection.PortableExecutable;
/*
    *** Statistics.cs ***

    Helper functions for gathering statistics about the game state.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Carcassonne;
using ExtraMath;
using static System.Math;
using static Carcassonne.GameEngine;
using static Utils;

namespace Carcassonne
{
    public partial class GameEngine
    {
        public class Statistics
        {
            public struct TurnStatistics
            {
                public struct PointsBreakdown
                {
                    public int Total {get; set;}
                    public int Real {get; set;}
                    public int Potential {get; set;}
                    public int FromFarms {get; set;}
                    public int FromRoads {get; set;}
                    public int FromCities {get; set;}
                    public int FromMonasteries {get; set;}
                    public int PotentialFromRoads {get; set;}
                    public int PotentialFromCities {get; set;}
                    public int PotentialFromMonasteries {get; set;}
                }
                public struct PlayerStats
                {
                    public long ID {get; set;}
                    public PointsBreakdown points;
                    public int AvailableMeeples {get; set;}
                    public int PlacedMeeples {get; set;}
                    public int TotalMeeples {get; set;}
                    public int Knights {get; set;}
                    public int Highwaymen {get; set;}
                    public int Monks {get; set;}
                    public int Farmers {get; set;}
                    public int TotalPlacedMeeples {get; set;}
                    public int TotalPlacedKnights {get; set;}
                    public int TotalPlacedHighwaymen {get; set;}
                    public int TotalPlacedFarmers {get; set;}
                    public int TotalPlacedMonks {get; set;}
                    public int TurnsUnableToPlaceMeeples {get; set;}
                    public int FinishedProjects {get; set;}
                    public int InvolvedProjects {get; set;}
                    public int ContestedProjects {get; set;}
                    public int FinishedRoads {get; set;}
                    public int FinishedMonasteries {get; set;}
                    public int FinishedCities {get; set;}
                    public int Farms {get; set;}
                    
                }
                public int Turn {get; set;}
                public long[] WinningPlayers;
                public PointsBreakdown CombinedPoints;
                public int PlacedTiles {get; set;}
                public int OpenProjects {get; set;}
                public int FinishedProjects {get; set;}
                public int ContestedProjects {get; set;}
                public int OpenMonasteries {get; set;}
                public int OpenRoads {get; set;}
                public int OpenCities {get; set;}
                public int FinishedMonasteries {get; set;}
                public int FinishedRoads {get; set;}
                public int FinishedCities {get; set;}
                public int OrphanedProjects {get; set;}
                public int MeeplesInPlay {get; set;}
                public int Knights {get; set;}
                public int Farmers {get; set;}
                public int Monks {get; set;}
                public int Highwaymen {get; set;}
                public int Farms {get; set;}
                public PlayerStats[] PlayerData;
            }
            public readonly ulong Seed;
            public readonly uint Turns;
            public readonly int Players;
            public readonly long[] PlayerIDs;
            public readonly int PlacedTiles;
            public readonly int TotalTiles;
            public readonly GameEngine.State FinalState;
            public readonly bool Ended;
            public readonly long[] Winners; // empty if the game has not ended
            public readonly TurnStatistics[] TurnsData;
            public readonly Action[] Actions;

            public readonly double AverageMeeplesInPlayPerPlayerPerTurn;
            public readonly double AveragePointsPerPlayerPerTurn;
            public readonly double AverageFinishedProjectsPerPlayerPerTurn;

            public readonly double AverageMeeplesInPlayPerTurn;
            public readonly double AveragePointsPerTurn;
            public readonly double AverageOpenProjectsPerTurn;
            public readonly double AverageFinishedProjectsPerTurn;

            public TurnStatistics FinalTurn => TurnsData.Last();
            public Statistics(GameEngine eng, bool recordActions)
            {
                eng = eng.Clone();
                Assert(eng.History.Count > 0);
                if(recordActions)
                    Actions = eng.History.ToArray();
                
                Seed = eng._seed;
                Turns = eng.Turn;
                Players = eng.Players.Count;
                FinalState = eng.CurrentState;
                PlayerIDs = eng.Players.ConvertAll<long>(it => it.ID).ToArray();
                PlacedTiles = eng.map.GetPlacedTiles().Count;
                TotalTiles = eng._tileset.NTiles;
                if(eng.CurrentState == State.GAME_OVER)
                {
                    Ended = true;
                    Winners = eng.GetWinners().ConvertAll<long>(it => it.ID).ToArray();
                }
                else
                {
                    Ended = false;
                    Winners = new long[0];
                }
                var state = GameEngine.CreateFromAction(eng._dataSource, eng.InitialAction);
                TurnsData = new TurnStatistics[Turns];
                var ts = new TurnStatistics();
                ts.PlayerData = new TurnStatistics.PlayerStats[eng.Players.Count];
                int _i = 0;
                foreach(var player in state.Players)
                {
                    int i = _i; // copy provided to the lambda below
                    player.OnAddScore += (int score, object source) =>
                    {
                        if(source is Map.Graph g)
                        {
                            // this avoids duplicating contested projects
                            bool islastplayer = g.Owners.Count == 1 || 
                                player == state.Players.FindLast(it => g.Owners.Contains(it));
                            if(islastplayer)
                            {
                                ts.FinishedProjects++;
                                ts.PlayerData[i].FinishedProjects++;
                            }
                            if(g.Type == NodeType.FARM)
                            {
                                throw new Exception("Farms cannot be finished");
                            }
                            else if(g.Type == NodeType.CITY)
                            {
                                if(islastplayer)
                                    ts.FinishedCities++;
                                ts.PlayerData[i].points.FromCities += score;
                                ts.PlayerData[i].FinishedCities++;
                            }
                            else if(g.Type == NodeType.ROAD)
                            {
                                if(islastplayer)
                                    ts.FinishedRoads++;
                                ts.PlayerData[i].points.FromRoads += score;
                                ts.PlayerData[i].FinishedRoads++;
                            }
                            else
                                throw new Exception($"Unsupported graph type {g.Type}");
                        }
                        else if (source is Tile.TileAttribute attr)
                        {
                            ts.FinishedProjects++;
                            ts.PlayerData[i].FinishedProjects++;

                            if(attr is TileMonasteryAttribute)
                            {
                                ts.PlayerData[i].FinishedMonasteries++;
                                ts.FinishedMonasteries++;
                                ts.PlayerData[i].points.FromMonasteries += score;   
                            }
                            else
                                throw new Exception($"Unsupported TileAttribute {attr}");
                        }
                        else
                            throw new Exception($"Unsupported source {source}");
                    };
                    player.OnAddPotentialScore += (int score, object source) =>
                    {
                        if(source is Map.Graph g)
                        {
                            if(g.Type == NodeType.FARM)
                            {
                                ts.PlayerData[i].points.FromFarms += score;
                            }
                            else if(g.Type == NodeType.CITY)
                            {
                                ts.PlayerData[i].points.PotentialFromCities += score;
                            }
                            else if(g.Type == NodeType.ROAD)
                            {
                                ts.PlayerData[i].points.PotentialFromRoads += score;
                            }
                            else
                                throw new Exception($"Unsupported graph type {g.Type}");
                        }
                        else if (source is Tile.TileAttribute attr)
                        {

                            if(attr is TileMonasteryAttribute)
                            {
                                ts.PlayerData[i].points.PotentialFromMonasteries += score;   
                            }
                            else
                                throw new Exception($"Unsupported TileAttribute {attr}");
                        }
                        else
                            throw new Exception($"Unsupported source {source}");
                    };
                    _i++;
                }
                var hist = eng.History.GetRange(1, eng.History.Count-1);
                for(int i = 0; i < Turns; i++)
                {
                    ts = ts.Clone();
                    ts.PlayerData = ts.PlayerData.ToArray();
                    for(int ii = 0; ii < state.Players.Count; ii++)
                    {
                        ref var pd = ref ts.PlayerData[ii];
                        pd.points.FromFarms = 0;
                        pd.points.PotentialFromCities = 0;
                        pd.points.PotentialFromMonasteries = 0;
                        pd.points.PotentialFromRoads = 0;
                    }
                    ts.Turn = i;
                    while(state.Turn == i && hist.Count > 0)
                    {
                        for(int ii = 0; ii < state.Players.Count; ii++)
                        {
                            var player = state.Players[ii];
                            ref var pd = ref ts.PlayerData[ii];
                            pd.ID = player.ID;
                            pd.PlacedMeeples = player.PlacedMeeples;
                            pd.TotalMeeples = player.TotalMeeples;
                            pd.AvailableMeeples = player.AvailableMeeples;
                            pd.Knights = player.Knights;
                            pd.Farmers = player.Farmers;
                            pd.Monks = player.Monks;
                            pd.Highwaymen = player.Highwaymen;
                            pd.points.Potential = player.PotentialScore;
                            pd.points.Real = player.Score;
                            pd.points.Total = player.EndScore;
                            var nodeprojects = player.Pawns
                                .FindAll(it => it.IsInPlay && it is Meeple meep && meep.Container is InternalNode node)
                                .ConvertAll<Map.Graph>(it => ((it as Meeple).Container as InternalNode).Graph)
                                .Distinct();
                            pd.Farms = nodeprojects.Count(it => it.Type == NodeType.FARM);
                            pd.InvolvedProjects = nodeprojects.Count() + player.Monks;
                            pd.ContestedProjects = nodeprojects.Count(it => it.Owners.Count > 1);
                        }
                        ts.WinningPlayers = state.GetWinners().ConvertAll<long>(it => it.ID).ToArray();
                        ts.ContestedProjects = 0;
                        ts.OpenProjects = 0;
                        ts.OpenMonasteries = 0;
                        ts.OpenRoads = 0;
                        ts.OpenCities = 0;
                        ts.Farms = 0;
                        ts.OrphanedProjects = 0;

                        foreach(var g in state.map.Graphs)
                        {
                            if(g.Owners.Count > 0)
                            {
                                ts.OpenProjects++;
                                if(g.Owners.Count > 1)
                                   ts.ContestedProjects++;
                                if(g.Type == NodeType.ROAD)
                                    ts.OpenRoads++;
                                else if(g.Type == NodeType.CITY)
                                    ts.OpenCities++;
                                else if(g.Type == NodeType.FARM)
                                    ts.Farms++;
                                else 
                                    throw new Exception("Unsupported NodeType!");
                            }
                            else
                                ts.OrphanedProjects++;
                        }
                        ts.OpenMonasteries = state._activeMonasteries.Count;
                        state.ExecuteAction(hist.First());
                        hist.RemoveAt(0);
                    }
                    ts.PlacedTiles = state.map.GetPlacedTiles().Count;
                    ts.CombinedPoints = new TurnStatistics.PointsBreakdown();
                    ts.Knights = state.Players.Sum(it => it.Knights);
                    ts.Farmers = state.Players.Sum(it => it.Farmers);
                    ts.Highwaymen = state.Players.Sum(it => it.Highwaymen);
                    ts.Monks = state.Players.Sum(it => it.Monks);
                    ts.MeeplesInPlay = state.Players.Sum(it => it.PlacedMeeples);
                    foreach(var p in ts.PlayerData)
                    {
                        ts.CombinedPoints.FromFarms += p.points.FromFarms;
                        ts.CombinedPoints.FromCities += p.points.FromCities;
                        ts.CombinedPoints.FromMonasteries += p.points.FromMonasteries;
                        ts.CombinedPoints.FromRoads += p.points.FromRoads;
                        ts.CombinedPoints.PotentialFromCities += p.points.PotentialFromCities;
                        ts.CombinedPoints.PotentialFromMonasteries += p.points.PotentialFromMonasteries;
                        ts.CombinedPoints.PotentialFromRoads += p.points.PotentialFromRoads;
                        ts.CombinedPoints.Real += p.points.Real;
                        ts.CombinedPoints.Potential += p.points.Potential;
                        ts.CombinedPoints.Total += p.points.Total;
                    }
                    TurnsData[i] = ts;
                }
                for(int i = 0; i < TurnsData[0].PlayerData.Length; i++)
                {
                    ref var pd = ref TurnsData[0].PlayerData[i];
                    pd.TotalPlacedFarmers = pd.Farmers;
                    pd.TotalPlacedHighwaymen = pd.Highwaymen;
                    pd.TotalPlacedKnights = pd.Knights;
                    pd.TotalPlacedMonks = pd.Monks;
                    pd.TotalPlacedMeeples = pd.PlacedMeeples;
                }
                for(int i = 1; i < TurnsData.Length; i++)
                {
                    var prev = TurnsData[i-1];
                    for(int ii = 0; ii < TurnsData[i].PlayerData.Length; ii++)
                    {
                        var ppd = prev.PlayerData[ii];
                        ref var pd = ref TurnsData[i].PlayerData[ii];
                        pd.TotalPlacedFarmers += pd.Farmers;
                        pd.TotalPlacedHighwaymen += Max(pd.Highwaymen - ppd.Highwaymen, 0);
                        pd.TotalPlacedKnights += Max(pd.Knights - ppd.Knights, 0);
                        pd.TotalPlacedMonks += Max(pd.Monks - ppd.Monks, 0);
                        pd.TotalPlacedMeeples = pd.TotalPlacedFarmers + pd.TotalPlacedHighwaymen + pd.TotalPlacedKnights + pd.TotalPlacedMonks;
                    }
                }
                if(FinalState == State.GAME_OVER)
                {
                    ref var last = ref TurnsData[TurnsData.Length -1];
                    for(int i = 0; i < last.PlayerData.Length; i++)
                    {
                        ref var pd = ref last.PlayerData[i];
                        pd.points.FromCities += pd.points.PotentialFromCities;
                        pd.points.FromMonasteries += pd.points.PotentialFromMonasteries;
                        pd.points.FromRoads += pd.points.PotentialFromRoads;
                    }
                    last.CombinedPoints.FromCities += last.CombinedPoints.PotentialFromCities;
                    last.CombinedPoints.FromMonasteries += last.CombinedPoints.PotentialFromMonasteries;
                    last.CombinedPoints.FromRoads += last.CombinedPoints.PotentialFromRoads;
                }

                foreach(var it in TurnsData)
                {
                    AverageMeeplesInPlayPerTurn += (double)it.MeeplesInPlay/(double)Turns;
                    AveragePointsPerTurn += ((double)it.CombinedPoints.Total)/(double)Turns;
                    AverageOpenProjectsPerTurn += (double)it.OpenProjects/(double)Turns;
                    AverageFinishedProjectsPerTurn += (double)it.FinishedProjects/(double)Turns;
                    AverageMeeplesInPlayPerPlayerPerTurn += (double)it.MeeplesInPlay/(double)Players/(double)Turns;
                }

                AverageFinishedProjectsPerTurn = (double)FinalTurn.FinishedProjects/(double)Turns;

                AveragePointsPerPlayerPerTurn = (double)FinalTurn.CombinedPoints.Total/(double)Players/(double)Turns;
                AverageFinishedProjectsPerPlayerPerTurn = (double)FinalTurn.FinishedProjects/(double)Players/(double)Turns;
            }
        }
    }
}