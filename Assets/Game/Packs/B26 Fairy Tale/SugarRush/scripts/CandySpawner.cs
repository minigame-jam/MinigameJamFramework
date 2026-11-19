using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;

namespace SugarRush {
    /// <summary>
    /// Scatters candy over the map when the round starts, then keeps topping it back up as candy gets grabbed.
    /// Put this on the Candies object: anything already under it is cleared, then up to Max Candies pop in one at a time,
    /// sweeping across the map from left to right. Spots aren't on a grid; any point works as long as the candy isn't
    /// inside a barrier (any solid collider, e.g. walls and obstacles), isn't near a pawn, and isn't on top of another candy.
    /// After that, while fewer than Max Candies are on the map, one more appears every Regenerate Interval seconds.
    /// Runs after ObstacleSpawner (see DefaultExecutionOrder), so this round's obstacles are already there to avoid.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CandySpawner : MonoBehaviour {
        [Header("Candy")]
        [Tooltip("Prefab placed for each candy.")]
        [SerializeField] private Candy candyPrefab;
        [Tooltip("Most candies on the map at once. The round starts with this many (fewer only if the map runs out of room).")]
        [SerializeField, Min(0)] private int maxCandies = 20;

        [Header("Opening Wave")]
        [Tooltip("Seconds between candies popping in when the round starts. They appear one at a time, sweeping from the left of the map to the right. 0 = all at once.")]
        [SerializeField, Min(0f)] private float waveInterval = 0.06f;

        [Header("Regeneration")]
        [Tooltip("While there are fewer than Max Candies on the map, one more appears every this many seconds. " +
                 "The wait starts from the first grab off a full map, so candy never comes back instantly.")]
        [SerializeField, Min(0.05f)] private float regenerateInterval = 1.5f;

        [Header("Area")]
        [Tooltip("Centre of the area candy can spawn in, relative to this object. Select this object to see the area in the Scene view.")]
        [SerializeField] private Vector2 areaCenter = Vector2.zero;
        [Tooltip("Size of the area candy can spawn in. The default fills the space between the map's walls.")]
        [SerializeField] private Vector2 areaSize = new(17f, 9f);

        [Header("Spacing")]
        [Tooltip("Room each candy needs around its centre: no solid collider (wall, obstacle, pawn) may overlap a circle this size.")]
        [SerializeField, Min(0f)] private float candyRadius = 0.3f;
        [Tooltip("Candies stay at least this far from each pawn's feet (the centre of its collider), so they never appear right under someone.")]
        [SerializeField, Min(0f)] private float playerClearance = 1f;
        [Tooltip("Smallest distance between two candies' centres, so they don't pile up on each other.")]
        [SerializeField, Min(0f)] private float candySpacing = 0.6f;

        // Random spots tried for each candy before giving up on it
        private const int AttemptsPerCandy = 30;

        // Other candies and pickups are triggers, so they aren't barriers
        private static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        // Candies on the map that can still be grabbed. Grabbed ones drop out while they play their grab animation.
        private readonly List<Candy> _candies = new();
        private readonly List<Collider2D> _overlapResults = new();
        private Pawn[] _pawns;
        private float _regenerateTimer;
        private Coroutine _wave;
        private bool _isSpawningWave;

        private void Start() {
            Respawn();
        }

        private void OnDisable() {
            _isSpawningWave = false; // disabling stops the wave coroutine, so let regeneration fill in the rest
        }

        private void Update() {
            if (candyPrefab == null || _isSpawningWave) return;

            _candies.RemoveAll(candy => candy == null || candy.IsGrabbed);

            if (_candies.Count >= maxCandies) {
                _regenerateTimer = 0f; // full map: the next candy waits a whole interval after a grab
                return;
            }

            _regenerateTimer += Time.deltaTime;
            if (_regenerateTimer < regenerateInterval) return;

            _regenerateTimer = 0f; // if there's no room right now, try again after another interval
            TrySpawnCandy(GetPawnFeet());
        }

        /// <summary>Destroys every candy under this object and starts a fresh opening wave.</summary>
        public void Respawn() {
            if (_wave != null) StopCoroutine(_wave);
            ClearCandies();
            _wave = StartCoroutine(SpawnWave());
        }

        private void ClearCandies() {
            for (int i = transform.childCount - 1; i >= 0; i--) {
                Transform child = transform.GetChild(i);
                child.SetParent(null); // Destroy only happens at the end of the frame, so get it out of the way now
                Destroy(child.gameObject);
            }
            _candies.Clear();
            _regenerateTimer = 0f;
        }

        // Picks every spot up front, then pops the candies in one at a time from left to right
        private IEnumerator SpawnWave() {
            if (candyPrefab == null) {
                Debug.LogWarning($"{nameof(CandySpawner)} on {name} has no Candy Prefab.", this);
                yield break;
            }

            _isSpawningWave = true;
            Physics2D.SyncTransforms(); // make sure the physics queries see obstacles just spawned this frame where they really are

            List<Vector2> spots = PickWaveSpots(GetPawnFeet());
            if (spots.Count < maxCandies) {
                Debug.Log($"{nameof(CandySpawner)} on {name} only found room for {spots.Count} of {maxCandies} candies.", this);
            }

            for (int i = 0; i < spots.Count; i++) {
                if (i > 0 && waveInterval > 0f) yield return new WaitForSeconds(waveInterval);

                // A pawn may have walked onto the spot since it was picked
                List<Vector2> pawnFeet = GetPawnFeet();
                if (IsFree(spots[i], pawnFeet)) SpawnCandyAt(spots[i]);
                else TrySpawnCandy(pawnFeet);
            }

            _isSpawningWave = false;
            _regenerateTimer = 0f;
            _wave = null;
        }

        /// <returns>Up to Max Candies free spots, spaced apart from each other, sorted left to right.</returns>
        private List<Vector2> PickWaveSpots(List<Vector2> pawnFeet) {
            List<Vector2> spots = new(maxCandies);
            for (int attempts = maxCandies * AttemptsPerCandy; attempts > 0 && spots.Count < maxCandies; attempts--) {
                Vector2 point = GetRandomPointInArea();
                if (IsFree(point, pawnFeet, spots)) spots.Add(point);
            }
            spots.Sort((a, b) => a.x.CompareTo(b.x)); // so the wave sweeps across the map
            return spots;
        }

        /// <param name="pawnFeet">World positions candy must stay clear of, from GetPawnFeet.</param>
        /// <returns>False if no free spot was found.</returns>
        private bool TrySpawnCandy(List<Vector2> pawnFeet) {
            for (int attempt = 0; attempt < AttemptsPerCandy; attempt++) {
                Vector2 point = GetRandomPointInArea();
                if (!IsFree(point, pawnFeet)) continue;

                SpawnCandyAt(point);
                return true;
            }
            return false;
        }

        /// <param name="point">World position.</param>
        private void SpawnCandyAt(Vector2 point) {
            Vector3 position = new(point.x, point.y, transform.position.z);
            _candies.Add(Instantiate(candyPrefab, position, transform.rotation, transform));
        }

        /// <returns>A random world position inside the area.</returns>
        private Vector2 GetRandomPointInArea() {
            Vector2 local = areaCenter + Vector2.Scale(areaSize, new Vector2(Random.value - 0.5f, Random.value - 0.5f));
            return transform.TransformPoint(local);
        }

        /// <param name="point">World position to check.</param>
        /// <param name="reserved">Extra spots to keep Candy Spacing away from, e.g. ones picked for candies that haven't appeared yet.</param>
        private bool IsFree(Vector2 point, List<Vector2> pawnFeet, List<Vector2> reserved = null) {
            foreach (Vector2 feet in pawnFeet) {
                if ((point - feet).sqrMagnitude < playerClearance * playerClearance) return false;
            }
            foreach (Candy candy in _candies) {
                if (candy == null) continue;
                if (IsTooClose(point, candy.transform.position)) return false;
            }
            if (reserved != null) {
                foreach (Vector2 spot in reserved) {
                    if (IsTooClose(point, spot)) return false;
                }
            }
            return Physics2D.OverlapCircle(point, candyRadius, SolidOnly, _overlapResults) == 0;
        }

        private bool IsTooClose(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < candySpacing * candySpacing;

        // World positions. Includes hidden pawns: a pawn with no player yet has no collider in the physics world,
        // but appears where it stands if one joins later
        private List<Vector2> GetPawnFeet() {
            _pawns ??= FindObjectsByType<Pawn>(FindObjectsInactive.Include);

            List<Vector2> feet = new(_pawns.Length);
            foreach (Pawn pawn in _pawns) {
                if (pawn == null) continue;
                feet.Add(AbstractPawn.GetRestingFeet(pawn));
            }
            return feet;
        }

        // Shows the spawn area when this object is selected
        private void OnDrawGizmosSelected() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.4f, 0.8f, 1f);
            Gizmos.DrawWireCube(areaCenter, areaSize);
        }
    }
}
