/*
    *** Player.cs ***

    The definition for the Player class. See Engine/Agent.cs for more.
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
        public class Player : Agent
        {
            GameEngine eng { get; }

            public int Score { get; protected set; } = 0;
            public int PotentialScore { get; protected set; } = 0;
            public int EndScore => Score + PotentialScore;
            public Action<int, object> OnAddScore;

            public Action<int, object> OnAddPotentialScore;

            public void ResetPotentialScore()
            {
                PotentialScore = 0;
            }
            void AddPotentialScoreObject(int count, object source)
            {
                if(OnAddPotentialScore != null)
                    OnAddPotentialScore(count, source);
                PotentialScore += count;
            }
            public void AddPotentialScore(int count, Map.Graph source) =>
                AddPotentialScoreObject(count, source);
            public void AddPotentialScore(int count, Tile.TileAttribute source) =>
                AddPotentialScoreObject(count, source);
            void AddScoreObject(int count, object source)
            {
                if(OnAddScore != null)
                    OnAddScore(count, source);
                Score += count;
            }
            public void AddScore(int count, Map.Graph source) =>
                AddScoreObject(count, source);
            public void AddScore(int count, Tile.TileAttribute source) =>
                AddScoreObject(count, source);


            public List<Pawn> Pawns { get; set; } = new List<Pawn>();
            public List<Pawn> PawnsInPlay => 
                Pawns.FindAll(it => it.IsInPlay);
            public int TotalMeeples => 
                Pawns.Count(it => it is Meeple);
            public int PlacedMeeples => 
                PawnsInPlay.Count(it => it is Meeple m && m.IsInPlay);
            public int AvailableMeeples => 
                Pawns.Count(it => it is Meeple m && !m.IsInPlay);
            public int Farmers => 
                PawnsInPlay.Count(it => it is Meeple m && m.CurrentRole == Meeple.Role.FARMER);
            public int Knights => 
                PawnsInPlay.Count(it => it is Meeple m && m.CurrentRole == Meeple.Role.KNIGHT);
            public int Highwaymen => 
                PawnsInPlay.Count(it => it is Meeple m && m.CurrentRole == Meeple.Role.HIGHWAYMAN);
            public int Monks => 
                PawnsInPlay.Count(it => it is Meeple m && m.CurrentRole == Meeple.Role.MONK);

            public Player(GameEngine eng, int ID) : base(ID)
            {
                this.eng = eng;
            }
        }
    }
}
