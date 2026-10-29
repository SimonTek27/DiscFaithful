using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Avversario CPU – fedele allo stile originale Disc (Loriciel 1990):
    /// - Para i dischi in arrivo (la parata cattura il disco, come nell'originale)
    /// - Elude se la probabilità di parata non scatta
    /// - Lancia verso l'avversario con errore di mira che scala col livello
    /// - Si posiziona al centro delle floor rimaste (come i CPU originali)
    /// La difficoltà è derivata da GameManager.CurrentLevel, come nell'originale
    /// dove i CPU diventano progressivamente più reattivi e precisi.
    /// </summary>
    public class AiOpponent : SyncScript
    {
        // ---- Riferimenti ----
        public PlayerController Self;
        public PlayerController Opponent;
        public GameManager Manager;
        public Platform MyPlatform;

        // ---- Difficoltà (override manuale se UseManualDifficulty) ----
        public bool UseManualDifficulty;

        /// <summary>Probabilità al secondo di avviare la parata su un disco minacciato.</summary>
        public float BlockChancePerSecond = 1.5f;

        /// <summary>Probabilità al secondo di schivare invece di parare.</summary>
        public float DodgeChance = 0.5f;

        /// <summary>Errore massimo di mira in gradi.</summary>
        public float AimErrorDegrees = 10f;

        /// <summary>Ritardo (s) prima del lancio dopo aver preso il disco.</summary>
        public float ThrowDelayMin = 0.8f;
        public float ThrowDelayMax = 2.0f;

        /// <summary>Reattività: tempo (s) prima di reagire a una minaccia.</summary>
        public float ReactionTime = 0.25f;

        // ---- Tuning comportamento ----
        public float ThreatLookaheadSeconds = 1.0f;
        public float ThreatRadius = 2.1f;
        public float BlockDuration = 0.9f;
        public float FacingSpeedFactor = 0.35f;   // quanto si muove mentre mira
        public float WanderRadius = 0.35f;        // micro-movimento quando fermo

        private readonly Random rng = new();

        private float blockTimer;
        private float reactionTimer;
        private float throwCooldownTimer;
        private float holdTimer;
        private float currentThrowDelay;
        private float wanderTimer;
        private Vector2 wanderOffset;
        private bool hadDisc;
        private bool dodgeActive;

        public override void Start()
        {
            if (Self == null)
                Log.Error("[AiOpponent] Self (PlayerController) non assegnato.");
        }

        /// <summary>
        /// Difficoltà scalata col livello (1-8), come i CPU originali:
        /// parate più frequenti, mira più precisa, lanci più rapidi.
        /// </summary>
        public void SetDifficultyFromLevel(int level)
        {
            if (UseManualDifficulty) return;

            int l = Math.Clamp(level, 1, 8);
            BlockChancePerSecond = 0.9f + 0.25f * l;
            DodgeChance = Math.Clamp(0.65f - 0.05f * l, 0.15f, 0.6f);
            AimErrorDegrees = Math.Max(1.5f, 14f - 2f * l);
            ThrowDelayMin = Math.Max(0.25f, 1.1f - 0.12f * l);
            ThrowDelayMax = Math.Max(0.6f, 2.2f - 0.2f * l);
            ReactionTime = Math.Max(0.08f, 0.35f - 0.04f * l);
        }

        public override void Update()
        {
            if (Self == null || Self.IsDead)
            {
                Self?.SetAiInput(Vector2.Zero, false, false, Vector3.UnitZ);
                return;
            }

            float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            blockTimer = Math.Max(0f, blockTimer - dt);
            reactionTimer -= dt;
            throwCooldownTimer = Math.Max(0f, throwCooldownTimer - dt);
            holdTimer += dt;
            wanderTimer -= dt;

            var myPos = Self.Entity.Transform.WorldMatrix.TranslationVector;

            // ---- 1. Minacce: disco in volo diretto verso di noi ----
            Disc incoming = FindIncomingDisc(myPos);

            bool wantBlock = false;
            Vector2 move = Vector2.Zero;

            if (incoming != null)
            {
                if (reactionTimer <= 0f)
                {
                    // Decide una volta sola tra parata e schivata
                    if (Roll(BlockChancePerSecond * dt))
                    {
                        blockTimer = BlockDuration;
                        reactionTimer = ReactionTime;
                        dodgeActive = false;
                    }
                    else if (Roll(DodgeChance * dt))
                    {
                        StartDodge(incoming, myPos);
                        reactionTimer = ReactionTime;
                    }
                }
            }
            else
            {
                dodgeActive = false;
            }

            if (dodgeActive && blockTimer <= 0f)
            {
                move = ComputeDodgeMove(myPos);
            }
            else if (blockTimer > 0f)
            {
                // In parata: fermo, trasverso leggero (stile originale)
                wantBlock = true;
                move = wanderOffset * 0.15f;
            }
            else
            {
                // ---- 2. Comportamento di default ----
                move = ComputeDefaultMove(myPos);
            }

            // ---- 3. Micro-wander (i CPU originali non stanno mai perfettamente fermi) ----
            if (wanderTimer <= 0f)
            {
                wanderTimer = 0.8f + (float)rng.NextDouble() * 1.2f;
                float a = (float)rng.NextDouble() * MathF.PI * 2f;
                wanderOffset = new Vector2(MathF.Cos(a), MathF.Sin(a)) * WanderRadius;
            }

            // ---- 4. Lancio ----
            bool wantThrow = false;
            Vector3 aimDir = ComputeAim();

            if (!wantBlock && throwCooldownTimer <= 0f && Self.CurrentDisc != null && !Self.CurrentDisc.IsInFlight)
            {
                if (!hadDisc)
                {
                    hadDisc = true;
                    holdTimer = 0f;
                    currentThrowDelay = ThrowDelayMin + (float)rng.NextDouble() * (ThrowDelayMax - ThrowDelayMin);
                }

                if (holdTimer >= currentThrowDelay)
                {
                    wantThrow = true;
                    holdTimer = 0f;
                    throwCooldownTimer = 0.3f + (float)rng.NextDouble() * 0.5f;
                }
            }
            else if (Self.CurrentDisc == null)
            {
                hadDisc = false;
            }

            move += wanderOffset * 0.2f;
            Self.SetAiInput(move, wantBlock, wantThrow, aimDir);
        }

        // ---------- Minacce ----------

        private Disc FindIncomingDisc(Vector3 myPos)
        {
            if (Manager == null) return null;

            Disc best = null;
            float bestT = float.MaxValue;

            foreach (var d in Manager.ActiveDiscs)
            {
                if (d?.Entity == null || !d.IsInFlight) continue;
                if (d.Owner == Self.Entity) continue;      // in mano a noi
                if (d.Owner != null) continue;             // in mano a qualcuno

                var rb = d.Rigidbody;
                if (rb == null) continue;

                var vel = rb.LinearVelocity;
                if (vel.LengthSquared() < 0.5f) continue;

                var pos = d.Entity.Transform.Position;

                // Tempo di minima distanza dalla traiettoria
                float t = Vector3.Dot(myPos - pos, vel) / vel.LengthSquared();
                if (t < 0f || t > ThreatLookaheadSeconds) continue;

                var closest = pos + vel * t;
                float distSq = Vector3.DistanceSquared(closest, myPos);
                if (distSq > ThreatRadius * ThreatRadius) continue;

                if (t < bestT)
                {
                    bestT = t;
                    best = d;
                }
            }

            return best;
        }

        private void StartDodge(Disc disc, Vector3 myPos)
        {
            dodgeActive = true;
        }

        /// <summary>
        /// Movimento perpendicolare alla traiettoria del disco,
        /// orientato verso il centro delle floor restanti (per non uscire).
        /// </summary>
        private Vector2 ComputeDodgeMove(Vector3 myPos)
        {
            var v = ComputePerpDirection(myPos);
            return WorldToStick(v);
        }

        private Vector3 ComputePerpDirection(Vector3 myPos)
        {
            // Perpendicolari all'asse piattaforme (le due piattaforme si fronteggiano su Z)
            Vector3 towardCenter = SafeSpot() - myPos;
            towardCenter.Y = 0f;

            Vector3 side = new Vector3(1f, 0f, 0f);
            if (towardCenter.LengthSquared() > 1e-4f)
            {
                towardCenter.Normalize();
                // Priorità: rientrare sul materiale vivo
                if (Vector3.Dot(towardCenter, side) < 0f)
                    side = -side;
                return Vector3.Normalize(side + towardCenter * 1.3f);
            }
            return side;
        }

        // ---------- Movimento di default ----------

        private Vector2 ComputeDefaultMove(Vector3 myPos)
        {
            if (Self.CurrentDisc != null && !Self.CurrentDisc.IsInFlight)
            {
                // Ha il disco: si posiziona per mirare (piccolo aggiustamento)
                Vector3 target = FacingSpot();
                Vector3 dir = target - myPos;
                dir.Y = 0f;
                return dir.LengthSquared() > 0.04f
                    ? WorldToStick(Vector3.Normalize(dir)) * FacingSpeedFactor
                    : Vector2.Zero;
            }

            // Senza disco: si tiene al centro delle floor vive (posizione difensiva)
            Vector3 spot = SafeSpot();
            Vector3 toSpot = spot - myPos;
            toSpot.Y = 0f;
            return toSpot.LengthSquared() > 0.02f
                ? WorldToStick(Vector3.Normalize(toSpot))
                : Vector2.Zero;
        }

        /// <summary>Posizione di lancio: dietro il centro della piattaforma.</summary>
        private Vector3 FacingSpot()
        {
            var center = SafeSpot();
            if (Opponent?.Entity == null) return center;

            var oppPos = Opponent.Entity.Transform.WorldMatrix.TranslationVector;
            Vector3 dir = center - oppPos;
            dir.Y = 0f;

            // Si mette a 1 tile dal centro, sul lato opposto all'avversario
            return dir.LengthSquared() > 1e-4f
                ? center + Vector3.Normalize(dir) * 0.6f
                : center;
        }

        // ---------- Mira ----------

        private Vector3 ComputeAim()
        {
            if (Opponent?.Entity == null || Opponent.IsDead)
            {
                var fwd = Self.Entity.Transform.WorldMatrix.Forward;
                fwd.Y = 0f;
                return fwd.LengthSquared() > 1e-6f ? Vector3.Normalize(fwd) : Vector3.UnitZ;
            }

            var oppPos = Opponent.Entity.Transform.WorldMatrix.TranslationVector;
            var myPos = Self.Entity.Transform.WorldMatrix.TranslationVector;

            Vector3 dir = oppPos - myPos;
            dir.Y = 0f;
            if (dir.LengthSquared() < 1e-6f)
                return Vector3.UnitZ;

            // Errore di mira: angolo casuale ±AimErrorDegrees intorno all'avversario.
            // Il disco rimbalza sui SideWall, quindi anche un errore può andare a segno.
            float errorDeg = AimErrorDegrees == 0f
                ? 0f
                : ((float)rng.NextDouble() * 2f - 1f) * AimErrorDegrees;

            float errorRad = MathUtil.DegreesToRadians(errorDeg);
            float cos = MathF.Cos(errorRad);
            float sin = MathF.Sin(errorRad);

            Vector3 n = Vector3.Normalize(dir);
            // Rotazione attorno a Y (x,z)
            return new Vector3(n.X * cos - n.Z * sin, 0f, n.X * sin + n.Z * cos);
        }

        // ---------- Utility ----------

        private Vector3 SafeSpot()
        {
            if (MyPlatform == null || Self?.Entity == null)
                return Self?.Entity?.Transform.Position ?? Vector3.Zero;

            var acc = Vector3.Zero;
            int n = 0;
            foreach (var f in MyPlatform.FloorTiles)
            {
                if (f?.Entity == null || f.IsDestroyed) continue;
                acc += f.Entity.Transform.WorldMatrix.TranslationVector;
                n++;
            }

            return n > 0 ? acc / n : Self.Entity.Transform.Position;
        }

        /// <summary>Direzione mondo → stick (PlayerController: move.x = stick.X, move.z = -stick.Y).</summary>
        private static Vector2 WorldToStick(Vector3 dir)
        {
            dir.Y = 0f;
            if (dir.LengthSquared() < 1e-6f) return Vector2.Zero;
            dir = Vector3.Normalize(dir);
            return new Vector2(dir.X, -dir.Z);
        }

        private bool Roll(float probabilityPerSecond)
        {
            if (probabilityPerSecond <= 0f) return false;
            return rng.NextDouble() < probabilityPerSecond;
        }
    }
}
