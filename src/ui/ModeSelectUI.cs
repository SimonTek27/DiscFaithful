using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace DiscFaithful.UI
{
    /// <summary>
    /// Menu selezione modalità – stile Amiga/DOS 1990.
    /// Testi a blocchi, niente UI moderna.
    /// Controlli: D-Pad / freccia su-giù, A conferma, B indietro.
    /// </summary>
    public class ModeSelectUI : SyncScript
    {
        public GameManager GameManager;
        public UIComponent UI;

        public Color BgColor = new Color(0, 0, 40);
        public Color TextColor = new Color(0, 220, 0);       // verde terminale
        public Color HighlightColor = new Color(255, 220, 0); // giallo selezione
        public Color DimColor = new Color(0, 120, 0);

        private readonly string[] modeLabels =
        {
            "TRAINING",
            "SINGLE MATCH",
            "TOURNAMENT",
            "CHAMPIONSHIP"
        };

        private readonly GameModeType[] modeValues =
        {
            GameModeType.Training,
            GameModeType.SingleMatch,
            GameModeType.Tournament,
            GameModeType.Championship
        };

        private int selectedIndex = 1; // default SINGLE MATCH
        private bool menuActive = true;
        private TextBlock titleText;
        private TextBlock[] optionTexts;
        private TextBlock hintText;
        private TextBlock descText;

        private readonly string[] descriptions =
        {
            "FREE PRACTICE  -  INFINITE LIFE  -  NO SCORE",
            "BEST OF 3 ROUNDS  -  ONE MATCH",
            "CLIMB LEVELS  -  MORE DISCS  -  BECOME CHAMPION",
            "FULL SERIES  -  BEST OF 5 MATCHES  -  HIGHER STAKES"
        };

        public override void Start()
        {
            UI ??= Entity.Get<UIComponent>();
            BuildUI();
            RefreshHighlight();
        }

        private void BuildUI()
        {
            if (UI == null || UI.Page == null)
            {
                // Fallback: se non c'è UI editor, crea pagina runtime
                var root = new Canvas { Name = "ModeSelectRoot" };
                UI = UI ?? Entity.GetOrCreate<UIComponent>();
                UI.Page = new UIPage { RootElement = root };
            }

            var rootPanel = UI.Page.RootElement as Panel
                            ?? UI.Page.RootElement as Canvas;

            // Sfondo pieno
            var bg = new Border
            {
                Name = "Bg",
                BackgroundColor = BgColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var stack = new StackPanel
            {
                Name = "MenuStack",
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            titleText = new TextBlock
            {
                Text = "===  D I S C  ===",
                TextSize = 28,
                TextColor = HighlightColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            };
            stack.Children.Add(titleText);

            var subtitle = new TextBlock
            {
                Text = "SELECT MODE",
                TextSize = 16,
                TextColor = TextColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(subtitle);

            optionTexts = new TextBlock[modeLabels.Length];
            for (int i = 0; i < modeLabels.Length; i++)
            {
                optionTexts[i] = new TextBlock
                {
                    Text = $"  {modeLabels[i]}  ",
                    TextSize = 20,
                    TextColor = DimColor,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                stack.Children.Add(optionTexts[i]);
            }

            descText = new TextBlock
            {
                Text = descriptions[selectedIndex],
                TextSize = 12,
                TextColor = TextColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 8)
            };
            stack.Children.Add(descText);

            hintText = new TextBlock
            {
                Text = "[UP/DOWN] MOVE   [A] START   [B] BACK",
                TextSize = 12,
                TextColor = DimColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            stack.Children.Add(hintText);

            // Se root è Canvas, posiziona stack al centro
            if (UI.Page.RootElement is Canvas canvas)
            {
                canvas.Children.Clear();
                canvas.Children.Add(bg);
                canvas.Children.Add(stack);
                Canvas.SetPinOrigin(stack, new Vector3(0.5f, 0.5f, 0));
                Canvas.SetRelativePosition(stack, new Vector3(0.5f, 0.5f, 0));
            }
            else if (rootPanel != null)
            {
                rootPanel.Children.Clear();
                rootPanel.Children.Add(stack);
            }
        }

        public override void Update()
        {
            if (!menuActive) return;

            var pad = GetPad(0);

            // Navigazione
            if (pad.IsButtonPressed(GamePadButton.PadUp) || pad.IsButtonPressed(GamePadButton.DPadUp)
                || Input.IsKeyPressed(Keys.Up))
            {
                selectedIndex = (selectedIndex - 1 + modeLabels.Length) % modeLabels.Length;
                RefreshHighlight();
            }
            else if (pad.IsButtonPressed(GamePadButton.PadDown) || pad.IsButtonPressed(GamePadButton.DPadDown)
                     || Input.IsKeyPressed(Keys.Down))
            {
                selectedIndex = (selectedIndex + 1) % modeLabels.Length;
                RefreshHighlight();
            }

            // Conferma
            if (pad.IsButtonPressed(GamePadButton.A) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
            {
                ConfirmSelection();
            }
        }

        private void RefreshHighlight()
        {
            for (int i = 0; i < optionTexts.Length; i++)
            {
                bool sel = (i == selectedIndex);
                optionTexts[i].Text = sel ? $"> {modeLabels[i]} <" : $"  {modeLabels[i]}  ";
                optionTexts[i].TextColor = sel ? HighlightColor : DimColor;
            }
            descText.Text = descriptions[selectedIndex];
        }

        private void ConfirmSelection()
        {
            if (GameManager == null) return;

            menuActive = false;
            HideMenu();

            var mode = modeValues[selectedIndex];
            switch (mode)
            {
                case GameModeType.Training:      GameManager.StartTraining(); break;
                case GameModeType.SingleMatch:   GameManager.StartSingleMatch(); break;
                case GameModeType.Tournament:    GameManager.StartTournament(); break;
                case GameModeType.Championship:  GameManager.StartChampionship(); break;
            }
        }

        public void ShowMenu()
        {
            menuActive = true;
            if (UI?.Page?.RootElement != null)
                UI.Page.RootElement.Visibility = Visibility.Visible;
            RefreshHighlight();
        }

        public void HideMenu()
        {
            menuActive = false;
            if (UI?.Page?.RootElement != null)
                UI.Page.RootElement.Visibility = Visibility.Hidden;
        }

        private GamePadState GetPad(int index)
        {
            if (Input.GamePadCount > index)
                return Input.GetGamePadByIndex(index);
            return default;
        }
    }
}
