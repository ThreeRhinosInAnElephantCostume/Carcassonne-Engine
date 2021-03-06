using System.Net;
using System.ComponentModel;
/*
    *** Meeple.cs ***

    The Meeple and Occupier classes.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
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

    public abstract class OccupierContainer
    {
        public List<Occupier> Occupiers {get; protected set;} = new List<Occupier>();
        public abstract int GetIndex();
        public void AddOccupier(Occupier occ)
        {
            Occupiers.Add(occ);
        }
        public void RemoveOccupier(Occupier occ)
        {
            Occupiers.Remove(occ);
        }
    }
    public abstract class Occupier : Pawn
    {
        public abstract int Weight { get; }
        public System.Action OnPlace = null;
        public System.Action OnRemove = null;
        public virtual void Place(Tile tile, OccupierContainer container)
        {
            Assert(!container.Occupiers.Contains(this));
            container.AddOccupier(this);
            if(OnPlace != null)
                OnPlace();
        }
        public virtual void Remove(OccupierContainer container)
        {
            Assert(container.Occupiers.Contains(this));
            container.RemoveOccupier(this);
            if(OnRemove != null)
                OnRemove();
        }

        public Occupier(Player player)
        {
            Assert(player != null);
            this.Owner = player;
        }
    }
    public class Meeple : Occupier
    {
        public enum Role
        {
            NONE=0,
            FARMER=1,
            KNIGHT=2,
            HIGHWAYMAN=3,
            MONK=4
        }
        public Role CurrentRole { get; set; } = Role.NONE;
        public override int Weight => 1;
        public override bool IsInPlay => CurrentRole != Role.NONE;
        public OccupierContainer Container = null;
        public static Role MatchRole(NodeType nt)
        {
            Assert(nt != NodeType.ERR);

            switch (nt)
            {
                case NodeType.FARM:
                    return Role.FARMER;
                case NodeType.CITY:
                    return Role.KNIGHT;
                case NodeType.ROAD:
                    return Role.HIGHWAYMAN;
                default:
                    throw new Exception("Invalid/unaccounted for NodeType!");
            }
        }
        public static Role MatchRole(TileAttributeType attr)
        {
            Assert(attr == TileAttributeType.MONASTERY);
            return Role.MONK;
        }
        public bool IsConnectedToNode(InternalNode node)
        {
            return Container == node;
        }
        public bool IsConnectedToAttribute(Tile.TileAttribute attr)
        {
            return Container == attr;
        }
        void Place(InternalNode node)
        {
            Assert(node != null);
            Assert(node.Type != NodeType.ERR);

            node.Graph.Owners.Add(this);
            this.CurrentTile = node.ParentTile;
            this.CurrentRole = MatchRole(node.Type);
            Container = node;
        }
        void Place(Tile tile, Tile.TileAttribute attr)
        {
            Assert(tile != null);
            Assert(tile.Attributes.Contains(attr));
            Assert(attr != null);
            Assert(attr is TileMonasteryAttribute);

            var mon = (TileMonasteryAttribute)attr;
            mon.Owner = this;
            this.CurrentTile = tile;
            this.CurrentRole = MatchRole(attr.Type);
            Container = mon;
        }
        public override void Place(Tile tile, OccupierContainer container)
        {
            if(container is Tile.TileAttribute attr)
            {
                Place(tile, attr);
            }
            else if(container is InternalNode node)
            {
                Place(node);
            }
            base.Place(tile, container);
        }
        public void Remove()
        {
            Assert(IsInPlay);

            if (CurrentRole == Role.MONK)
            {
                var mon = (TileMonasteryAttribute)Container;
                mon.Owner = null;
            }
            else
            {
                var node = (InternalNode)Container;
                node.Graph.Owners.Remove(this);
            }
            CurrentRole = Role.NONE;
            base.Remove(this.Container);
        }
        public Meeple(Player player) : base(player)
        {
            Assert(player != null);
            this.Owner = player;
        }
    }
}
