using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace DiscFaithful.UI
{
    /// <summary>
    /// HUD in-game stile 1990: score, vita, livello, modalità.
    /// Font mono / colori terminali, niente barre moderne.
    /// </summary>
    public class GameHud : SyncScript
    {
        public GameManager GameManager;
        public UIComponent UI;

        /// <summary>Menu modalità a cui tornare alla fine della modalità (opzionale).</summary>
        public ModeSelectUI Menu;

        public Color TextColor = new Color(0, 220, 0);
        public Color AlertColor = new Color(255, 60, 60);
        public Color TitleColor = new Color(255, 220, 0);

        private TextBlock modeLabel;
        private TextBlock scoreLabel;
        private TextBlock lifeLabel;
        private TextBlock levelLabel;
        private TextBlock messageLabel;

        private float messageTimer;
        private bool awaitingMenuReturn;
        private float menuPromptTimer;

        private const string MenuPrompt = "PRESS  A  FOR  MENU";

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
            if (UI == null)
                UI = Entity.GetOrCreate<UIComponent>();

            var root = new Canvas { Name = "HudRoot" };
            UI.Page = new UIPage { RootElement = root };

            modeLabel = MakeText("MODE", 14, TitleColor);
            scoreLabel = MakeText("P1 0 - 0 P2", 18, TextColor);
            lifeLabel = MakeText("LIFE 5 | 5", 14, TextColor);
            levelLabel = MakeText("LEVEL 1", 14, TextColor);
            messageLabel = MakeText("", 20, TitleColor);

            // Posizionamento stile arcade: alto
            Place(root, modeLabel, 0.5f, 0.04f);
            Place(root, scoreLabel, 0.5f, 0.09f);
            Place(root, lifeLabel, 0.5f, 0.14f);
            Place(root, levelLabel, 0.15f, 0.04f);
            Place(root, messageLabel, 0.5f, 0.45f);
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
            string modeName = cfg?.DisplayName ?? "—";

            modeLabel.Text = modeName;

            if (cfg != null && cfg.TrackScore)
                scoreLabel.Text = $"P1  {GameManager.ScoreP1}  -  {GameManager.ScoreP2}  P2";
            else
                scoreLabel.Text = "TRAINING";

            int l1 = GameManager.Player1?.CurrentLife ?? 0;
            int l2 = GameManager.Player2?.CurrentLife ?? 0;
            lifeLabel.Text = cfg != null && cfg.InfiniteLife
                ? "LIFE  --  |  --"
                : $"LIFE  {l1}  |  {l2}";

            levelLabel.Text = $"LEVEL {GameManager.CurrentLevel}";

            UpdateMenuReturn();

            if (messageTimer > 0f)
            {
                messageTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (messageTimer <= 0f && !awaitingMenuReturn)
                    messageLabel.Text = "";
            }
        }

        /// <summary>A modalità completata: dopo una pausa mostra il prompt e
        /// riporta al menu selezione modalità con A / Enter.</summary>
        private void UpdateMenuReturn()
        {
            if (!awaitingMenuReturn) return;

            menuPromptTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (menuPromptTimer <= 0f && messageLabel.Text != MenuPrompt)
            {
                messageLabel.Text = MenuPrompt;
                messageLabel.TextColor = TitleColor;
            }

            bool confirmPressed = Input.IsKeyPressed(Keys.Enter)
                               || Input.IsKeyPressed(Keys.Space);

            if (!confirmPressed)
            {
                var pad = Input.GamePadCount > 0 ? Input.GetGamePadByIndex(0) : default;
                confirmPressed = pad.IsButtonPressed(GamePadButton.A);
            }

            if (confirmPressed)
            {
                awaitingMenuReturn = false;
                messageLabel.Text = "";
                Menu?.ShowMenu();
            }
        }

        private void OnRoundReset()
        {
            ShowMessage("ROUND START", 1.5f);
        }

        private void OnMatchEnded(PlayerController winner, string reason)
        {
            string name = winner?.Entity.Name ?? "?";
            ShowMessage($"{name} WINS", 2.5f);
        }

        private void OnModeCompleted(GameModeType mode, PlayerController champ)
        {
            string name = champ?.Entity.Name ?? "?";
            ShowMessage($"CHAMPION: {name}", 4f);
            awaitingMenuReturn = true;
            menuPromptTimer = 3.5f;
        }

        private void OnModeChanged(GameModeConfig cfg)
        {
            awaitingMenuReturn = false;
            ShowMessage(cfg.DisplayName, 2f);
        }

        public void ShowMessage(string text, float seconds)
        {
            messageLabel.Text = text;
            messageLabel.TextColor = TitleColor;
            messageTimer = seconds;
        }
    }
}
