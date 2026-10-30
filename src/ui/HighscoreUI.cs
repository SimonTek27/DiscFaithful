using System.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace DiscFaithful.UI
{
    /// <summary>
    /// Tabella highscore – stile contemporaneo.
    /// Mostra rank, nome, modalità, score, livello, data.
    /// </summary>
    public class HighscoreUI : SyncScript
    {
        public HighscoreService Highscores;
        public UIComponent UI;
        public ModeSelectUI ModeSelect; // opzionale: B torna al menu

        public Color BgColor = new Color(12, 14, 20, 245);
        public Color CardColor = new Color(24, 28, 38);
        public Color TextPrimary = new Color(245, 247, 250);
        public Color TextSecondary = new Color(160, 170, 190);
        public Color Accent = new Color(88, 140, 255);
        public Color HeaderColor = new Color(120, 130, 150);

        private bool visible;
        private TextBlock titleText;
        private TextBlock tableText;
        private TextBlock emptyText;
        private TextBlock hintText;

        public override void Start()
        {
            UI ??= Entity.Get<UIComponent>();
            Highscores ??= Entity.Get<HighscoreService>()
                ?? Entity.Scene?.Entities
                    .Select(e => e.Get<HighscoreService>())
                    .FirstOrDefault(s => s != null);

            BuildUI();
            Hide();

            if (Highscores != null)
                Highscores.OnScoresChanged += RefreshTable;
        }

        public override void Cancel()
        {
            if (Highscores != null)
                Highscores.OnScoresChanged -= RefreshTable;
        }

        private void BuildUI()
        {
            UI ??= Entity.GetOrCreate<UIComponent>();

            var root = new Canvas { Name = "HighscoreRoot" };
            UI.Page = new UIPage { RootElement = root };

            var bg = new Border
            {
                BackgroundColor = BgColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            root.Children.Add(bg);

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(32)
            };

            titleText = new TextBlock
            {
                Text = "High Scores",
                TextSize = 32,
                TextColor = TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(titleText);

            var header = new TextBlock
            {
                Text = FormatHeader(),
                TextSize = 12,
                TextColor = HeaderColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 4)
            };
            stack.Children.Add(header);

            // Divider feel
            var line = new Border
            {
                BackgroundColor = new Color(50, 56, 70),
                Height = 1,
                Width = 520,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(line);

            tableText = new TextBlock
            {
                Text = "",
                TextSize = 14,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(tableText);

            emptyText = new TextBlock
            {
                Text = "No scores yet. Play a match!",
                TextSize = 14,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 24, 0, 8),
                Visibility = Visibility.Collapsed
            };
            stack.Children.Add(emptyText);

            hintText = new TextBlock
            {
                Text = "B  Back to menu",
                TextSize = 12,
                TextColor = new Color(100, 110, 130),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 28, 0, 0)
            };
            stack.Children.Add(hintText);

            root.Children.Add(stack);
            Canvas.SetPinOrigin(stack, new Vector3(0.5f, 0.5f, 0));
            Canvas.SetRelativePosition(stack, new Vector3(0.5f, 0.5f, 0));

            RefreshTable();
        }

        private static string FormatHeader()
        {
            return $"{