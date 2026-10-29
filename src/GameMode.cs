namespace DiscFaithful
{
    /// <summary>
    /// Modalità di gioco disponibili.
    /// </summary>
    public enum GameModeType
    {
        /// <summary>Allenamento libero: vita infinita, reset manuale, no score.</summary>
        Training = 0,

        /// <summary>Una sola partita al meglio di N round (default 3).</summary>
        SingleMatch = 1,

        /// <summary>Torneo a eliminazione: avanzamento livelli, più dischi, fine a campione.</summary>
        Tournament = 2,

        /// <summary>Campionato completo: serie di match, classifica, più vite/round.</summary>
        Championship = 3
    }

    /// <summary>
    /// Configurazione per ogni modalità.
    /// </summary>
    public class GameModeConfig
    {
        public GameModeType Mode;

        public int WinsToChampion = 3;
        public int StartingLevel = 1;
        public int MaxLevel = 8;
        public int StartingLife = 5;
        public bool InfiniteLife = false;
        public bool AutoAdvanceLevel = false;
        public bool TrackScore = true;
        public bool AllowRestartMidMatch = true;
        public string DisplayName = "";

        public static GameModeConfig For(GameModeType mode)
        {
            return mode switch
            {
                GameModeType.Training => new GameModeConfig
                {
                    Mode = GameModeType.Training,
                    DisplayName = "TRAINING",
                    WinsToChampion = 999,
                    StartingLevel = 1,
                    MaxLevel = 8,
                    StartingLife = 99,
                    InfiniteLife = true,
                    AutoAdvanceLevel = false,
                    TrackScore = false,
                    AllowRestartMidMatch = true
                },
                GameModeType.SingleMatch => new GameModeConfig
                {
                    Mode = GameModeType.SingleMatch,
                    DisplayName = "SINGLE MATCH",
                    WinsToChampion = 2,      // best of 3
                    StartingLevel = 1,
                    MaxLevel = 1,
                    StartingLife = 5,
                    InfiniteLife = false,
                    AutoAdvanceLevel = false,
                    TrackScore = true,
                    AllowRestartMidMatch = true
                },
                GameModeType.Tournament => new GameModeConfig
                {
                    Mode = GameModeType.Tournament,
                    DisplayName = "TOURNAMENT",
                    WinsToChampion = 3,
                    StartingLevel = 1,
                    MaxLevel = 6,
                    StartingLife = 5,
                    InfiniteLife = false,
                    AutoAdvanceLevel = true,  // dopo ogni match avanza livello
                    TrackScore = true,
                    AllowRestartMidMatch = false
                },
                GameModeType.Championship => new GameModeConfig
                {
                    Mode = GameModeType.Championship,
                    DisplayName = "CHAMPIONSHIP",
                    WinsToChampion = 5,      // best of 9 feel
                    StartingLevel = 1,
                    MaxLevel = 8,
                    StartingLife = 6,
                    InfiniteLife = false,
                    AutoAdvanceLevel = true,
                    TrackScore = true,
                    AllowRestartMidMatch = false
                },
                _ => For(GameModeType.SingleMatch)
            };
        }
    }
}
