using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Piattaforma 2×4: 8 floor + 8 wall collegate 1:1.
    /// </summary>
    public class Platform : StartupScript
    {
        public List<Tile> FloorTiles = new();
        public List<Tile> WallTiles = new();
        public int StartingHits = 3;
        public Entity PlayerEntity;

        public override void Start()
        {
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
                var wall = WallTiles.FirstOrDefault(t => t.Index == i);
                if (floor == null || wall == null) continue;

                floor.LinkedTile = wall;
                wall.LinkedTile = floor;
                floor.Type = Tile.TileType.Floor;
                wall.Type = Tile.TileType.Wall;
            }
        }

        public void InitializeHits()
        {
            foreach (var wall in WallTiles)
                wall.ResetTile(StartingHits);

            foreach (var floor in FloorTiles)
                floor.ResetTile(0);
        }

        public bool HasNoFloorLeft() =>
            FloorTiles.Count == 0 || FloorTiles.All(t => t.IsDestroyed);

        public int RemainingFloorCount() =>
            FloorTiles.Count(t => !t.IsDestroyed);

        public Tile GetClosestAliveFloor(Vector3 worldPosition)
        {
            Tile closest = null;
            float minDist = float.MaxValue;

            foreach (var floor in FloorTiles)
            {
                if (floor.IsDestroyed) continue;

                float dist = Vector3.DistanceSquared(
                    floor.Entity.Transform.WorldMatrix.TranslationVector,
                    worldPosition);

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
