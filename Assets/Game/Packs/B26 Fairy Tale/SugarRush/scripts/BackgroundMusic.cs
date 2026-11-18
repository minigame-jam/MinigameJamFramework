using System.Collections;
using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Loops the background music. It starts at Volume when the opening's Part 2 shows (OpeningSequence's
    /// On Round Start, the moment the players get control), and fades down to Ending Volume when time runs out and the
    /// results come in (RoundTimer's On Time Up). With no opening in the scene it starts straight away.
    /// It plays through an AudioSource of its own, so its volume never touches the other sounds.
    /// Put this on the StarterGameManager object.
    /// </summary>
    public class BackgroundMusic : MonoBehaviour {
        [Tooltip("Looped from when the opening's Part 2 shows until the scene ends.")]
        [SerializeField] private AudioClip music;
        [Tooltip("Loudness during the round, as a fraction of the clip's normal volume (0.3 = 30%).")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
        [Tooltip("Loudness once time runs out and the results show. Keep it below Volume.")]
        [SerializeField, Range(0f, 1f)] private float endingVolume = 0.12f;
        [Tooltip("Seconds the music takes to fade from Volume down to Ending Volume. 0 = straight away.")]
        [SerializeField, Min(0f)] private float endingFadeDuration = 1f;

        [Tooltip("The music starts when this opening shows Part 2. Found automatically if left empty.")]
        [SerializeField] private OpeningSequence opening;
        [Tooltip("The music gets quieter when this timer runs out. Found automatically if left empty.")]
        [SerializeField] private RoundTimer timer;

        private AudioSource _source;
        private bool _isEnding;

        private void Awake() {
            if (opening == null) opening = FindAnyObjectByType<OpeningSequence>();
            if (timer == null) timer = FindAnyObjectByType<RoundTimer>();

            // A source of its own: PlayOneShot on a shared source would be scaled by the music's volume
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f; // 2D: the same loudness wherever the camera is
            _source.clip = music;
            _source.volume = volume;
        }

        // OnEnable runs before any Start, so the listeners are in place before the opening or the round can finish
        private void OnEnable() {
            if (opening != null) opening.onRoundStart.AddListener(StartMusic);
            if (timer != null) timer.onTimeUp.AddListener(StartEnding);
        }

        private void OnDisable() {
            if (opening != null) opening.onRoundStart.RemoveListener(StartMusic);
            if (timer != null) timer.onTimeUp.RemoveListener(StartEnding);
        }

        private void Start() {
            // No opening to wait for (or it already got there, or it's switched off and never will)
            if (opening == null || opening.HasRoundStarted || !opening.isActiveAndEnabled) StartMusic();
            if (timer != null && timer.IsRoundOver) StartEnding();
        }

        private void StartMusic() {
            if (music == null || _source.isPlaying) return;
            _source.volume = _isEnding ? endingVolume : volume;
            _source.Play();
        }

        private void StartEnding() {
            if (_isEnding) return;
            _isEnding = true;
            StartCoroutine(FadeTo(endingVolume, endingFadeDuration));
        }

        private IEnumerator FadeTo(float target, float duration) {
            float start = _source.volume;
            for (float t = 0f; t < duration; t += Time.deltaTime) {
                _source.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            _source.volume = target;
        }
    }
}
