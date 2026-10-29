using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Orchestratore match + modalità:
    /// TRAINING | SINGLE MATCH | TOURNAMENT | CHAMPIONSHIP
    /// </summary>
    public class GameManager : SyncScript
    {
        public PlayerController Player1;
        public PlayerController Player2;
        public Platform Platform1;
        public Platform Platform2;
        public Prefab DiscPrefab;
        public AudioManager Audio;

        /// <summary>Avversario CPU (opzionale: se null, P2 è umano).</summary>
        public AiOpponent OpponentAi;

        /// <summary>Se false, la partita parte solo quando il menu seleziona una modalità.</summary>
        public bool AutoStart = true;

        /// <summary>Secondi tra fine round e nuovo round (più velocemente nei tornei).</summary>
        public float RoundRestartDelay = 3f;

        public List<Disc> ActiveDiscs = new();

        public GameModeType SelectedMode = GameModeType.SingleMatch;
        public GameModeConfig Config { get; private set; }

        public int CurrentLevel = 1;
        public int MaxDiscs = 1;

        public int ScoreP1 { get; private set; }
        public int ScoreP2 { get; private set; }

        public int ChampionshipWinsP1 { get; private set; }
        public int ChampionshipWinsP2 { get; private set; }

        public bool MatchInProgress { get; private set; }
        public bool RoundOver { get; private set; }
        public bool ModeFinished { get; private set; }

        private bool restartScheduled;

        public event Action<PlayerController, string> OnMatchEnded;
        public event Action OnRoundReset;
        public event Action<GameModeType, PlayerController> OnModeCompleted;
        public event Action<GameModeConfig> OnModeChanged;

        public override void Start()
        {
            SetMode(SelectedMode);
            SubscribePlayers();
            if (AutoStart)
                StartRound();
        }

        public void SetMode(GameModeType mode)
        {
            SelectedMode = mode;
            Config = GameModeConfig.For(mode);

            CurrentLevel = Config.StartingLevel;
            ScoreP1 = 0;
            ScoreP2 = 0;
            ChampionshipWinsP1 = 0;
            ChampionshipWinsP2 = 0;
            ModeFinished = false;

            if (Player1 != null) Player1.MaxLife = Config.StartingLife;
            if (Player2 != null) Player2.MaxLife = Config.StartingLife;

            ApplyLevelSettings();
            OnModeChanged?.Invoke(Config);

            Log.Info($"[GameManager] Mode → {Config.DisplayName}");
        }

        public void StartTraining() => BeginMode(GameModeType.Training);
        public void StartSingleMatch() => BeginMode(GameModeType.SingleMatch);
        public void StartTournament() => BeginMode(GameModeType.Tournament);
        public void StartChampionship() => BeginMode(GameModeType.Championship);

        private void BeginMode(GameModeType mode)
        {
            SetMode(mode);
            StartRound();
        }

        private void SubscribePlayers()
        {
            if (Player1 != null)
            {
                Player1.OnDeath += OnPlayerDeath;
                Player1.OnFell += OnPlayerFell;
            }
            if (Player2 != null)
            {
                Player2.OnDeath += OnPlayerDeath;
                Player2.OnFell += OnPlayerFell;
            }
        }

        private void ApplyLevelSettings()
        {
            int hits = GetHitsForLevel(CurrentLevel);
            MaxDiscs = GetMaxDiscsForLevel(CurrentLevel);

            if (Platform1 != null)
            {
                Platform1.StartingHits = hits;
                Platform1.InitializeHits();
            }
            if (Platform2 != null)
            {
                Platform2.StartingHits = hits;
                Platform2.InitializeHits();
            }

            // Difficoltà CPU scalata col livello (come nell'originale)
            OpponentAi?.SetDifficultyFromLevel(CurrentLevel);
        }

        private int GetHitsForLevel(int level) => Math.Clamp(2 + (level - 1), 2, 6);

        private int GetMaxDiscsForLevel(int level)
        {
            if (level <= 1) return 1;
            if (level <= 3) return 2;
            if (level <= 5) return 3;
            return 4;
        }

        public void StartRound()
        {
            if (ModeFinished) return;

            restartScheduled = false;
            RoundOver = false;
            MatchInProgress = true;

            if (Player1 != null)
            {
                Player1.MaxLife = Config.InfiniteLife ? 99 : Config.StartingLife;
                Player1.ResetForRound();
            }
            if (Player2 != null)
            {
                Player2.MaxLife = Config.InfiniteLife ? 99 : Config.StartingLife;
                Player2.ResetForRound();
            }

            Platform1?.InitializeHits();
            Platform2?.InitializeHits();

            ClearAllDiscs();
            SpawnInitialDiscs();

            Audio?.PlayGong();
            OnRoundReset?.Invoke();
            Log.Info($"[GameManager] [{Config.DisplayName}] Round – Lv{CurrentLevel} Discs:{MaxDiscs} Score {ScoreP1}-{ScoreP2}");
        }

        private void SpawnInitialDiscs()
        {
            if (DiscPrefab == null) return;

            int toSpawn = Math.Min(MaxDiscs, 4);
            for (int i = 0; i < toSpawn; i++)
            {
                bool toP1 = (i % 2 == 0);
                SpawnDisc(toP1 ? Player1 : Player2);
            }
        }

        public Disc SpawnDisc(PlayerController owner)
        {
            if (DiscPrefab == null || owner == null) return null;
            if (ActiveDiscs.Count >= MaxDiscs) return null;

            var instances = DiscPrefab.Instantiate();
            if (instances == null || instances.Count == 0) return null;

            Entity discEntity = instances[0];
            SceneSystem.SceneInstance.RootScene.Entities.Add(discEntity);

            var disc = discEntity.Get<Disc>() ?? discEntity.GetOrCreate<Disc>();

            disc.OnHitPlayer += OnDiscHitPlayer;
            disc.OnHitWallTile += OnDiscHitWall;
            disc.OnOutOfBounds += OnDiscOutOfBounds;

            disc.Catch(owner.Entity);
            owner.ReceiveDisc(disc);

            ActiveDiscs.Add(disc);
            return disc;
        }

        /// <summary>
        /// Disco perso: caduto sotto l'arena o in volo troppo a lungo.
        /// Viene restituito al suo ultimo proprietario (o a un giocatore vivo).
        /// </summary>
        private void OnDiscOutOfBounds(Disc disc)
        {
            if (disc == null || RoundOver) return;

            var receiver = new[] { disc.LastOwner?.Get<PlayerController>(), Player1, Player2 }
                .Where(p => p != null && !p.IsDead && p.CurrentDisc == null)
                .FirstOrDefault();

            if (receiver == null)
            {
                // Nessuno lo può riprendere (nessun giocatore vivo o tutti con un disco): lo rimuoviamo
                disc.OnOutOfBounds -= OnDiscOutOfBounds;
                disc.OnHitPlayer -= OnDiscHitPlayer;
                disc.OnHitWallTile -= OnDiscHitWall;
                ActiveDiscs.Remove(disc);
                if (disc.Entity != null)
                    disc.Entity.Scene = null;
                return;
            }

            Audio?.PlayImpact();
            disc.Catch(receiver.Entity);
            receiver.ReceiveDisc(disc);
        }

        private void ClearAllDiscs()
        {
            foreach (var d in ActiveDiscs.ToList())
            {
                if (d == null) continue;
                d.OnOutOfBounds -= OnDiscOutOfBounds;
                d.OnHitPlayer -= OnDiscHitPlayer;
                d.OnHitWallTile -= OnDiscHitWall;
                if (d.Entity != null)
                    d.Entity.Scene = null;
            }
            ActiveDiscs.Clear();

            if (Player1 != null) Player1.CurrentDisc = null;
            if (Player2 != null) Player2.CurrentDisc = null;
        }

        private void OnPlayerDeath(PlayerController p)
        {
            if (RoundOver || ModeFinished) return;

            if (Config.Mode == GameModeType.Training)
            {
                p.ResetForRound();
                Audio?.PlayDeath();
                return;
            }

            var winner = (p == Player1) ? Player2 : Player1;
            EndRound(winner, "Life depleted");
        }

        private void OnPlayerFell(PlayerController p)
        {
            if (RoundOver || ModeFinished) return;

            if (Config.Mode == GameModeType.Training)
            {
                p.ResetForRound();
                Platform1?.InitializeHits();
                Platform2?.InitializeHits();
                return;
            }

            var winner = (p == Player1) ? Player2 : Player1;
            EndRound(winner, "Opponent fell");
        }

        private void EndRound(PlayerController winner, string reason)
        {
            RoundOver = true;
            MatchInProgress = false;

            if (Config.TrackScore)
            {
                if (winner == Player1) ScoreP1++;
                else if (winner == Player2) ScoreP2++;
            }

            winner?.PlayVictory();
            Audio?.PlayVictory();

            Log.Info($"[GameManager] Round over – {winner?.Entity.Name} ({reason})  {ScoreP1}-{ScoreP2}");
            OnMatchEnded?.Invoke(winner, reason);

            CheckModeProgress(winner);

            // Round successivo automatico (il mode completed ferma il ciclo)
            if (!ModeFinished)
                ScheduleNextRound();
        }

        /// <summary>
        /// Auto-restart: dopo RoundRestartDelay il round riparte da solo,
        /// a meno che nel frattempo non sia iniziato manualmente (StartRound
        /// azzera restartScheduled, annullando il timer pendente).
        /// </summary>
        private async void ScheduleNextRound()
        {
            if (restartScheduled || ModeFinished) return;
            restartScheduled = true;

            await Task.Delay((int)(RoundRestartDelay * 1000));

            if (!restartScheduled || ModeFinished) return;
            restartScheduled = false;

            if (RoundOver)
                StartRound();
        }

        private void CheckModeProgress(PlayerController lastWinner)
        {
            int needed = Config.WinsToChampion;

            if (Config.Mode == GameModeType.Training)
                return;

            if (ScoreP1 >= needed || ScoreP2 >= needed)
            {
                var champ = ScoreP1 >= needed ? Player1 : Player2;

                if (Config.Mode == GameModeType.Championship)
                {
                    if (champ == Player1) ChampionshipWinsP1++;
                    else ChampionshipWinsP2++;

                    Log.Info($"[GameManager] Championship match won by {champ?.Entity.Name}  Series {ChampionshipWinsP1}-{ChampionshipWinsP2}");

                    if (ChampionshipWinsP1 >= 3 || ChampionshipWinsP2 >= 3)
                    {
                        FinishMode(champ);
                        return;
                    }

                    ScoreP1 = 0;
                    ScoreP2 = 0;
                    if (Config.AutoAdvanceLevel && CurrentLevel < Config.MaxLevel)
                        CurrentLevel++;
                    ApplyLevelSettings();
                    return;
                }

                if (Config.Mode == GameModeType.Tournament && Config.AutoAdvanceLevel)
                {
                    if (CurrentLevel < Config.MaxLevel)
                    {
                        CurrentLevel++;
                        ScoreP1 = 0;
                        ScoreP2 = 0;
                        ApplyLevelSettings();
                        Log.Info($"[GameManager] Tournament advance → Level {CurrentLevel}");
                        return;
                    }
                }

                FinishMode(champ);
            }
        }

        private void FinishMode(PlayerController champion)
        {
            ModeFinished = true;
            MatchInProgress = false;
            Log.Info($"[GameManager] {Config.DisplayName} COMPLETE – Champion: {champion?.Entity.Name}");
            OnModeCompleted?.Invoke(Config.Mode, champion);
        }

        public void OnDiscHitPlayer(Disc disc, Entity hitEntity)
        {
            var player = hitEntity.Get<PlayerController>();
            if (player == null || player.IsDead) return;

            if (player.IsBlocking)
            {
                Audio?.PlayBlock();
                return;
            }

            if (Config.InfiniteLife)
            {
                player.PlayHitFeedback();
                Audio?.PlayHitPlayer();
                return;
            }

            player.TakeDamage(1);
            Audio?.PlayHitPlayer();
        }

        private void OnDiscHitWall(Disc disc, Tile tile)
        {
            Audio?.PlayTileDestroy();
            Audio?.PlayImpact();
        }

        public void NextRound()
        {
            if (ModeFinished) return;
            if (Config.Mode == GameModeType.Training || Config.AllowRestartMidMatch || RoundOver)
                StartRound();
        }

        public void RestartMatch()
        {
            if (!Config.AllowRestartMidMatch && MatchInProgress && !RoundOver)
                return;

            restartScheduled = false;
            ScoreP1 = 0;
            ScoreP2 = 0;
            ModeFinished = false;
            CurrentLevel = Config.StartingLevel;
            ApplyLevelSettings();
            StartRound();
        }

        public void AdvanceLevel()
        {
            if (CurrentLevel < Config.MaxLevel)
            {
                CurrentLevel++;
                ApplyLevelSettings();
            }
            StartRound();
        }
    }
}
