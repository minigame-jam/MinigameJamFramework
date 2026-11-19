using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SugarRush {
    /// <summary>
    /// Counts the round down from Round Duration, showing the whole seconds left (35, 34, ... 0).
    /// The countdown starts when the opening hands control to the players (OpeningSequence's On Round Start).
    /// When it reaches 0, every pawn freezes (AbstractPawn.Freeze: no control, and nothing can touch it),
    /// the timer and the ScoreBoard play their "collapse" animations, then the Results screen shows and counts up the scores.
    /// Each time the seconds go down, it ticks: the text's Animator plays normal_tick and Normal Tick Sound plays, or, at
    /// End Tick Seconds or fewer, end_tick plays and End Tick Sound plays along with Normal Tick Sound.
    /// Put this on the RoundTimer text.
    /// </summary>
    public class RoundTimer : MonoBehaviour {
        [Tooltip("Seconds the round lasts, counted from when the players get control.")]
        [SerializeField, Min(1f)] private float roundDuration = 35f;
        [Tooltip("Text that shows the seconds left. Auto-found on this object if left empty.")]
        [SerializeField] private TMP_Text timerText;
        [Tooltip("The countdown starts when this opening hands control to the players. Found automatically if left empty; " +
                 "with no opening in the scene the countdown starts straight away.")]
        [SerializeField] private OpeningSequence opening;
        [Tooltip("Shown when time runs out. Found automatically if left empty.")]
        [SerializeField] private ResultsScreen results;
        [Tooltip("Collapses along with the timer when time runs out. Found automatically if left empty.")]
        [SerializeField] private ScoreBoard scoreBoard;
        [Tooltip("Called once when time runs out, after every pawn has frozen and before the results show.")]
        public UnityEvent onTimeUp = new();

        [Header("Ticks")]
        [Tooltip("Animator on the timer text with normal_tick and end_tick states. One of them restarts every time the seconds go down, " +
                 "and its collapse state plays when time runs out. Auto-found on this object if left empty.")]
        [SerializeField] private Animator animator;
        [Tooltip("At this many seconds left or fewer, each tick plays end_tick instead of normal_tick, and End Tick Sound on top of Normal Tick Sound.")]
        [SerializeField, Min(0)] private int endTickSeconds = 10;
        [Tooltip("Played every time the seconds go down, all the way to the end.")]
        [SerializeField] private AudioClip normalTickSound;
        [Tooltip("Normal Tick Sound's loudness, as a fraction of the clip's normal volume (0.15 = 15%).")]
        [SerializeField, Range(0f, 1f)] private float normalTickVolume = 0.15f;
        [Tooltip("Played together with Normal Tick Sound each time the seconds go down, once End Tick Seconds or fewer are left.")]
        [SerializeField] private AudioClip endTickSound;
        [Tooltip("End Tick Sound's loudness, as a fraction of the clip's normal volume (0.5 = half).")]
        [SerializeField, Range(0f, 1f)] private float endTickVolume = 0.5f;
        [Tooltip("Plays the tick sounds. Auto-found on this object, or added, if left empty.")]
        [SerializeField] private AudioSource audioSource;

        private static readonly int NormalTickState = Animator.StringToHash("normal_tick");
        private static readonly int EndTickState = Animator.StringToHash("end_tick");
        private static readonly int CollapseState = Animator.StringToHash("collapse");

        private float _timeLeft;
        private bool _isRunning;
        private bool _isOver;
        private int _shownSeconds = -1;

        /// Seconds left in the round.
        public float TimeLeft => _timeLeft;
        /// True once time has run out.
        public bool IsRoundOver => _isOver;

        private void Awake() {
            if (timerText == null) timerText = GetComponent<TMP_Text>();
            if (opening == null) opening = FindAnyObjectByType<OpeningSequence>();
            if (results == null) results = FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include);
            if (scoreBoard == null) scoreBoard = FindAnyObjectByType<ScoreBoard>();
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            _timeLeft = roundDuration;
            ShowTime();
        }

        // OnEnable runs before any Start, so the listener is in place before the opening can finish
        private void OnEnable() {
            if (opening != null) opening.onRoundStart.AddListener(StartCountdown);
        }

        private void OnDisable() {
            if (opening != null) opening.onRoundStart.RemoveListener(StartCountdown);
        }

        private void Start() {
            // No opening to wait for (or it already finished, or it's switched off and never will)
            if (opening == null || opening.HasRoundStarted || !opening.isActiveAndEnabled) StartCountdown();
        }

        private void StartCountdown() {
            if (!_isOver) _isRunning = true;
        }

        private void Update() {
            if (!_isRunning) return;

            _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
            ShowTime();
            if (_timeLeft <= 0f) EndRound();
        }

        // Rounded up, so it reads 35 for the whole first second and only reaches 0 as the round ends
        private void ShowTime() {
            int seconds = Mathf.CeilToInt(_timeLeft);
            if (seconds == _shownSeconds || timerText == null) return;
            _shownSeconds = seconds;
            timerText.text = seconds.ToString();

            // Tick as the seconds go down (34 ... 1). Not before the countdown starts, and not on 0, when the round ends.
            if (_isRunning && seconds > 0) Tick(seconds);
        }

        private void Tick(int seconds) {
            bool isEnding = seconds <= endTickSeconds;
            PlayAnimation(isEnding ? EndTickState : NormalTickState);

            // The little tick plays every second; in the last End Tick Seconds the big tick plays with it
            PlaySound(normalTickSound, normalTickVolume);
            if (isEnding) PlaySound(endTickSound, endTickVolume);
        }

        private void PlaySound(AudioClip clip, float volume) {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
        }

        // From the first frame, so every tick's animation lines up with the number changing
        private void PlayAnimation(int stateHash) {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
            if (!animator.HasState(0, stateHash)) return;
            animator.Play(stateHash, 0, 0f);
        }

        private void EndRound() {
            _isRunning = false;
            _isOver = true;

            // Include hidden pawns, so a player who joins after the round ends can't play either
            foreach (AbstractPawn pawn in FindObjectsByType<AbstractPawn>(FindObjectsInactive.Include)) {
                if (pawn != null) pawn.Freeze();
            }

            // The timer and the scores slide away as the results come in
            PlayAnimation(CollapseState);
            if (scoreBoard != null) scoreBoard.Collapse();

            onTimeUp.Invoke();

            if (results != null) results.Show();
            else Debug.LogWarning($"{nameof(RoundTimer)} on {name}: no {nameof(ResultsScreen)} in the scene, so no results were shown.", this);
        }
    }
}
