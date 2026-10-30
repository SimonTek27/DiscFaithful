namespace DiscFaithful
{
    /// <summary>
    /// Modalità di gioco disponibili.
    /// </summary>
    public enum GameModeType
    {
        Training = 0,
        SingleMatch = 1,
        Tournament = 2,
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
                    DisplayName = "Training",
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
                    DisplayName = "Single Match",
                    WinsToChampion = 2,
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
                    DisplayName = "Tournament",
                    WinsToChampion = 3,
                    StartingLevel = 1,
                    MaxLevel = 6,
                    StartingLife = 5,
                    InfiniteLife = false,
                    AutoAdvanceLevel = true,
                    TrackScore = true,
                    AllowRestartMidMatch = false
                },
                GameModeType.Championship => new GameModeConfig
                {
                    Mode = GameModeType.Championship,
                    DisplayName = "Championship",
                    WinsToChampion = 5,
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
