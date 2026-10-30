using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Voce classifica.
    /// </summary>
    public class HighscoreEntry
    {
        public string PlayerName { get; set; } = "Player";
        public string Mode { get; set; } = "Single Match";
        public int Score { get; set; }
        public int Level { get; set; }
        public string Date { get; set; } = "";
    }

    /// <summary>
    /// Servizio highscore: carica/salva JSON, top N per modalità o globale.
    /// </summary>
    public class HighscoreService : StartupScript
    {
        public int MaxEntries = 10;
        public string FileName = "highscores.json";

        public List<HighscoreEntry> Entries { get; private set; } = new();

        public event Action OnScoresChanged;

        private string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiscFaithful", FileName);

        public override void Start()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(FilePath))
                {
                    Entries = new List<HighscoreEntry>();
                    return;
                }

                string json = File.ReadAllText(FilePath);
                Entries = JsonSerializer.Deserialize<List<HighscoreEntry>>(json)
                          ?? new List<HighscoreEntry>();
            }
            catch (Exception ex)
            {
                Log.Warning($"[Highscore] Load failed: {ex.Message}");
                Entries = new List<HighscoreEntry>();
            }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Entries, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Log.Warning($"[Highscore] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiunge un punteggio. Restituisce true se entra in top MaxEntries.
        /// Score tipico: round vinti * 100 + livello * 10 (o personalizzato).
        /// </summary>
        public bool TryAdd(string playerName, string mode, int score, int level)
        {
            var entry = new HighscoreEntry
            {
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim(),
                Mode = mode ?? "—",
                Score = score,
                Level = level,
                Date = DateTime.Now.ToString("yyyy-MM-dd")
            };

            Entries.Add(entry);
            Entries = Entries
                .OrderByDescending(e => e.Score)
                .ThenByDescending(e => e.Level)
                .Take(MaxEntries)
                .ToList();

            bool madeIt = Entries.Any(e =>
                e.PlayerName == entry.PlayerName &&
                e.Score == entry.Score &&
                e.Date == entry.Date);

            Save();
            OnScoresChanged?.Invoke();
            return madeIt;
        }

        public List<HighscoreEntry> GetTop(int count = -1)
        {
            int n = count < 0 ? MaxEntries : count;
            return Entries.Take(n).ToList();
        }

        public List<HighscoreEntry> GetByMode(string mode, int count = 10)
        {
            return Entries
                .Where(e => string.Equals(e.Mode, mode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Score)
                .Take(count)
                .ToList();
        }

        public void Clear()
        {
            Entries.Clear();
            Save();
            OnScoresChanged?.Invoke();
        }

        /// <summary>
        /// Calcolo score standard da GameManager a fine modalità.
        /// </summary>
        public static int ComputeScore(GameManager gm, PlayerController winner)
        {
            if (gm == null) return 0;

            int wins = (winner == gm.Player1) ? gm.ScoreP1 : gm.ScoreP2;
            int levelBonus = gm.CurrentLevel * 25;
            int seriesBonus = 0;

            if (gm.Config?.Mode == GameModeType.Championship)
            {
                int series = (winner == gm.Player1) ? gm.ChampionshipWinsP1 : gm.ChampionshipWinsP2;
                seriesBonus = series * 100;
            }

            return wins * 100 + levelBonus + seriesBonus;
        }
    }
}
