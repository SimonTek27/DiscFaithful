using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace DiscFaithful.UI
{
    /// <summary>
    /// HUD in-game contemporaneo: score, vita, livello, modalità.
    /// Layout leggero in alto, messaggi centrati con fade timer.
    /// </summary>
    public class GameHud : SyncScript
    {
        public GameManager GameManager;
        public UIComponent UI;

        public Color TextPrimary = new Color(245, 247, 250);
        public Color TextSecondary = new Color(160, 170, 190);
        public Color Accent = new Color(88, 140, 255);
        public Color Danger = new Color(255, 90, 90);

        private TextBlock modeLabel;
        private TextBlock scoreLabel;
        private TextBlock lifeLabel;
        private TextBlock levelLabel;
        private TextBlock messageLabel;

        private float messageTimer;

        public override void Start()
        {
            UI ??= Entity.Get<UIComponent>();
            BuildHud();

            if (GameManager != null)
            {
                GameManager.OnRoundReset += OnRoundReset;
                GameManager.OnMatchEnded += OnMatchEnded;
                GameManager.OnModeCompleted += OnModeCompleted;
                GameManager.OnModeChanged += OnModeChanged;
            }
        }

        public override void Cancel()
        {
            if (GameManager == null) return;
            GameManager.OnRoundReset -= OnRoundReset;
            GameManager.OnMatchEnded -= OnMatchEnded;
            GameManager.OnModeCompleted -= OnModeCompleted;
            GameManager.OnModeChanged -= OnModeChanged;
        }

        private void BuildHud()
        {
            UI ??= Entity.GetOrCreate<UIComponent>();

            var root = new Canvas { Name = "HudRoot" };
            UI.Page = new UIPage { RootElement = root };

            // Top bar background (subtle)
            var topBar = new Border
            {
                BackgroundColor = new Color(12, 14, 20, 180),
                Height = 56,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            root.Children.Add(topBar);
            Canvas.SetRelativePosition(topBar, new Vector3(0, 0, 0));
            Canvas.SetPinOrigin(topBar, new Vector3(0, 0, 0));

            modeLabel = MakeText("Mode", 13, TextSecondary);
            scoreLabel = MakeText("0  —  0", 22, TextPrimary);
            lifeLabel = MakeText("5  ·  5", 14, TextSecondary);
            levelLabel = MakeText("Level 1", 13, TextSecondary);
            messageLabel = MakeText("", 28, TextPrimary);

            Place(root, modeLabel, 0.12f, 0.035f);
            Place(root, scoreLabel, 0.5f, 0.03f);
            Place(root, lifeLabel, 0.5f, 0.065f);
            Place(root, levelLabel, 0.88f, 0.035f);
            Place(root, messageLabel, 0.5f, 0.42f);
        }

        private TextBlock MakeText(string text, float size, Color color)
        {
            return new TextBlock
            {
                Text = text,
                TextSize = size,
                TextColor = color,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void Place(Canvas canvas, UIElement el, float rx, float ry)
        {
            canvas.Children.Add(el);
            Canvas.SetPinOrigin(el, new Vector3(0.5f, 0.5f, 0));
            Canvas.SetRelativePosition(el, new Vector3(rx, ry, 0));
        }

        public override void Update()
        {
            if (GameManager == null) return;

            var cfg = GameManager.Config;
            modeLabel.Text = cfg?.DisplayName ?? "—";

            if (cfg != null && cfg.TrackScore)
                scoreLabel.Text = $"{GameManager.ScoreP1}  —  {GameManager.ScoreP2}";
            else
                scoreLabel.Text = "Practice";

            int l1 = GameManager.Player1?.CurrentLife ?? 0;
            int l2 = GameManager.Player2?.CurrentLife ?? 0;

            if (cfg != null && cfg.InfiniteLife)
            {
                lifeLabel.Text = "∞  ·  ∞";
                lifeLabel.TextColor = TextSecondary;
            }
            else
            {
                lifeLabel.Text = $"{l1}  ·  {l2}";
                lifeLabel.TextColor = (l1 <= 1 || l2 <= 1) ? Danger : TextSecondary;
            }

            levelLabel.Text = $"Level {GameManager.CurrentLevel}";

            if (messageTimer > 0f)
            {
                messageTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (messageTimer <= 0f)
                    messageLabel.Text = "";
            }
        }

        private void OnRoundReset()
        {
            ShowMessage("Round start", 1.4f);
        }

        private void OnMatchEnded(PlayerController winner, string reason)
        {
            string name = winner?.Entity.Name ?? "Player";
            ShowMessage($"{name} wins", 2.2f);
        }

        private void OnModeCompleted(GameModeType mode, PlayerController champ)
        {
            string name = champ?.Entity.Name ?? "Player";
            ShowMessage($"Champion — {name}", 3.5f);
        }

        private void OnModeChanged(GameModeConfig cfg)
        {
            ShowMessage(cfg.DisplayName, 1.8f);
        }

        public void ShowMessage(string text, float seconds)
        {
            messageLabel.Text = text;
            messageLabel.TextColor = TextPrimary;
            messageTimer = seconds;
        }
    }
}
