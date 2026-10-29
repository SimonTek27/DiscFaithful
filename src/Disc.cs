using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Stride.Animations;

namespace DiscFaithful
{
    /// <summary>
    /// Disco con fisica vera 3D (Stride/Bullet).
    /// - In mano: kinematic
    /// - In volo: dynamic, massa, restitution alta, CCD, gravità zero sul piano di gioco
    /// - Rimbalzo muri laterali gestito dal motore fisico
    /// - Collisione piastrella muro / corpo giocatore gestita via eventi
    /// </summary>
    public class Disc : SyncScript
    {
        // ---- Riferimenti ----
        public RigidbodyComponent Rigidbody;
        public Entity Owner;
        public AnimationComponent Anim;
        public Entity VisualModel;
        public Entity TrailEntity;

        // ---- Parametri fisica vera ----
        public float Speed = 14f;
        public float SpinSpeed = 720f;          // gradi/secondo (visual spin)
        public float Mass = 0.5f;
        public float Restitution = 0.98f;       // rimbalzo quasi elastico
        public float LinearDamping = 0.05f;
        public float AngularDamping = 0.15f;
        public float CcdMotionThreshold = 0.1f;
        public float CcdSweptSphereRadius = 0.3f;

        /// <summary>Offset locale della mano rispetto al player.</summary>
        public Vector3 HandOffset = new Vector3(0.35f, 0.9f, 0.55f);

        public bool IsInFlight { get; private set; }

        /// <summary>Ultimo giocatore che l'ha avuto in mano (per la restituzione se esce dall'arena).</summary>
        public Entity LastOwner;

        // ---- Limiti arena ----
        /// <summary>Se il disco scende sotto questa altezza è uscito dall'arena.</summary>
        public float OutOfBoundsY = -3f;

        /// <summary>Volo oltre questi secondi senza pesci prese → fuori.</summary>
        public float MaxFlightSeconds = 12f;

        public event Action<Disc, Entity> OnHitPlayer;
        public event Action<Disc, Tile> OnHitWallTile;
        public event Action<Disc> OnOutOfBounds;

        private Quaternion flightSpin = Quaternion.Identity;
        private float flightTime;

        public override void Start()
        {
            Rigidbody ??= Entity.Get<RigidbodyComponent>();
            if (Rigidbody == null)
            {
                Log.Error("[Disc] RigidbodyComponent mancante sull'entity.");
                return;
            }

            VisualModel ??= Entity.FindChild("Model") ?? Entity;
            Anim ??= Entity.Get<AnimationComponent>();
            TrailEntity ??= Entity.FindChild("Trail");

            ApplyPhysicsSettings();

            // In mano all'inizio
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

        /// <summary>
        /// Applica massa, damping, CCD e gravità zero (piano di gioco).
        /// </summary>
        private void ApplyPhysicsSettings()
        {
            Rigidbody.Mass = Mass;
            Rigidbody.LinearDamping = LinearDamping;
            Rigidbody.AngularDamping = AngularDamping;

            // Piano di gioco: niente caduta verticale
            Rigidbody.OverrideGravity = true;
            Rigidbody.Gravity = Vector3.Zero;

            // Continuous Collision Detection (evita tunneling a alta velocità)
            Rigidbody.CcdMotionThreshold = CcdMotionThreshold;
            Rigidbody.CcdSweptSphereRadius = CcdSweptSphereRadius;

            // Restitution: in Stride si imposta spesso sul PhysicsMaterial del collider.
            // Se il componente espone Restitution direttamente:
            try
            {
                Rigidbody.Restitution = Restitution;
            }
            catch
            {
                // Fallback: configura il materiale fisico sull'entity in editor
                // Phys_Disc → Restitution 0.98, Friction 0.1
            }
        }

        public override void Update()
        {
            if (!IsInFlight || VisualModel == null)
                return;

            // Spin visivo indipendente (il motore già applica AngularVelocity)
            float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            flightSpin *= Quaternion.RotationYawPitchRoll(
                0f,
                MathUtil.DegreesToRadians(SpinSpeed * dt),
                0f);
            VisualModel.Transform.Rotation = flightSpin;

            // Fuori dall'arena o volo troppo lungo → segnala per la restituzione
            flightTime += dt;
            if (Entity.Transform.Position.Y < OutOfBoundsY || flightTime > MaxFlightSeconds)
                OnOutOfBounds?.Invoke(this);
        }

        /// <summary>
        /// Lancia il disco in direzione (normalizzata sul piano XZ).
        /// Passa da kinematic → dynamic e applica impulso di velocità.
        /// </summary>
        public void Throw(Vector3 direction)
        {
            if (Owner == null || IsInFlight)
                return;

            // Stacca dal player
            Entity.Transform.Parent = null;

            var ownerPos = Owner.Transform.WorldMatrix.TranslationVector;
            var offsetWorld = Vector3.TransformNormal(HandOffset, Owner.Transform.WorldMatrix);
            Entity.Transform.Position = ownerPos + offsetWorld;
            Entity.Transform.Rotation = Quaternion.Identity;

            // Solo piano orizzontale (fisica vera ma gameplay stile originale)
            direction.Y = 0f;
            if (direction.LengthSquared() < 1e-6f)
                direction = Owner.Transform.WorldMatrix.Forward;
            direction = Vector3.Normalize(direction);

            // Sync transform → physics prima di rendere dynamic
            Rigidbody.IsKinematic = false;
            Rigidbody.UpdatePhysicsTransformation();

            Rigidbody.LinearVelocity = direction * Speed;
            Rigidbody.AngularVelocity = new Vector3(
                0f,
                SpinSpeed * MathUtil.DegreesToRadians(1f),
                0f);

            IsInFlight = true;
            flightSpin = Quaternion.Identity;
            flightTime = 0f;
            LastOwner = Owner;
            Owner = null; // non più in mano finché non viene preso

            if (TrailEntity != null)
                TrailEntity.EnableAll(true, true);

            if (Anim != null && Anim.Animations.ContainsKey("Spin"))
                Anim.Play("Spin");
        }

        /// <summary>
        /// Prende il disco: ferma la fisica e lo attacca alla mano del nuovo owner.
        /// </summary>
        public void Catch(Entity newOwner)
        {
            if (newOwner == null)
                return;

            Owner = newOwner;
            LastOwner = newOwner;
            IsInFlight = false;
            flightTime = 0f;

            Rigidbody.LinearVelocity = Vector3.Zero;
            Rigidbody.AngularVelocity = Vector3.Zero;
            Rigidbody.IsKinematic = true;
            Rigidbody.UpdatePhysicsTransformation();

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
            if (!IsInFlight)
                return;

            Entity other = collision.ColliderA.Entity == Entity
                ? collision.ColliderB.Entity
                : collision.ColliderA.Entity;

            if (other == null)
                return;

            // Muri laterali: rimbalzo gestito interamente dalla fisica (restitution).
            // Non intercettare, lascia fare al motore.
            if (other.Get<SideWall>() != null)
                return;

            // Piastrella muro → ciclo danni / distruzione
            var tile = other.Get<Tile>();
            if (tile != null && tile.Type == Tile.TileType.Wall && !tile.IsDestroyed)
            {
                tile.TakeHit();
                OnHitWallTile?.Invoke(this, tile);

                // Piccolo impulso opzionale per non "attaccarsi" alla piastrella
                // (la restitution del materiale fa già il lavoro principale)
                return;
            }

            // Colpo a un giocatore
            var player = other.Get<PlayerController>();
            if (player != null)
            {
                // Non colpire chi lo ha appena lanciato nello stesso frame
                if (Owner != null && player.Entity == Owner)
                    return;

                OnHitPlayer?.Invoke(this, player.Entity);

                if (player.IsBlocking)
                {
                    Catch(player.Entity);
                    player.ReceiveDisc(this);
                }
                // Se non sta parando: il danno è gestito da GameManager via OnHitPlayer
            }
        }
    }

    /// <summary>
    /// Marker per i muri laterali (rimbalzo fisico puro).
    /// </summary>
    public class SideWall : StartupScript { }
}
