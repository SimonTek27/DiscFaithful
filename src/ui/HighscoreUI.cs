using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tabella highscore con pagine:
    /// 0 = All, 1 = Single Match, 2 = Tournament
    /// Navigazione: ← → / D-Pad Left-Right, B torna al menu.
    /// </summary>
    public class HighscoreUI : SyncScript
    {
        public HighscoreService Highscores;
        public UIComponent UI;
        public ModeSelectUI ModeSelect;

        public Color BgColor = new Color(12, 14, 20, 245);
        public Color TextPrimary = new Color(245, 247, 250);
        public Color TextSecondary = new Color(160, 170, 190);
        public Color Accent = new Color(88, 140, 255);
        public Color HeaderColor = new Color(120, 130, 150);
        public Color DimColor = new Color(100, 110, 130);

        private static readonly string[] PageTitles =
        {
            "All Modes",
            "Single Match",
            "Tournament"
        };

        /// <summary>Filtro modalità: null = tutte, altrimenti DisplayName esatto.</summary>
        private static readonly string[] PageFilters =
        {
            null,
            "Single Match",
            "Tournament"
        };

        private int pageIndex;
        private bool visible;

        private TextBlock titleText;
        private TextBlock pageIndicator;
        private TextBlock tableText;
        private TextBlock emptyText;

        public override void Start()
        {
            UI ??= Entity.Get<UIComponent>();

            if (Highscores == null && Entity.Scene != null)
            {
                foreach (var e in Entity.Scene.Entities)
                {
                    Highscores = e.Get<HighscoreService>();
                    if (Highscores != null) break;
                }
            }

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

            stack.Children.Add(new TextBlock
            {
                Text = "High Scores",
                TextSize = 32,
                TextColor = TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            pageIndicator = new TextBlock
            {
                Text = PageLabel(),
                TextSize = 15,
                TextColor = Accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(pageIndicator);

            stack.Children.Add(new TextBlock
            {
                Text = "  #    Name            Mode             Score  Lv   Date",
                TextSize = 12,
                TextColor = HeaderColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            stack.Children.Add(new Border
            {
                BackgroundColor = new Color(50, 56, 70),
                Height = 1,
                Width = 560,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            });

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
                Text = "No scores yet for this category.",
                TextSize = 14,
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 24, 0, 8),
                Visibility = Visibility.Collapsed
            };
            stack.Children.Add(emptyText);

            stack.Children.Add(new TextBlock
            {
                Text = "← →  Switch page      B  Back",
                TextSize = 12,
                TextColor = DimColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 28, 0, 0)
            });

            root.Children.Add(stack);
            Canvas.SetPinOrigin(stack, new Vector3(0.5f, 0.5f, 0));
            Canvas.SetRelativePosition(stack, new Vector3(0.5f, 0.5f, 0));

            RefreshTable();
        }

        private string PageLabel()
        {
            int n = PageTitles.Length;
            // es.  ·  All Modes  ·
            return $"‹  {PageTitles[pageIndex]}  ›    {pageIndex + 1}/{n}";
        }

        public void RefreshTable()
        {
            if (tableText == null) return;

            if (pageIndicator != null)
                pageIndicator.Text = PageLabel();

            var list = GetEntriesForCurrentPage();

            if (list.Count == 0)
            {
                tableText.Text = "";
                if (emptyText != null)
                {
                    emptyText.Text = pageIndex == 0
                        ? "No scores yet. Play a match!"
                        : $"No scores yet for {PageTitles[pageIndex]}.";
                    emptyText.Visibility = Visibility.Visible;
                }
                return;
            }

            if (emptyText != null)
                emptyText.Visibility = Visibility.Collapsed;

            var sb = new StringBuilder();
            int rank = 1;
            foreach (var e in list)
            {
                string name = Truncate(e.PlayerName, 14);
                string mode = Truncate(e.Mode, 14);
                sb.AppendLine(
                    $"  {rank,2}   {name,-14}  {mode,-14}  {e.Score,5}  {e.Level,2}   {e.Date}");
                rank++;
            }
            tableText.Text = sb.ToString().TrimEnd();
        }

        private List<HighscoreEntry> GetEntriesForCurrentPage()
        {
            if (Highscores == null)
                return new List<HighscoreEntry>();

            string filter = PageFilters[pageIndex];
            if (filter == null)
                return Highscores.GetTop();

            return Highscores.GetByMode(filter, Highscores.MaxEntries);
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        public override void Update()
        {
            if (!visible) return;

            var pad = Input.GamePadCount > 0 ? Input.GetGamePadByIndex(0) : default;

            // Pagine ← →
            if (pad.IsButtonPressed(GamePadButton.PadLeft) || pad.IsButtonPressed(GamePadButton.DPadLeft)
                || Input.IsKeyPressed(Keys.Left))
            {
                pageIndex = (pageIndex - 1 + PageTitles.Length) % PageTitles.Length;
                RefreshTable();
            }
            else if (pad.IsButtonPressed(GamePadButton.PadRight) || pad.IsButtonPressed(GamePadButton.DPadRight)
                     || Input.IsKeyPressed(Keys.Right))
            {
                pageIndex = (pageIndex + 1) % PageTitles.Length;
                RefreshTable();
            }

            if (pad.IsButtonPressed(GamePadButton.B) || Input.IsKeyPressed(Keys.Escape))
            {
                Hide();
                ModeSelect?.ShowMenu();
            }
        }

        public void Show()
        {
            visible = true;
            pageIndex = 0;
            RefreshTable();
            if (UI?.Page?.RootElement != null)
                UI.Page.RootElement.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            visible = false;
            if (UI?.Page?.RootElement != null)
                UI.Page.RootElement.Visibility = Visibility.Hidden;
        }

        /// <summary>Apre direttamente una pagina (0=All, 1=Single, 2=Tournament).</summary>
        public void ShowPage(int index)
        {
            pageIndex = Math.Clamp(index, 0, PageTitles.Length - 1);
            Show();
        }
    }
}
