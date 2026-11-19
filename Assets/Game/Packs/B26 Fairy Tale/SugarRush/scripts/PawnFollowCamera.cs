using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Keeps the camera on the average position of every visible pawn, and zooms out a little as
    /// they spread apart and back in as they come together.
    /// Pawns with no player are hidden by AbstractPawn, so they're ignored.
    /// When the game starts, the camera begins zoomed out at Opening Size and eases in to its normal zoom over Opening Zoom Duration.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PawnFollowCamera : MonoBehaviour {
        [Header("Follow")]
        [Tooltip("Roughly how many seconds the camera takes to catch up to the pawns. 0 = no smoothing.")]
        [SerializeField] private float followSmoothTime = 0.3f;

        [Header("Zoom")]
        [Tooltip("Orthographic size when the pawns are close together.")]
        [SerializeField] private float closeSize = 5f;
        [Tooltip("Orthographic size when the pawns are Far Spread or more apart.")]
        [SerializeField] private float farSize = 6.5f;
        [Tooltip("How far apart the pawns must be (world units, measured vertically; horizontal distance is divided by the screen's aspect ratio) for the camera to be fully zoomed out.")]
        [SerializeField] private float farSpread = 8f;
        [Tooltip("Roughly how many seconds the zoom takes to catch up. 0 = no smoothing.")]
        [SerializeField] private float zoomSmoothTime = 0.5f;

        [Header("Opening Zoom")]
        [Tooltip("Orthographic size the camera starts at when the game starts: zoomed out further than usual. It eases in from here to its normal zoom over Opening Zoom Duration.")]
        [SerializeField] private float openingSize = 7.5f;
        [Tooltip("Seconds the opening zoom-in takes. Matches Part 1 of the opening (OpeningSequence) by default. 0 = no opening zoom.")]
        [SerializeField, Min(0f)] private float openingZoomDuration = 1.5f;

        private Camera _camera;
        private AbstractPawn[] _pawns;
        private Vector3 _followVelocity;
        private float _zoomVelocity;
        private bool _hasSnapped;
        private float _sceneSize;
        private float _openingZoomStartTime;

        private bool IsOpeningZoom => Time.time < _openingZoomStartTime + openingZoomDuration;

        private void Awake() {
            _camera = GetComponent<Camera>();
            _sceneSize = _camera.orthographicSize;
        }

        private void Start() {
            // Include inactive pawns: unbound ones start hidden but appear if a player joins later
            _pawns = FindObjectsByType<AbstractPawn>(FindObjectsInactive.Include);

            _openingZoomStartTime = Time.time;
            if (openingZoomDuration > 0f) _camera.orthographicSize = openingSize;
        }

        // LateUpdate so every pawn has already moved this frame
        private void LateUpdate() {
            bool hasPawns = TryGetVisiblePawns(out Vector3 average, out Bounds bounds);
            if (!hasPawns && !IsOpeningZoom) return; // nobody visible, stay put

            // With nobody visible yet, the opening zoom eases back to the size the camera has in the scene
            float targetSize = _sceneSize;
            if (hasPawns) {
                Vector3 targetPosition = new(average.x, average.y, transform.position.z);

                // Compare spread in screen-height units so wide and tall spreads zoom out the same amount
                float spread = Mathf.Max(bounds.size.y, bounds.size.x / _camera.aspect);
                targetSize = Mathf.Lerp(closeSize, farSize, Mathf.InverseLerp(0f, farSpread, spread));

                // Jump straight to the pawns on the first frame instead of sliding in from the scene's camera position
                if (!_hasSnapped) {
                    _hasSnapped = true;
                    transform.position = targetPosition;
                    if (!IsOpeningZoom) {
                        _camera.orthographicSize = targetSize;
                        return;
                    }
                }
                else {
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _followVelocity, followSmoothTime);
                }
            }

            if (IsOpeningZoom) {
                // Ease in from the opening size to wherever the zoom would normally be right now
                float progress = (Time.time - _openingZoomStartTime) / openingZoomDuration;
                _camera.orthographicSize = Mathf.Lerp(openingSize, targetSize, Mathf.SmoothStep(0f, 1f, progress));
                _zoomVelocity = 0f; // so normal smoothing picks up from a standstill when the opening zoom ends
                return;
            }

            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetSize, ref _zoomVelocity, zoomSmoothTime);
        }

        /// <returns>False if no pawns are visible.</returns>
        private bool TryGetVisiblePawns(out Vector3 average, out Bounds bounds) {
            average = Vector3.zero;
            bounds = default;
            if (_pawns == null) return false;

            int count = 0;
            foreach (AbstractPawn pawn in _pawns) {
                if (pawn == null || !pawn.gameObject.activeInHierarchy) continue;

                Vector3 position = pawn.transform.position;
                if (count == 0) bounds = new Bounds(position, Vector3.zero);
                else bounds.Encapsulate(position);
                average += position;
                count++;
            }

            if (count == 0) return false;
            average /= count;
            return true;
        }
    }
}
