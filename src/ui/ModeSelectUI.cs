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
    /// Menu selezione modalità – stile contemporaneo.
    /// Layout pulito, tipografia chiara, focus states moderni.
    /// Controlli: D-Pad / frecce, A conferma, B indietro.
    /// </summary>
    public class ModeSelectUI : SyncScript
    {
        public GameManager GameManager;
        public UIComponent UI;

        // Palette contemporanea
        public Color BgColor = new Color(12, 14, 20, 240);
        public Color CardColor = new Color(24, 28, 38, 255);
        public Color TextPrimary = new Color(245, 247, 250);
        public Color TextSecondary = new Color(160, 170, 190);
        public Color Accent = new Color(88, 140, 255);
        public Color AccentSoft = new Color(88, 140, 255, 40);

        private readonly string[] modeLabels =
        {
            "Training",
            "Single Match",
            "Tournament",
            "Championship"
        };

        private readonly GameModeType[] modeValues =
        {
            GameModeType.Training,
            GameModeType.SingleMatch,
            GameModeType.Tournament,
            GameModeType.Championship
        };

        private readonly string[] descriptions =
        {
            "Practice freely with infinite life. No score tracking.",
            "One match, best of 3 rounds.",
            "Climb levels, more discs each stage. Become the champion.",
            "Full series — best of 5 matches, higher stakes."
        };

        private int selectedIndex = 1;
        private bool menuActive = true;

        private TextBlock titleText;
        private TextBlock subtitleText;
        private TextBlock[] optionTexts;
        private TextBlock descText;
        private TextBlock hintText;
        private Border[] optionCards;

        public override void Start()
        {
            UI ??= Entity.Get<UIComponent>();
            BuildUI();
            RefreshHighlight();
        }

        private void BuildUI()
        {
            UI ??= Entity.GetOrCreate<UIComponent>();

            var root = new Canvas { Name = "ModeSelectRoot" };
            UI.Page = new UIPage { RootElement = root };

            // Backdrop
            var bg = new Border
            {
                Name = "Backdrop",
                BackgroundColor = BgColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            root.Children.Add(bg);

            var stack = new StackPanel
            {
                Name = "MenuStack",
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(32)
            };

            titleText = new TextBlock
            {
                Text = "DISC",
                TextSize = 42,
                TextColor = TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(titleText);

            subtitleText = new TextBlock
            {
                Text = "Select a mode",
                TextSize = 16,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 28)
            };
            stack.Children.Add(subtitleText);

            optionTexts = new TextBlock[modeLabels.Length];
            optionCards = new Border[modeLabels.Length];

            for (int i = 0; i < modeLabels.Length; i++)
            {
                var card = new Border
                {
                    Name = $"Card_{i}",
                    BackgroundColor = CardColor,
                    Padding = new Thickness(20, 14, 20, 14),
                    Margin = new Thickness(0, 6, 0, 6),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Width = 360
                };

                var label = new TextBlock
                {
                    Text = modeLabels[i],
                    TextSize = 18,
                    TextColor = TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                card.Content = label;
                optionCards[i] = card;
                optionTexts[i] = label;
                stack.Children.Add(card);
            }

            descText = new TextBlock
            {
                Text = descriptions[selectedIndex],
                TextSize = 13,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 24, 0, 8),
                Width = 360,
                TextAlignment = TextAlignment.Center
            };
            stack.Children.Add(descText);

            hintText = new TextBlock
            {
                Text = "↑↓ Navigate    A Confirm",
                TextSize = 12,
                TextColor = new Color(100, 110, 130),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };
            stack.Children.Add(hintText);

            root.Children.Add(stack);
            Canvas.SetPinOrigin(stack, new Vector3(0.5f, 0.5f, 0));
            Canvas.SetRelativePosition(stack, new Vector3(0.5f, 0.5f, 0));
        }

        public override void Update()
        {
            if (!menuActive) return;

            var pad = GetPad(0);

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
                optionTexts[i].TextColor = sel ? TextPrimary : TextSecondary;
                optionCards[i].BackgroundColor = sel ? AccentSoft : CardColor;
                // Accent bar feel: slightly brighter text + soft blue tint on card
            }
            descText.Text = descriptions[selectedIndex];
        }

        private void ConfirmSelection()
        {
            if (GameManager == null) return;

            menuActive = false;
            HideMenu();

            switch (modeValues[selectedIndex])
            {
                case GameModeType.Training:     GameManager.StartTraining(); break;
                case GameModeType.SingleMatch:  GameManager.StartSingleMatch(); break;
                case GameModeType.Tournament:   GameManager.StartTournament(); break;
                case GameModeType.Championship: GameManager.StartChampionship(); break;
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
