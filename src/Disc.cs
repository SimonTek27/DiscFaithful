using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Stride.Animations;

namespace DiscFaithful
{
    /// <summary>
    /// Disco 3D completo. Possesso, lancio, rimbalzo laterale, collisione corpo/muro, parata, rotazione e trail.
    /// </summary>
    public class Disc : SyncScript
    {
        public RigidbodyComponent Rigidbody;
        public Entity Owner;
        public float Speed = 14f;
        public float SpinSpeed = 720f;

        public bool IsInFlight { get; private set; }

        public AnimationComponent Anim;
        public Entity VisualModel;
        public Entity TrailEntity;
        public Vector3 HandOffset = new Vector3(0.35f, 0.9f, 0.55f);

        public event Action<Disc, Entity> OnHitPlayer;
        public event Action<Disc, Tile> OnHitWallTile;

        private Quaternion flightSpin = Quaternion.Identity;

        public override void Start()
        {
            if (Rigidbody == null)
                Rigidbody = Entity.Get<RigidbodyComponent>();

            if (VisualModel == null)
                VisualModel = Entity.FindChild("Model") ?? Entity;

            if (Anim == null)
                Anim = Entity.Get<AnimationComponent>();

            if (TrailEntity == null)
                TrailEntity = Entity.FindChild("Trail");

            Rigidbody.IsKinematic = true;
            Rigidbody.CanSleep = false;
            IsInFlight = false;

            if (TrailEntity != null)
                TrailEntity.EnableAll(false, true);

            Rigidbody.NewCollision += OnNewCollision;
        }

        public override void Cancel()
        {
            if (Rigidbody != null)
                Rigidbody.NewCollision -= OnNewCollision;
        }

        public override void Update()
        {
            if (IsInFlight && VisualModel != null)
            {
                float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                flightSpin *= Quaternion.RotationYawPitchRoll(0, MathUtil.DegreesToRadians(SpinSpeed * dt), 0);
                VisualModel.Transform.Rotation = flightSpin;
            }
        }

        public void Throw(Vector3 direction)
        {
            if (Owner == null || IsInFlight) return;

            Entity.Transform.Parent = null;
            var ownerPos = Owner.Transform.WorldMatrix.TranslationVector;
            var offsetWorld = Vector3.TransformNormal(HandOffset, Owner.Transform.WorldMatrix);
            Entity.Transform.Position = ownerPos + offsetWorld;
            Entity.Transform.Rotation = Quaternion.Identity;

            direction.Y = 0.015f;
            direction = Vector3.Normalize(direction);

            Rigidbody.IsKinematic = false;
            Rigidbody.LinearVelocity = direction * Speed;
            Rigidbody.AngularVelocity = new Vector3(0, SpinSpeed * 0.017f, 0);

            IsInFlight = true;
            flightSpin = Quaternion.Identity;

            if (TrailEntity != null)
                TrailEntity.EnableAll(true, true);

            if (Anim != null && Anim.Animations.ContainsKey("Spin"))
                Anim.Play("Spin");
        }

        public void Catch(Entity newOwner)
        {
            Owner = newOwner;
            IsInFlight = false;

            Rigidbody.IsKinematic = true;
            Rigidbody.LinearVelocity = Vector3.Zero;
            Rigidbody.AngularVelocity = Vector3.Zero;

            Entity.Transform.Parent = newOwner.Transform;
            Entity.Transform.Position = HandOffset;
            Entity.Transform.Rotation = Quaternion.Identity;

            if (VisualModel != null)
                VisualModel.Transform.Rotation = Quaternion.Identity;

            if (TrailEntity != null)
                TrailEntity.EnableAll(false, true);

            if (Anim != null)
                Anim.Stop();
        }

        private void OnNewCollision(Collision collision)
        {
            if (!IsInFlight) return;

            Entity other = collision.ColliderA.Entity == Entity
                ? collision.ColliderB.Entity
                : collision.ColliderA.Entity;

            if (other.Get<SideWall>() != null)
                return;

            var tile = other.Get<Tile>();
            if (tile != null && tile.Type == Tile.TileType.Wall && !tile.IsDestroyed)
            {
                tile.TakeHit();
                OnHitWallTile?.Invoke(this, tile);
                return;
            }

            var player = other.Get<PlayerController>();
            if (player != null && player.Entity != Owner)
            {
                OnHitPlayer?.Invoke(this, player.Entity);

                if (player.IsBlocking)
                {
                    Catch(player.Entity);
                    player.ReceiveDisc(this);
                }
            }
        }
    }

    public class SideWall : StartupScript { }
}
