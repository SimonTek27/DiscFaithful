using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Stride.Animations;

namespace DiscFaithful
{
    /// <summary>
    /// Piastrella 3D (Floor o Wall) – fedele all'originale.
    /// Solo le Wall ricevono colpi. A 0 hits spariscono wall + floor collegata.
    /// Simboli: Pentagon → Square → Triangle → Equals → Circle → destroy.
    /// </summary>
    public class Tile : SyncScript
    {
        public enum TileType { Floor, Wall }

        public TileType Type = TileType.Floor;

        /// <summary>0-7 (riga*4 + colonna). Identico tra floor e wall corrispondenti.</summary>
        public int Index = 0;

        public int HitsRemaining = 0;
        public bool IsDestroyed { get; private set; }

        public Tile LinkedTile;

        public AnimationComponent Anim;
        public string AnimHit = "Hit";
        public string AnimDestroy = "Destroy";
        public Entity SymbolRoot;
        public float HitFlashDuration = 0.18f;

        public event Action<Tile> OnDestroyed;
        public event Action<Tile, int> OnHit;

        private float flashTimer;

        public override void Start()
        {
            if (Type == TileType.Floor)
                HitsRemaining = 0;

            Anim ??= Entity.Get<AnimationComponent>()
                ?? Entity.FindChild("Model")?.Get<AnimationComponent>();

            SymbolRoot ??= Entity.FindChild("Symbol");
            UpdateSymbolVisual();
        }

        public override void Update()
        {
            if (flashTimer > 0f)
                flashTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
        }

        public void TakeHit()
        {
            if (IsDestroyed || Type != TileType.Wall)
                return;

            HitsRemaining--;
            flashTimer = HitFlashDuration;

            if (Anim != null && Anim.Animations.ContainsKey(AnimHit))
                Anim.Play(AnimHit);

            UpdateSymbolVisual();
            OnHit?.Invoke(this, HitsRemaining);

            if (HitsRemaining <= 0)
                DestroyTile();
        }

        public void DestroyTile()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            // Disabilita collider fisici (pavimento sparisce → rischio caduta)
            var staticCol = Entity.Get<StaticColliderComponent>();
            if (staticCol != null) staticCol.Enabled = false;

            var rb = Entity.Get<RigidbodyComponent>();
            if (rb != null) rb.Enabled = false;

            if (Anim != null && Anim.Animations.ContainsKey(AnimDestroy))
            {
                Anim.Play(AnimDestroy);
                DelayedDisable(1.15f);
            }
            else
            {
                ScaleDownAndDisable(0.55f);
            }

            // Distrugge anche la piastrella collegata (floor ↔ wall)
            if (LinkedTile != null && !LinkedTile.IsDestroyed)
                LinkedTile.DestroyTile();

            OnDestroyed?.Invoke(this);
        }

        /// <summary>Ripristina la piastrella per un nuovo round.</summary>
        public void ResetTile(int hits)
        {
            IsDestroyed = false;
            HitsRemaining = Type == TileType.Wall ? hits : 0;

            var staticCol = Entity.Get<StaticColliderComponent>();
            if (staticCol != null) staticCol.Enabled = true;

            var rb = Entity.Get<RigidbodyComponent>();
            if (rb != null) rb.Enabled = true;

            Entity.EnableAll(true, true);
            Entity.Transform.Scale = Vector3.One;
            UpdateSymbolVisual();
        }

        private async void DelayedDisable(float seconds)
        {
            await Task.Delay((int)(seconds * 1000));
            if (Entity != null)
                Entity.EnableAll(false, true);
        }

        private async void ScaleDownAndDisable(float duration)
        {
            Vector3 start = Entity.Transform.Scale;
            float t = 0f;
            while (t < duration && Entity != null)
            {
                t += (float)Game.UpdateTime.Elapsed.TotalSeconds;
                float k = 1f - Math.Clamp(t / duration, 0f, 1f);
                Entity.Transform.Scale = start * k;
                await Script.NextFrame();
            }
            if (Entity != null)
                Entity.EnableAll(false, true);
        }

        private void UpdateSymbolVisual()
        {
            if (SymbolRoot == null) return;

            string symbol = GetCurrentSymbol();
            foreach (var child in SymbolRoot.GetChildren())
                child.EnableAll(child.Name == symbol, true);
        }

        public string GetCurrentSymbol()
        {
            return HitsRemaining switch
            {
                5 => "Pentagon",
                4 => "Square",
                3 => "Triangle",
                2 => "Equals",
                1 => "Circle",
                _ => "Destroyed"
            };
        }
    }
}
