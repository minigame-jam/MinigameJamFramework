using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SugarRush {
    /// <summary>
    /// The end-of-round results. Hidden when the game starts; RoundTimer calls Show when time runs out, and it plays out:
    /// <list type="number">
    /// <item>Every player's score counts up from 0 at Count Speed, all at the same time, each one stopping at what that
    /// player actually got. Portraits play idle meanwhile, and the two team totals follow each score as it changes.</item>
    /// <item>Bonus Delay later, the team with the higher total wins, and each of its players gets Win Bonus added (counted up the same way).</item>
    /// <item>Reveal Delay after the bonus, all at once: winners' portraits play win and losers' play lose, each player's
    /// Ranking appears (#1 to #4 by score after the bonus, colored by place; equal scores share a place), the winning
    /// team's background (HanselWin or WitchWin) appears, and the winning team's confetti fires.</item>
    /// </list>
    /// If the team totals tie there's no bonus and no background: everyone plays win and both confetti fire.
    /// Sounds: a tick while any score counts up, pitched higher the higher the score, and every Reveal Sound together as the winner is shown and the confetti fires.
    /// Players nobody is playing (no pawn bound) have their panel hidden and are left out of the rankings.
    /// Put this on the Results object. It hides the Results object when the game starts, whatever it was left as in the editor.
    /// </summary>
    public class ResultsScreen : MonoBehaviour {
        public enum Team { None, Hansels, Witches }

        [System.Serializable]
        private struct PlayerSlot {
            [Tooltip("The player's whole results panel. Hidden if nobody is playing this player.")]
            public GameObject container;
            [Tooltip("Portrait Animator with idle, win and lose states.")]
            public Animator portrait;
            [Tooltip("Text the player's score counts up in.")]
            public TMP_Text scoreText;
            [Tooltip("Text showing the player's place (#1 to #4). Hidden until the winners are decided.")]
            public TMP_Text rankingText;
        }

        private static readonly int IdleState = Animator.StringToHash("idle");
        private static readonly int WinState = Animator.StringToHash("win");
        private static readonly int LoseState = Animator.StringToHash("lose");

        [Header("Players")]
        [Tooltip("One entry per player, in order: element 0 is Player 1. Each player's team comes from the pawn bound to them (Hansel or Witch).")]
        [SerializeField] private PlayerSlot[] players = new PlayerSlot[4];
        [Tooltip("Where the scores come from. Found automatically if left empty.")]
        [SerializeField] private SugarRushPawnManager pawnManager;

        [Header("Team Totals")]
        [Tooltip("Text showing the Hansels' scores added up.")]
        [SerializeField] private TMP_Text hanselTotalText;
        [Tooltip("Text showing the Witches' scores added up.")]
        [SerializeField] private TMP_Text witchTotalText;

        [Header("Counting")]
        [Tooltip("Seconds the results are up, all at 0, before the scores start counting.")]
        [SerializeField, Min(0f)] private float countStartDelay = 0.5f;
        [Tooltip("How fast the scores count up, in points per second. Every score counts at this speed, so lower scores finish first.")]
        [SerializeField, Min(1f)] private float countSpeed = 30f;

        [Header("Winners")]
        [Tooltip("Score added to each player on the winning team, before the rankings are worked out.")]
        [SerializeField] private int winBonus = 20;
        [Tooltip("Seconds the counted scores stay up before the winning team's bonus is added (or, for a tie, before the reveal).")]
        [SerializeField, Min(0f), FormerlySerializedAs("stepDelay")] private float bonusDelay = 0.5f;
        [Tooltip("Seconds after the bonus finishes counting before the reveal: win/lose animations, rankings, background and confetti, all at once.")]
        [SerializeField, Min(0f)] private float revealDelay = 0.1f;
        [Tooltip("Ranking text color for #1, #2, #3 and #4.")]
        [SerializeField] private Color[] rankColors = {
            new(1f, 0.8252331f, 0f),            // #1 yellow
            new(0.8113208f, 0.8113208f, 0.8113208f), // #2 silver
            new(1f, 0.5615011f, 0f),            // #3 lighter brown
            new(0.6132076f, 0.3841419f, 0f),    // #4 darker brown
        };
        [Tooltip("Background shown when the Hansels win.")]
        [SerializeField] private GameObject hanselWinBackground;
        [Tooltip("Background shown when the Witches win.")]
        [SerializeField] private GameObject witchWinBackground;
        [Tooltip("Confetti fired when the Hansels win.")]
        [SerializeField] private ParticleSystem hanselConfetti;
        [Tooltip("Confetti fired when the Witches win.")]
        [SerializeField] private ParticleSystem witchConfetti;

        [Header("Sounds")]
        [Tooltip("Ticks while the scores count up, at most once every Count Sound Interval. Its pitch follows the score being counted: the higher the score, the higher the pitch.")]
        [SerializeField] private AudioClip countSound;
        [Tooltip("Fewest seconds between count ticks, so several scores counting at once don't pile up into a buzz.")]
        [SerializeField, Min(0f)] private float countSoundInterval = 0.05f;
        [Tooltip("Count tick pitch at a score of 0 (1 = normal).")]
        [SerializeField, Range(0.1f, 3f)] private float countPitchAtZero = 0.8f;
        [Tooltip("Count tick pitch at Count Top Score. It rises steadily between the two, and stays here for higher scores.")]
        [SerializeField, Range(0.1f, 3f)] private float countPitchAtTop = 1.8f;
        [Tooltip("Score where the count tick reaches Count Pitch At Top.")]
        [SerializeField, Min(1)] private int countTopScore = 100;
        [Tooltip("All played together at the reveal, as the winner is shown and the confetti fires (e.g. the confetti pops and a cheer).")]
        [SerializeField, FormerlySerializedAs("confettiSounds")] private AudioClip[] revealSounds = { };
        [Tooltip("Plays the reveal sounds. Auto-found on this object, or added, if left empty. The count ticks get a source of their own, since their pitch changes.")]
        [SerializeField] private AudioSource audioSource;
        private AudioSource _countSource;
        private float _nextCountSoundTime;

        [Tooltip("Called once the whole results sequence has played, after the confetti fires.")]
        public UnityEvent onResultsFinished = new();

        private bool _isShowing;
        private bool _isFinished;
        private Team _winner = Team.None;
        private bool[] _isPresent;
        private Team[] _teams;
        private float[] _countedScores; // what the count has reached, as it goes
        private int[] _shownScores;     // what the score text shows now
        private int[] _finalScores;     // what the count is heading for: the player's score, plus the bonus once it's given
        private int[] _ranks;

        /// True from Show until the scene ends.
        public bool IsShowing => _isShowing;
        /// True once the whole sequence has played (the confetti has fired).
        public bool IsFinished => _isFinished;
        /// The winning team, once the counting is done. None for a tie, or before then.
        public Team Winner => _winner;

        /// <returns>The player's score including any win bonus, once the bonus has been given.</returns>
        public int GetFinalScore(int playerIndex) => IsValidIndex(playerIndex) ? _finalScores[playerIndex] : 0;
        /// <returns>The player's place (1 to 4) once the rankings are shown, or 0 before then and for players nobody is playing.</returns>
        public int GetRank(int playerIndex) => IsValidIndex(playerIndex) ? _ranks[playerIndex] : 0;

        private void Awake() {
            if (pawnManager == null) pawnManager = FindAnyObjectByType<SugarRushPawnManager>();

            int count = players.Length;
            _isPresent = new bool[count];
            _teams = new Team[count];
            _countedScores = new float[count];
            _shownScores = new int[count];
            _finalScores = new int[count];
            _ranks = new int[count];

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = AddAudioSource();
            _countSource = AddAudioSource();

            // Confetti is set to Play On Awake, which would fire it the moment the results show
            StopConfetti(hanselConfetti);
            StopConfetti(witchConfetti);
            HideWinnerReveal();

            if (!_isShowing) gameObject.SetActive(false); // Show may be what woke this up, if Results was left hidden in the editor
        }

        /// <summary>Shows the results and plays the whole sequence. Does nothing if they're already showing.</summary>
        public void Show() {
            if (_isShowing) return;
            _isShowing = true;
            gameObject.SetActive(true);
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence() {
            SetUpPlayers();
            if (countStartDelay > 0f) yield return new WaitForSeconds(countStartDelay);

            // Count everyone up to what they actually got
            for (int i = 0; i < players.Length; i++) {
                _finalScores[i] = _isPresent[i] && pawnManager != null ? pawnManager.GetScore(i) : 0;
            }
            yield return CountScores();

            _winner = GetWinningTeam();
            yield return Wait(bonusDelay);

            // The winning team's bonus
            if (_winner != Team.None && winBonus != 0) {
                for (int i = 0; i < players.Length; i++) {
                    if (_isPresent[i] && _teams[i] == _winner) _finalScores[i] += winBonus;
                }
                yield return CountScores();
                yield return Wait(revealDelay);
            }

            // The reveal, all in the same frame
            // Win and lose animations (everyone wins a tie)
            for (int i = 0; i < players.Length; i++) {
                if (!_isPresent[i]) continue;
                bool won = _winner == Team.None || _teams[i] == _winner;
                PlayPortrait(i, won ? WinState : LoseState);
            }

            ShowRankings();

            // The winning team's background
            if (hanselWinBackground != null) hanselWinBackground.SetActive(_winner == Team.Hansels);
            if (witchWinBackground != null) witchWinBackground.SetActive(_winner == Team.Witches);

            // Confetti (both for a tie)
            if (_winner != Team.Witches) PlayConfetti(hanselConfetti);
            if (_winner != Team.Hansels) PlayConfetti(witchConfetti);
            foreach (AudioClip clip in revealSounds) {
                if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
            }

            _isFinished = true;
            onResultsFinished.Invoke();
        }

        // Works out who's playing and on which team, then shows their panels with every score at 0 and the portraits idle
        private void SetUpPlayers() {
            AbstractPawn[] pawns = FindObjectsByType<AbstractPawn>(FindObjectsInactive.Include);
            for (int i = 0; i < players.Length; i++) {
                _teams[i] = GetTeam(i, pawns);
                _isPresent[i] = _teams[i] != Team.None;
                _countedScores[i] = 0f;
                _finalScores[i] = 0;
                _ranks[i] = 0;

                if (players[i].container != null) players[i].container.SetActive(_isPresent[i]);
                if (!_isPresent[i]) continue;
                ShowScore(i, 0, force: true);
                PlayPortrait(i, IdleState);
            }
            RefreshTotals();
        }

        /// <returns>The team of the pawn bound to this player, or None if nobody is playing them.</returns>
        private static Team GetTeam(int playerIndex, AbstractPawn[] pawns) {
            foreach (AbstractPawn pawn in pawns) {
                if (pawn == null || pawn.playerIndex != playerIndex) continue;
                if (pawn is HanselPawn) return Team.Hansels;
                if (pawn is WitchPawn) return Team.Witches;
            }
            return Team.None;
        }

        // Moves every score towards its final score at Count Speed, until they've all got there
        private IEnumerator CountScores() {
            while (true) {
                float step = countSpeed * Time.deltaTime;
                bool isDone = true;
                int tickScore = -1; // the highest score that went up this frame, if any did
                for (int i = 0; i < players.Length; i++) {
                    if (!_isPresent[i]) continue;
                    _countedScores[i] = Mathf.MoveTowards(_countedScores[i], _finalScores[i], step);
                    int score = (int)_countedScores[i];
                    if (score != _shownScores[i]) tickScore = Mathf.Max(tickScore, score);
                    ShowScore(i, score);
                    if (_countedScores[i] != _finalScores[i]) isDone = false;
                }
                RefreshTotals();
                if (tickScore >= 0) PlayCountSound(tickScore);

                if (isDone) yield break;
                yield return null;
            }
        }

        // One tick, pitched by the score: Count Pitch At Zero at 0, rising to Count Pitch At Top at Count Top Score
        private void PlayCountSound(int score) {
            if (countSound == null || _countSource == null || Time.time < _nextCountSoundTime) return;

            _countSource.pitch = Mathf.Lerp(countPitchAtZero, countPitchAtTop, (float)score / countTopScore);
            _countSource.PlayOneShot(countSound);
            _nextCountSoundTime = Time.time + countSoundInterval;
        }

        private AudioSource AddAudioSource() {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private void ShowScore(int playerIndex, int score, bool force = false) {
            if (!force && score == _shownScores[playerIndex]) return;
            _shownScores[playerIndex] = score;
            SetText(players[playerIndex].scoreText, score.ToString());
        }

        // Each team's total is the scores its players are showing right now, so it counts up along with them
        private void RefreshTotals() {
            SetText(hanselTotalText, GetShownTotal(Team.Hansels).ToString());
            SetText(witchTotalText, GetShownTotal(Team.Witches).ToString());
        }

        private int GetShownTotal(Team team) {
            int total = 0;
            for (int i = 0; i < players.Length; i++) {
                if (_isPresent[i] && _teams[i] == team) total += _shownScores[i];
            }
            return total;
        }

        private Team GetWinningTeam() {
            int hansels = GetShownTotal(Team.Hansels);
            int witches = GetShownTotal(Team.Witches);
            if (hansels > witches) return Team.Hansels;
            if (witches > hansels) return Team.Witches;
            return Team.None;
        }

        // Place = 1 + how many players scored more, so equal scores share a place (two #1s, then #3)
        private void ShowRankings() {
            for (int i = 0; i < players.Length; i++) {
                if (!_isPresent[i]) continue;

                int rank = 1;
                for (int other = 0; other < players.Length; other++) {
                    if (_isPresent[other] && _finalScores[other] > _finalScores[i]) rank++;
                }
                _ranks[i] = rank;

                TMP_Text text = players[i].rankingText;
                if (text == null) continue;
                SetText(text, $"#{rank}");
                if (rankColors.Length > 0) text.color = rankColors[Mathf.Min(rank, rankColors.Length) - 1];
                text.gameObject.SetActive(true);
            }
        }

        // Rankings, backgrounds and confetti only appear once the winners are decided
        private void HideWinnerReveal() {
            foreach (PlayerSlot player in players) {
                if (player.rankingText != null) player.rankingText.gameObject.SetActive(false);
            }
            if (hanselWinBackground != null) hanselWinBackground.SetActive(false);
            if (witchWinBackground != null) witchWinBackground.SetActive(false);
        }

        private void PlayPortrait(int playerIndex, int stateHash) {
            Animator portrait = players[playerIndex].portrait;
            if (portrait == null || portrait.runtimeAnimatorController == null) return;
            portrait.Play(stateHash, 0, 0f); // Unity warns in the Console if the controller has no state with this name

        }

        private static void StopConfetti(ParticleSystem confetti) {
            if (confetti == null) return;
            ParticleSystem.MainModule main = confetti.main;
            main.playOnAwake = false;
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void PlayConfetti(ParticleSystem confetti) {
            if (confetti == null) return;
            confetti.gameObject.SetActive(true);
            confetti.Play(true);
        }

        // 0 = no wait
        private static IEnumerator Wait(float seconds) {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
        }

        // Keeps any line breaks after the text (the score texts end in one, which nudges them into place)
        private static void SetText(TMP_Text text, string value) {
            if (text == null) return;
            string current = text.text ?? "";
            text.text = value + current.Substring(current.TrimEnd().Length);
        }

        private bool IsValidIndex(int playerIndex) => _finalScores != null && playerIndex >= 0 && playerIndex < _finalScores.Length;
    }
}
