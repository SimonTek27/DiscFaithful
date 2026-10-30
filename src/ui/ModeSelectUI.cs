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
    /// Training / Single Match / Tournament / Championship / High Scores
    /// </summary>
    public class ModeSelectUI : SyncScript
    {
        public GameManager GameManager;
        public HighscoreUI HighscorePanel;
        public UIComponent UI;

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
            "Championship",
            "High Scores"
        };

        private readonly string[] descriptions =
        {
            "Practice freely with infinite life. No score tracking.",
            "One match, best of 3 rounds.",
            "Climb levels, more discs each stage. Become the champion.",
            "Full series — best of 5 matches, higher stakes.",
            "View the top scores across all modes."
        };

        private int selectedIndex = 1;
        private bool menuActive = true;

        private TextBlock[] optionTexts;
        private TextBlock descText;
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

            stack.Children.Add(new TextBlock
            {
                Text = "DISC",
                TextSize = 42,
                TextColor = TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Select a mode",
                TextSize = 16,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 28)
            });

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

            stack.Children.Add(new TextBlock
            {
                Text = "↑↓ Navigate    A Confirm",
                TextSize = 12,
                TextColor = new Color(100, 110, 130),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            });

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
            }
            descText.Text = descriptions[selectedIndex];
        }

        private void ConfirmSelection()
        {
            // Ultima voce = High Scores
            if (selectedIndex == modeLabels.Length - 1)
            {
                menuActive = false;
                HideMenu();
                HighscorePanel?.Show();
                return;
            }

            if (GameManager == null) return;

            menuActive = false;
            HideMenu();

            switch (selectedIndex)
            {
                case 0: GameManager.StartTraining(); break;
                case 1: GameManager.StartSingleMatch(); break;
                case 2: GameManager.StartTournament(); break;
                case 3: GameManager.StartChampionship(); break;
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
