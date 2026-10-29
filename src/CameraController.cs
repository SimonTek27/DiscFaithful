using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Camera fissa stile originale Disc (1990) ma in 3D.
    /// </summary>
    public class CameraController : SyncScript
    {
        public Entity TargetCenter;
        public Platform Platform1;
        public Platform Platform2;

        public Vector3 Offset = new Vector3(0f, 9.5f, -11.0f);
        public float LookAtHeight = 1.2f;
        public float SmoothSpeed = 4.0f;

        public bool EnableBreathing = false;
        public float BreathAmplitude = 0.08f;
        public float BreathSpeed = 0.6f;

        private Vector3 currentPos;
        private float breathTimer;

        public override void Start()
        {
            UpdateCamera(1f);
            currentPos = Entity.Transform.Position;
        }

        public override void Update()
        {
            UpdateCamera((float)Game.UpdateTime.Elapsed.TotalSeconds);
        }

        private void UpdateCamera(float dt)
        {
            Vector3 center = GetArenaCenter();
            Vector3 desired = center + Offset;

            if (EnableBreathing)
            {
                breathTimer += dt * BreathSpeed;
                desired.Y += MathF.Sin(breathTimer) * BreathAmplitude;
            }

            currentPos = Vector3.Lerp(currentPos, desired, Math.Clamp(SmoothSpeed * dt, 0f, 1f));
            Entity.Transform.Position = currentPos;

            Vector3 lookAt = center + new Vector3(0f, LookAtHeight, 0f);
            Entity.Transform.LookAt(lookAt, Vector3.UnitY);
        }

        private Vector3 GetArenaCenter()
        {
            if (TargetCenter != null)
                return TargetCenter.Transform.WorldMatrix.TranslationVector;

            Vector3 p1 = Platform1?.Entity.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;
            Vector3 p2 = Platform2?.Entity.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;
            return (p1 + p2) * 0.5f;
        }

        public void FocusOn(Entity target, float height = 6f, float distance = 7f)
        {
            if (target == null) return;
            Offset = new Vector3(0f, height, -distance);
            TargetCenter = target;
        }

        public void ResetToArenaView()
        {
            Offset = new Vector3(0f, 9.5f, -11.0f);
            TargetCenter = null;
        }
    }
}
