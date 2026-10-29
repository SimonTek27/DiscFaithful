using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Animations;

namespace DiscFaithful
{
    /// <summary>
    /// Controlli giocatore 3D + Xbox controller + animazioni.
    /// Regole 100% fedeli all'originale.
    /// </summary>
    public class PlayerController : SyncScript
    {
        public Platform MyPlatform;
        public Disc CurrentDisc;
        public int MaxLife = 5;
        public int CurrentLife;

        public bool IsBlocking { get; private set; }
        public bool IsDead { get; set; }

        public float MoveSpeed = 5.5f;
        public float RotationSpeed = 12f;
        public float StickDeadzone = 0.18f;
        public float FloorClampRadius = 1.15f;

        public int PlayerIndex = 0;

        public AnimationComponent Anim;
        public string AnimIdle    = "Idle";
        public string AnimWalk    = "Walk";
        public string AnimThrow   = "Throw";
        public string AnimBlock   = "Block";
        public string AnimHit     = "Hit";
        public string AnimFall    = "Fall";
        public string AnimVictory = "Victory";

        private string currentAnim = "";
        private float throwCooldown;

        public event Action<PlayerController> OnDeath;
        public event Action<PlayerController> OnFell;

        public override void Start()
        {
            CurrentLife = MaxLife;
            IsDead = false;

            if (Anim == null)
                Anim = Entity.Get<AnimationComponent>()
                    ?? Entity.FindChild("Model")?.Get<AnimationComponent>();

            PlayAnim(AnimIdle, true);
        }

        public override void Update()
        {
            if (IsDead) return;

            float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            throwCooldown = Math.Max(0f, throwCooldown - dt);

            HandleMovement(dt);
            HandleThrow();
            HandleBlock();
            CheckFall();
            UpdateAnimationState();
        }

        private GamePadState GetPad()
        {
            if (Input.GamePadCount > PlayerIndex)
                return Input.GetGamePadByIndex(PlayerIndex);
            return default;
        }

        private void HandleMovement(float dt)
        {
            var pad = GetPad();
            Vector2 stick = pad.LeftThumb;

            if (stick.Length() < StickDeadzone)
                stick = Vector2.Zero;
            else
            {
                float mag = (stick.Length() - StickDeadzone) / (1f - StickDeadzone);
                stick = Vector2.Normalize(stick) * mag;
            }

            Vector3 move = new Vector3(stick.X, 0f, -stick.Y);

            if (move.LengthSquared() > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(Vector3.Normalize(move), Vector3.UnitY);
                Entity.Transform.Rotation = Quaternion.Slerp(
                    Entity.Transform.Rotation,
                    targetRot,
                    Math.Clamp(RotationSpeed * dt, 0f, 1f));

                Entity.Transform.Position += move * MoveSpeed * dt;
                ClampToAliveFloor();
            }
        }

        private void ClampToAliveFloor()
        {
            if (MyPlatform == null) return;

            var closest = MyPlatform.GetClosestAliveFloor(Entity.Transform.Position);
            if (closest == null) return;

            Vector3 tilePos = closest.Entity.Transform.WorldMatrix.TranslationVector;
            Vector3 pos = Entity.Transform.Position;
            Vector3 offset = pos - tilePos;
            offset.Y = 0f;

            if (offset.Length() > FloorClampRadius)
            {
                offset = Vector3.Normalize(offset) * FloorClampRadius;
                Entity.Transform.Position = new Vector3(tilePos.X + offset.X, pos.Y, tilePos.Z + offset.Z);
            }
        }

        private void HandleThrow()
        {
            if (CurrentDisc == null || CurrentDisc.IsInFlight || throwCooldown > 0f)
                return;

            var pad = GetPad();
            bool throwPressed = pad.RightTrigger > 0.55f || pad.IsButtonPressed(GamePadButton.A);

            if (throwPressed)
            {
                Vector3 dir = Entity.Transform.WorldMatrix.Forward;

                Vector2 aim = pad.RightThumb;
                if (aim.Length() > StickDeadzone)
                {
                    dir = new Vector3(aim.X, 0f, -aim.Y);
                    dir = Vector3.Normalize(dir);
                }

                CurrentDisc.Throw(dir);
                PlayAnim(AnimThrow, false);
                throwCooldown = 0.45f;
            }
        }

        private void HandleBlock()
        {
            var pad = GetPad();
            IsBlocking = pad.IsButtonDown(GamePadButton.X)
                      || pad.IsButtonDown(GamePadButton.LeftShoulder)
                      || pad.LeftTrigger > 0.4f;
        }

        private void CheckFall()
        {
            if (MyPlatform != null && MyPlatform.HasNoFloorLeft())
            {
                IsDead = true;
                PlayAnim(AnimFall, false);
                OnFell?.Invoke(this);
                Enabled = false;
            }
        }

        private void UpdateAnimationState()
        {
            if (IsDead) return;

            if (IsBlocking)
            {
                PlayAnim(AnimBlock, true);
                return;
            }

            var pad = GetPad();
            bool isMoving = pad.LeftThumb.Length() > StickDeadzone;

            PlayAnim(isMoving ? AnimWalk : AnimIdle, true);
        }

        private void PlayAnim(string name, bool loop)
        {
            if (Anim == null || string.IsNullOrEmpty(name) || currentAnim == name)
                return;

            if (Anim.Animations.ContainsKey(name))
            {
                Anim.Play(name);
                currentAnim = name;
            }
        }

        public void TakeDamage(int amount = 1)
        {
            if (IsDead) return;

            CurrentLife -= amount;
            PlayAnim(AnimHit, false);

            if (CurrentLife <= 0)
            {
                CurrentLife = 0;
                IsDead = true;
                PlayAnim(AnimFall, false);
                OnDeath?.Invoke(this);
                Enabled = false;
            }
        }

        public void ReceiveDisc(Disc disc)
        {
            CurrentDisc = disc;
            disc.Catch(Entity);
        }

        public void PlayVictory()
        {
            PlayAnim(AnimVictory, true);
        }
    }
}
