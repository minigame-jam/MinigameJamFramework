using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts;
using Game.MinigameFramework.Scripts.Framework;
using Game.MinigameFramework.Scripts.Framework.Minigames;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager instance;

    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }

        transform.parent = null;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        DetermineFewestPlayers();
        PopulateMinigameList();
        PawnBindingManager.onPauseButtonPressed.AddListener(OnPauseButton);
    }

    public SceneField minigameSelectScene;
    public SceneField resultsScene;
    public SceneField mainMenuScene;
    public PauseMenuButtons pauseMenuPrefab;
    
    [Header("Points")]
    public int pointsToWin = 20;
    [SerializeField] private int[] pointsPerRank = new int[4] { 4, 3, 2, 1 };

    [Header("Minigames")] 
    [FormerlySerializedAs("minigamePacks")]public List<MinigamePack> activePacks = new ();
    public List<MinigamePack> allPacks = new();
    public List<MinigameInfo> minigames { get; private set; }
    public MinigameInfo debugMinigame;
    private bool _isMinigameEnded = false;

    // Represents the rank of each player in each minigame that has been played. Duplicate minigames supported.
    [HideInInspector]
    public List<KeyValuePair<MinigameInfo, Ranking>> minigameResults = new();
    private MinigameInfo _currentMinigame = null;


    // Sets expectedPlayers in PlayerManager
    // Called on start and when minigamePacks is updated
    public void DetermineFewestPlayers() {
        int min = PlayerManager.maxPlayers + 1; // could never occur

        // account for if debugMinigame assigned
        if (debugMinigame != null) {
            min = Mathf.Min(min, debugMinigame.minimumPlayers);
        } else {
            // otherwise, set min to the least restrictive value
            foreach(MinigamePack pack in activePacks) {
                foreach(MinigameInfo minigame in pack.minigames) {
                    min = Mathf.Min(min, minigame.minimumPlayers);
                }
            }
        }

        PlayerManager.expectedPlayers = min;
    }
    
    public void PopulateMinigameList() {
        minigames = new List<MinigameInfo>();

        if (debugMinigame != null) {
            minigames.Add(debugMinigame);
            return;
        }

        foreach(MinigamePack pack in activePacks) {
            foreach(MinigameInfo minigame in pack.minigames) {
                if (minigame.minimumPlayers <= PlayerManager.GetNumPlayers()) {
                    minigames.Add(minigame);
                }
            }
        }
    }

    public void RemoveMinigameFromQueue(MinigameInfo minigameInfo) {
        minigames.Remove(minigameInfo);
        if(minigames.Count == 0) {
            PopulateMinigameList();
        }
    }
    
    public void GoToMinigameSelectScene() {
        SceneManager.LoadScene(minigameSelectScene.SceneName);
    }
    
    public void LoadMinigame(MinigameInfo minigame) {
        SceneManager.LoadScene(minigame.scene.SceneName);
        PlayerManager.SetMinigameActionMap();
        _currentMinigame = minigame;
        _isMinigameEnded = false;
    }
    
    public bool PackIsOn(MinigamePack pack) {
        return activePacks.Contains(pack);
    }
    
    public void TogglePack(MinigamePack pack) {
        if (!PackIsOn(pack)) activePacks.Add(pack);
        else activePacks.Remove(pack);
        DetermineFewestPlayers();
    }
    
    
    public void EndMinigame(Ranking ranking) {
        if (_isMinigameEnded) {
            Debug.LogWarning("MinigameManager: EndMinigame has been called multiple times. Redundant request has been ignored.");
            return;
        }
        
        _isMinigameEnded = true;
        AwardPoints(ranking.ToList());
        minigameResults.Add(new KeyValuePair<MinigameInfo, Ranking>(_currentMinigame, ranking));
        _currentMinigame = null;
        PlayerManager.SetMenuActionMap();
        SceneManager.LoadScene(resultsScene.SceneName);
    }

    /// <summary>
    /// Reset the game for new players.
    /// Called when a player wins or a play session is ended early.
    /// </summary>
    public void ResetPoints() {
        foreach(Player player in PlayerManager.players) {
            player.points = 0;
        }
        minigameResults.Clear();
        _currentMinigame = null;
    }

    private void AwardPoints(List<int> ranking) {
        for (int playerIndex = 0; playerIndex < PlayerManager.players.Count; playerIndex++) {
            int playerRank = ranking[playerIndex];
            PlayerManager.players[playerIndex].points += GetPointsForRank(playerRank);
        }
    }
    private int GetPointsForRank(int rank) {
        return pointsPerRank[Math.Clamp(rank - 1, 0, 3)];
    }

    public KeyValuePair<MinigameInfo, Ranking> GetLastMinigameResult() {
        if (minigameResults.Count == 0) {
            Debug.LogWarning("MinigameManager: GetLastMinigameResult called but no minigames have been played yet.");
            return new KeyValuePair<MinigameInfo, Ranking>(null, new Ranking());
        }
        return minigameResults[minigameResults.Count - 1];
    }
    public int GetPointsForLastMinigameRank(int playerIndex) {
        return GetPointsForRank(GetLastMinigameResult().Value[playerIndex]);
    }

    /// <summary>
    /// Collection representing the placement of each player in a minigame.
    /// Index corresponds to player index with range [0-3]. Value corresponds to that player's rank with range [1-4] .
    /// For example, "ranking[3] = 1" sets Player 4 to first place.
    /// </summary>
    public class Ranking {
        private List<int> _playerRanks = new(4) { 0, 0, 0, 0 };

        public int this[int index] {
            get => _playerRanks[index];
            set {
                if(value < 1 || value > 4) {
                    Debug.LogWarning($"Ranking: new value should be in range [1-4]. Value {value} clamped to {Math.Clamp(value, 1, 4)}.");
                }
                _playerRanks[index] = Math.Clamp(value, 1, 4);
            }
        }

        public List<int> ToList() {
            return _playerRanks;
        }
        
        public int GetNextHighestRank() {
            int i = 1;
            for(; i <= 4; i++) {
                if (!_playerRanks.Contains(i)) break;
            }
            return i;
        }
        public int GetNextLowestRank() {
            int i = 4;
            for (; i >= 1; i--) {
                if (!_playerRanks.Contains(i)) break;
            }
            return i;
        }
        
        public void SetRank(int playerIndex, int rank) {
            this[playerIndex] = rank;
        }
        public void SetRanksFromList(int[] ranks) {
            for (int i = 0; i < ranks.Length; i++) {
                SetRank(i, ranks[i]);
            }
        }
        public void SetRanksFromPlayerIndexList(int[] playerIndexList) {
            if(playerIndexList.Length > 4) {
                throw new ArgumentException("Ranking: argument 'playerIndexList' length cannot be greater than 4");
            }
            for(int i = 0; i < 4; i++) {
                if (i >= playerIndexList.Length) break;
                if(playerIndexList[i] < 0) continue; // skip invalid player indices
                SetRank(playerIndexList[i], i + 1);
            }
        }
        public void SetAllPlayersToRank(int rank) {
            for(int i = 0; i<4; i++) {
                SetRank(i, rank);
            }
        }
        
        /// <summary>
        /// Ranks players from a list of scores ordered by player index.
        /// If players achieve the same score they will achieve the same rank.
        /// </summary>
        /// <param name="scores">Index in the list corresponds to player index</param>
        /// <returns></returns>
        public void DetermineRankingFromScores(List<int> scores) {
            _playerRanks = new(4) { 0, 0, 0, 0 };
            
            // Set up player index list
            List<int> playerIndexList = new();
            for(int i = 0; i < scores.Count; i++) playerIndexList.Add(i);
            // Sort player index list to be ordered by player score
            playerIndexList.Sort((a, b) => scores[b].CompareTo(scores[a]));
            // Determine rankings 
            SetRank(playerIndexList[0],1);
            print($"Player {playerIndexList[0]+1} ranked 1");

            for (int i = 1; i < playerIndexList.Count; i++) {
                int prevPlayerRank = this[playerIndexList[i - 1]];
                
                // Handle ties
                if (scores[playerIndexList[i]] == scores[playerIndexList[i - 1]]) {
                    SetRank(playerIndexList[i], prevPlayerRank);
                    print($"Player {playerIndexList[i]+1} ranked {this[playerIndexList[i]]}");
                    continue;
                }
                SetRank(playerIndexList[i], prevPlayerRank+1);
                print($"Player {playerIndexList[i]+1} ranked {this[playerIndexList[i]]}");
            }
        }
    }

    public void GoToMainMenuScene() {
        SceneManager.LoadScene(mainMenuScene.SceneName);
        PlayerManager.SetMenuActionMap();
    }
    private void OnPauseButton() {
        if (FindAnyObjectByType<PauseMenuButtons>() != null) {
            // Already paused
            return;
        }
        Time.timeScale = 0;
        Instantiate(pauseMenuPrefab);
        PlayerManager.SetMenuActionMap();
    }

    public List<Sprite> GetPackageSprites() {
        // if debugMinigame, just return its sprite
        // otherwise return the pack thumbnails for each selected pack
        List<Sprite> sprites = new List<Sprite>();
        if (debugMinigame != null) {
            sprites.Add(debugMinigame.thumbnail);
        } else {
            foreach (MinigamePack pack in activePacks) {
                sprites.Add(pack.icon);
            }
        }
        return sprites;
    }
}
