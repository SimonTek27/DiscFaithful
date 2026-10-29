using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace DiscFaithful
{
    /// <summary>
    /// Orchestratore match: livelli, multi-disco, score, reset round, vittoria.
    /// </summary>
    public class GameManager : SyncScript
    {
        public PlayerController Player1;
        public PlayerController Player2;
        public Platform Platform1;
        public Platform Platform2;
        public Prefab DiscPrefab;
        public AudioManager Audio;

        public List<Disc> ActiveDiscs = new();

        public int CurrentLevel = 1;
        public int MaxDiscs = 1;
        public int WinsToChampion = 3;

        public int ScoreP1 { get; private set; }
        public int ScoreP2 { get; private set; }

        public bool MatchInProgress { get; private set; }
        public bool RoundOver { get; private set; }

        public event Action<PlayerController, string> OnMatchEnded;
        public event Action OnRoundReset;

        public override void Start()
        {
            ApplyLevelSettings();
            SubscribePlayers();
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
            RoundOver = false;
            MatchInProgress = true;

            Player1?.ResetForRound();
            Player2?.ResetForRound();

            Platform1?.InitializeHits();
            Platform2?.InitializeHits();

            ClearAllDiscs();
            SpawnInitialDiscs();

            Audio?.PlayGong();
            OnRoundReset?.Invoke();
            Log.Info($"[GameManager] Round start – Level {CurrentLevel}, MaxDiscs {MaxDiscs}");
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

            disc.Catch(owner.Entity);
            owner.ReceiveDisc(disc);

            ActiveDiscs.Add(disc);
            return disc;
        }

        private void ClearAllDiscs()
        {
            foreach (var d in ActiveDiscs.ToList())
            {
                if (d?.Entity != null)
                    d.Entity.Scene = null;
            }
            ActiveDiscs.Clear();

            if (Player1 != null) Player1.CurrentDisc = null;
            if (Player2 != null) Player2.CurrentDisc = null;
        }

        private void OnPlayerDeath(PlayerController p)
        {
            if (RoundOver) return;
            var winner = (p == Player1) ? Player2 : Player1;
            EndRound(winner, "Life depleted");
        }

        private void OnPlayerFell(PlayerController p)
        {
            if (RoundOver) return;
            var winner = (p == Player1) ? Player2 : Player1;
            EndRound(winner, "Opponent fell");
        }

        private void EndRound(PlayerController winner, string reason)
        {
            RoundOver = true;
            MatchInProgress = false;

            if (winner == Player1) ScoreP1++;
            else if (winner == Player2) ScoreP2++;

            winner?.PlayVictory();
            Audio?.PlayVictory();

            Log.Info($"[GameManager] Round over – Winner: {winner?.Entity.Name} ({reason})  Score {ScoreP1}-{ScoreP2}");
            OnMatchEnded?.Invoke(winner, reason);
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

            player.TakeDamage(1);
            Audio?.PlayHitPlayer();
        }

        private void OnDiscHitWall(Disc disc, Tile tile)
        {
            Audio?.PlayTileDestroy();
        }

        public void AdvanceLevel()
        {
            CurrentLevel++;
            ApplyLevelSettings();
            StartRound();
        }

        public void RestartMatch()
        {
            ScoreP1 = 0;
            ScoreP2 = 0;
            CurrentLevel = 1;
            ApplyLevelSettings();
            StartRound();
        }

        public void NextRound()
        {
            if (ScoreP1 >= WinsToChampion || ScoreP2 >= WinsToChampion)
            {
                Log.Info("[GameManager] Match finished – Champion decided");
                return;
            }
            StartRound();
        }
    }
}
