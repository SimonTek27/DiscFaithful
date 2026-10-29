using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Piattaforma 3D completa: 8 floor + 8 wall.
    /// Griglia 2×4. Corrispondenza Index 0-7 rigorosa.
    /// </summary>
    public class Platform : StartupScript
    {
        public List<Tile> FloorTiles = new();
        public List<Tile> WallTiles = new();

        /// <summary> Colpi iniziali delle wall tiles (scala con il livello). </summary>
        public int StartingHits = 3;

        public Entity PlayerEntity;

        public override void Start()
        {
            // Se le liste sono vuote, prova a raccogliere i figli automaticamente
            if (FloorTiles.Count == 0 || WallTiles.Count == 0)
                AutoCollectTiles();

            LinkTiles();
            InitializeHits();
        }

        private void AutoCollectTiles()
        {
            FloorTiles.Clear();
            WallTiles.Clear();

            foreach (var child in Entity.GetChildren())
            {
                var tile = child.Get<Tile>();
                if (tile == null) continue;

                if (tile.Type == Tile.TileType.Floor || child.Name.StartsWith("Floor"))
                    FloorTiles.Add(tile);
                else if (tile.Type == Tile.TileType.Wall || child.Name.StartsWith("Wall"))
                    WallTiles.Add(tile);
            }
        }

        public void LinkTiles()
        {
            for (int i = 0; i < 8; i++)
            {
                var floor = FloorTiles.FirstOrDefault(t => t.Index == i);
                var wall  = WallTiles.FirstOrDefault(t => t.Index == i);

                if (floor != null && wall != null)
                {
                    floor.LinkedTile = wall;
                    wall.LinkedTile  = floor;
                    floor.Type = Tile.TileType.Floor;
                    wall.Type  = Tile.TileType.Wall;
                }
            }
        }

        public void InitializeHits()
        {
            foreach (var wall in WallTiles)
            {
                wall.HitsRemaining = StartingHits;
                wall.IsDestroyed = false;
            }

            foreach (var floor in FloorTiles)
            {
                floor.IsDestroyed = false;
                floor.Entity.EnableAll(true, true);
                floor.Entity.Transform.Scale = Vector3.One;
            }

            foreach (var wall in WallTiles)
            {
                wall.Entity.EnableAll(true, true);
                wall.Entity.Transform.Scale = Vector3.One;
            }
        }

        public bool HasNoFloorLeft()
        {
            return FloorTiles.Count == 0 || FloorTiles.All(t => t.IsDestroyed);
        }

        public int RemainingFloorCount()
        {
            return FloorTiles.Count(t => !t.IsDestroyed);
        }

        public Tile GetWallTileAt(Vector3 worldPosition)
        {
            Tile closest = null;
            float minDist = float.MaxValue;

            foreach (var wall in WallTiles)
            {
                if (wall.IsDestroyed) continue;
                float dist = Vector3.Distance(wall.Entity.Transform.WorldMatrix.TranslationVector, worldPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = wall;
                }
            }
            return closest;
        }

        public Tile GetClosestAliveFloor(Vector3 worldPosition)
        {
            Tile closest = null;
            float minDist = float.MaxValue;

            foreach (var floor in FloorTiles)
            {
                if (floor.IsDestroyed) continue;
                float dist = Vector3.DistanceSquared(floor.Entity.Transform.WorldMatrix.TranslationVector, worldPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = floor;
                }
            }
            return closest;
        }
    }
}
