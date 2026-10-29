using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Animations;

namespace DiscFaithful
{
    /// <summary>
    /// Controlli giocatore 3D + Xbox pad.
    /// Movimento vincolato alle piastrelle vive, lancio, parata, vita, caduta.
    /// Supporta 3 sorgenti di input:
    ///   - Gamepad Xbox (default, un pad per PlayerIndex)
    ///   - AI (AiOpponent) quando AiControlled = true
    ///   - Tastiera come fallback se non c'è gamepad per questo PlayerIndex
    /// </summary>
    public class PlayerController : SyncScript
    {
        public Platform MyPlatform;
        public Disc CurrentDisc;

        public int MaxLife = 5;
        public int CurrentLife;

        public bool IsBlocking { get; private set; }
        public bool IsDead { get; set; }

        /// <summary>Se true, l'input arriva da SetAiInput() (AiOpponent).</summary>
        public bool AiControlled;

        public float MoveSpeed = 5.5f;
        public float RotationSpeed = 12f;
        public float StickDeadzone = 0.18f;
        public float FloorClampRadius = 1.15f;
        public float ThrowCooldownSeconds = 0.45f;

        public int PlayerIndex = 0;

        public AnimationComponent Anim;
        public string AnimIdle = "Idle";
        public string AnimWalk = "Walk";
        public string AnimThrow = "Throw";
        public string AnimBlock = "Block";
        public string AnimHit = "Hit";
        public string AnimFall = "Fall";
        public string AnimVictory = "Victory";

        public event Action<PlayerController> OnDeath;
        public event Action<PlayerController> OnFell;

        private string currentAnim = "";
        private float throwCooldown;

        // ---- Input AI (usati solo con AiControlled) ----
        private Vector2 aiMove;
        private bool aiBlocking;
        private bool aiWantThrow;
        private Vector3 aiThrowDir = Vector3.UnitZ;

        public override void Start()
        {
            CurrentLife = MaxLife;
            IsDead = false;

            Anim ??= Entity.Get<AnimationComponent>()
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

        /// <summary>
        /// Input dell'IA. Chiamato ogni frame da AiOpponent.
        /// move: direzione desiderata in coordinate stick (X = mondo X, Y = -mondo Z).
        /// throwDir: direzione mondo di lancio.
        /// </summary>
        public void SetAiInput(Vector2 move, bool blocking, bool wantThrow, Vector3 throwDir)
        {
            aiMove = move;
            aiBlocking = blocking;
            aiWantThrow = wantThrow;
            aiThrowDir = throwDir;
        }

        private GamePadState GetPad()
        {
            if (Input.GamePadCount > PlayerIndex)
                return Input.GetGamePadByIndex(PlayerIndex);
            return default;
        }

        /// <summary>True se non c'è un gamepad per questo giocatore → fallback tastiera.</summary>
        private bool UsingKeyboard => !AiControlled && Input.GamePadCount <= PlayerIndex;

        /// <summary>Stick della sorgente attiva (AI, pad o tastiera). Deadzone applicata dopo.</summary>
        private Vector2 GetMoveStick()
        {
            if (AiControlled) return aiMove;
            if (Input.GamePadCount > PlayerIndex)
                return GetPad().LeftThumb;
            return KeyboardStick();
        }

        private Vector2 KeyboardStick()
        {
            Keys up, down, left, right;
            if (PlayerIndex == 0) { up = Keys.W; down = Keys.S; left = Keys.A; right = Keys.D; }
            else { up = Keys.Up; down = Keys.Down; left = Keys.Left; right = Keys.Right; }

            float x = (Input.IsKeyDown(right) ? 1f : 0f) - (Input.IsKeyDown(left) ? 1f : 0f);
            float y = (Input.IsKeyDown(up) ? 1f : 0f) - (Input.IsKeyDown(down) ? 1f : 0f);

            var stick = new Vector2(x, y);
            if (stick.Length() > 1f)
                stick = Vector2.Normalize(stick);
            return stick;
        }

        private void HandleMovement(float dt)
        {
            Vector2 stick = GetMoveStick();

            stick = ApplyDeadzone(stick);

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

            // Tastiera: rotazione di mira dedicata (senza dover camminare)
            if (UsingKeyboard)
                HandleKeyboardAiming(dt);
        }

        private Vector2 ApplyDeadzone(Vector2 stick)
        {
            if (stick.Length() < StickDeadzone)
                return Vector2.Zero;

            float mag = (stick.Length() - StickDeadzone) / (1f - StickDeadzone);
            return Vector2.Normalize(stick) * Math.Clamp(mag, 0f, 1f);
        }

        /// <summary>Solo tastiera: Q/E (P1) o O/P (P2) ruotano il personaggio per mirare.</summary>
        private void HandleKeyboardAiming(float dt)
        {
            Keys rotLeft = PlayerIndex == 0 ? Keys.Q : Keys.O;
            Keys rotRight = PlayerIndex == 0 ? Keys.E : Keys.P;

            float yaw = (Input.IsKeyDown(rotLeft) ? 1f : 0f) - (Input.IsKeyDown(rotRight) ? 1f : 0f);
            if (yaw == 0f) return;

            float speed = (float)Math.PI * 1.6f * dt; // ~290°/s
            var rot = Entity.Transform.Rotation;
            var yawQuat = Quaternion.RotationAxis(Vector3.UnitY, yaw * speed);
            Entity.Transform.Rotation = yawQuat * rot;
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
                Entity.Transform.Position = new Vector3(
                    tilePos.X + offset.X,
                    pos.Y,
                    tilePos.Z + offset.Z);
            }
        }

        private void HandleThrow()
        {
            if (CurrentDisc == null || CurrentDisc.IsInFlight || throwCooldown > 0f)
                return;

            bool throwPressed;
            Vector3 dir;

            if (AiControlled)
            {
                throwPressed = aiWantThrow;
                dir = aiThrowDir.LengthSquared() > 0.01f ? aiThrowDir
                    : Entity.Transform.WorldMatrix.Forward;
            }
            else if (UsingKeyboard)
            {
                Keys throwKey = PlayerIndex == 0 ? Keys.Space : Keys.Enter;
                throwPressed = Input.IsKeyPressed(throwKey);
                dir = Entity.Transform.WorldMatrix.Forward;
            }
            else
            {
                var pad = GetPad();
                throwPressed = pad.RightTrigger > 0.55f
                              || pad.IsButtonPressed(GamePadButton.A);
                dir = Entity.Transform.WorldMatrix.Forward;

                Vector2 aim = pad.RightThumb;
                if (aim.Length() > StickDeadzone)
                {
                    dir = Vector3.Normalize(new Vector3(aim.X, 0f, -aim.Y));
                }
            }

            if (!throwPressed) return;

            CurrentDisc.Throw(dir);
            CurrentDisc = null;
            PlayAnim(AnimThrow, false);
            throwCooldown = ThrowCooldownSeconds;
        }

        private void HandleBlock()
        {
            if (AiControlled)
            {
                IsBlocking = aiBlocking;
                return;
            }

            if (UsingKeyboard)
            {
                Keys blockKey = PlayerIndex == 0 ? Keys.LeftShift : Keys.RightShift;
                IsBlocking = Input.IsKeyDown(blockKey);
                return;
            }

            var pad = GetPad();
            IsBlocking = pad.IsButtonDown(GamePadButton.X)
                      || pad.IsButtonDown(GamePadButton.LeftShoulder)
                      || pad.LeftTrigger > 0.4f;
        }

        private void CheckFall()
        {
            if (MyPlatform == null) return;

            if (MyPlatform.HasNoFloorLeft())
            {
                DieFromFall();
                return;
            }

            var under = MyPlatform.GetClosestAliveFloor(Entity.Transform.Position);
            if (under == null)
                DieFromFall();
        }

        private void DieFromFall()
        {
            if (IsDead) return;
            IsDead = true;
            PlayAnim(AnimFall, false);
            OnFell?.Invoke(this);
            Enabled = false;
        }

        private void UpdateAnimationState()
        {
            if (IsDead) return;

            if (IsBlocking)
            {
                PlayAnim(AnimBlock, true);
                return;
            }

            if (currentAnim == AnimThrow || currentAnim == AnimHit)
                return;

            bool isMoving = GetMoveStick().Length() > StickDeadzone;
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

        /// <summary>Solo feedback visuale (Training con vita infinita).</summary>
        public void PlayHitFeedback()
        {
            PlayAnim(AnimHit, false);
        }

        public void ReceiveDisc(Disc disc)
        {
            if (disc == null) return;
            CurrentDisc = disc;
            disc.Catch(Entity);
        }

        public void PlayVictory()
        {
            PlayAnim(AnimVictory, true);
        }

        public void ResetForRound()
        {
            CurrentLife = MaxLife;
            IsDead = false;
            IsBlocking = false;
            CurrentDisc = null;
            Enabled = true;
            currentAnim = "";
            PlayAnim(AnimIdle, true);
        }
    }
}
