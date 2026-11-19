using HopPot;
using UnityEngine;
using UnityEngine.UI;


namespace HopPot {
    public class TimerBar : MonoBehaviour {

        public static TimerBar Instance { get; private set; }

        [SerializeField] private Image timerBar;
        [SerializeField] private float timerDuration;
        private bool countDown;

        private float currentTime;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() {
            currentTime = timerDuration;
            countDown = false;
            timerBar.fillAmount = 0f;
        }

        private void Update() {

            if (currentTime < 0f) {
                WitchMovement.Instance.CookFrog();
                ResetTimer();
            }

            if (countDown) {
                currentTime -= Time.deltaTime;
                timerBar.fillAmount = 1f - (currentTime / timerDuration);
            }
        }

        public void StartTimer() {
            countDown = true;
        }

        public bool isCountDown() {
            return countDown;
        }
        
        public void ResetTimer() {
            countDown = false;
            currentTime = timerDuration;
            timerBar.fillAmount = 0f;
        }

        private void OnDestroy() {
            if (Instance == this)
                Instance = null;
        }
    }
}