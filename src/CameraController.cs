using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Camera fissa/cinematica sull'arena (vista laterale/3/4 stile sportivo).
    /// Opzionale follow leggero del disco in volo.
    /// </summary>
    public class CameraController : SyncScript
    {
        public Entity ArenaCenter;
        public Entity FollowDisc;           // opzionale: disco da inseguire
        public Vector3 Offset = new Vector3(0f, 12f, 18f);
        public float FollowLerp = 3f;
        public bool FollowActiveDisc = false;

        private Vector3 targetPos;

        public override void Start()
        {
            if (ArenaCenter != null)
                targetPos = ArenaCenter.Transform.WorldMatrix.TranslationVector + Offset;
            else
                targetPos = Entity.Transform.Position;

            Entity.Transform.Position = targetPos;
            LookAtArena();
        }

        public override void Update()
        {
            Vector3 desired = targetPos;

            if (FollowActiveDisc && FollowDisc != null)
            {
                var discPos = FollowDisc.Transform.WorldMatrix.TranslationVector;
                desired = Vector3.Lerp(targetPos, discPos + Offset, 0.25f);
            }
            else if (ArenaCenter != null)
            {
                desired = ArenaCenter.Transform.WorldMatrix.TranslationVector + Offset;
            }

            float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Position = Vector3.Lerp(
                Entity.Transform.Position,
                desired,
                Math.Clamp(FollowLerp * dt, 0f, 1f));

            LookAtArena();
        }

        private void LookAtArena()
        {
            Vector3 lookAt = ArenaCenter != null
                ? ArenaCenter.Transform.WorldMatrix.TranslationVector
                : Vector3.Zero;

            var forward = lookAt - Entity.Transform.Position;
            if (forward.LengthSquared() < 1e-4f) return;

            Entity.Transform.Rotation = Quaternion.LookRotation(
                Vector3.Normalize(forward),
                Vector3.UnitY);
        }
    }
}
