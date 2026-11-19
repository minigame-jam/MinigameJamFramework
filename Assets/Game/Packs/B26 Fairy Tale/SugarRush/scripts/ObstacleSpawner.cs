using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif

namespace SugarRush {
    /// <summary>
    /// Scatters obstacles over the map when the game starts.
    /// Put this on the Obstacles object: anything already under it is cleared, then a random number of obstacles
    /// (Min to Max Obstacles) is placed on random cells of a grid covering the play area, never on top of a pawn.
    /// Each obstacle is a random prefab from the Obstacles folder. In the editor the prefab list keeps itself in sync with
    /// that folder, so a new obstacle only needs its prefab dropped in there.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour {
        [Header("Obstacles")]
        [Tooltip("Folder the obstacle prefabs are picked from, subfolders included. Found automatically when the component is added.")]
        [SerializeField] private Object obstaclesFolder;
        [Tooltip("Filled in from Obstacles Folder automatically in the editor. Each obstacle is a random pick from this list.")]
        [SerializeField] private List<GameObject> obstaclePrefabs = new();

        [Header("Count")]
        [Tooltip("Fewest obstacles to spawn. Fewer are spawned only if there aren't enough free cells.")]
        [SerializeField, Min(0)] private int minObstacles = 3;
        [Tooltip("Most obstacles to spawn.")]
        [SerializeField, Min(0)] private int maxObstacles = 6;

        [Header("Grid")]
        [Tooltip("Centre of the area obstacles can spawn in, relative to this object. Select this object to see the grid in the Scene view.")]
        [SerializeField] private Vector2 areaCenter = Vector2.zero;
        [Tooltip("Size of the area obstacles can spawn in. The default fills the space between the map's walls.")]
        [SerializeField] private Vector2 areaSize = new(17f, 9f);
        [Tooltip("Size of one grid cell. Each obstacle sits in the middle of a cell, and no two share one.")]
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        [Header("Players")]
        [Tooltip("Obstacles stay at least this far from each pawn's feet (the centre of its collider), so nobody starts stuck inside one.")]
        [SerializeField, Min(0f)] private float playerClearance = 0.5f;

        // Small allowance so an area of e.g. 17 units still fits 17 cells despite float rounding
        private const float CellFitTolerance = 0.001f;

        private Vector2Int CellCount => new(
            Mathf.Max(0, Mathf.FloorToInt(areaSize.x / cellSize + CellFitTolerance)),
            Mathf.Max(0, Mathf.FloorToInt(areaSize.y / cellSize + CellFitTolerance)));

        // Bottom-left corner of the grid. The grid is centred in the area if the area isn't a whole number of cells.
        private Vector2 GridOrigin => areaCenter - (Vector2)CellCount * (cellSize * 0.5f);

        private void Start() {
#if UNITY_EDITOR
            RefreshObstaclePrefabs(); // pick up prefabs added to the folder since the scene was last saved
#endif
            Respawn();
        }

        /// <summary>Destroys every obstacle under this object and scatters a fresh set.</summary>
        public void Respawn() {
            ClearObstacles();
            SpawnObstacles();
        }

        private void ClearObstacles() {
            for (int i = transform.childCount - 1; i >= 0; i--) {
                Transform child = transform.GetChild(i);
                child.SetParent(null); // Destroy only happens at the end of the frame, so get it out of the way now
                Destroy(child.gameObject);
            }
        }

        private void SpawnObstacles() {
            List<GameObject> prefabs = obstaclePrefabs.FindAll(prefab => prefab != null);
            if (prefabs.Count == 0) {
                Debug.LogWarning($"{nameof(ObstacleSpawner)} on {name} has no obstacle prefabs. Check Obstacles Folder.", this);
                return;
            }

            List<Vector2> freeCells = GetFreeCells();
            int count = Mathf.Min(Random.Range(Mathf.Min(minObstacles, maxObstacles), maxObstacles + 1), freeCells.Count);

            for (int i = 0; i < count; i++) {
                // Swap a random not-yet-used cell into slot i, so every cell is equally likely and none is used twice
                int pick = Random.Range(i, freeCells.Count);
                (freeCells[i], freeCells[pick]) = (freeCells[pick], freeCells[i]);

                GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                Instantiate(prefab, transform.TransformPoint(freeCells[i]), transform.rotation, transform);
            }
        }

        /// <returns>The centre of every cell that isn't too close to a pawn, relative to this object.</returns>
        private List<Vector2> GetFreeCells() {
            List<Vector2> pawnFeet = GetPawnFeet();
            Vector2Int cells = CellCount;
            Vector2 origin = GridOrigin;
            List<Vector2> free = new(cells.x * cells.y);

            for (int x = 0; x < cells.x; x++) {
                for (int y = 0; y < cells.y; y++) {
                    Vector2 center = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                    if (!IsNearPawn(center, pawnFeet)) free.Add(center);
                }
            }
            return free;
        }

        // Includes hidden pawns: a pawn with no player yet appears where it stands if one joins later
        private List<Vector2> GetPawnFeet() {
            List<Vector2> feet = new();
            foreach (Pawn pawn in FindObjectsByType<Pawn>(FindObjectsInactive.Include)) {
                feet.Add(transform.InverseTransformPoint(AbstractPawn.GetRestingFeet(pawn)));
            }
            return feet;
        }

        private bool IsNearPawn(Vector2 cellCenter, List<Vector2> pawnFeet) {
            Vector2 halfCell = Vector2.one * (cellSize * 0.5f);
            foreach (Vector2 feet in pawnFeet) {
                // Nearest point of the cell to the feet (the feet themselves if they're inside it)
                Vector2 nearest = Vector2.Max(cellCenter - halfCell, Vector2.Min(feet, cellCenter + halfCell));
                if ((feet - nearest).sqrMagnitude <= playerClearance * playerClearance) return true;
            }
            return false;
        }

        // Shows the spawn grid when this object is selected
        private void OnDrawGizmosSelected() {
            Vector2Int cells = CellCount;
            Vector2 origin = GridOrigin;
            Vector2 size = (Vector2)cells * cellSize;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            for (int x = 1; x < cells.x; x++) {
                Gizmos.DrawLine(origin + new Vector2(x * cellSize, 0f), origin + new Vector2(x * cellSize, size.y));
            }
            for (int y = 1; y < cells.y; y++) {
                Gizmos.DrawLine(origin + new Vector2(0f, y * cellSize), origin + new Vector2(size.x, y * cellSize));
            }
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 1f);
            Gizmos.DrawWireCube(origin + size * 0.5f, size);
        }

#if UNITY_EDITOR
        // Runs when the component is added: find the Obstacles folder so it works straight away
        private void Reset() {
            obstaclesFolder = FindObstaclesFolder();
            RefreshObstaclePrefabs();
        }

        private void OnValidate() {
            if (minObstacles > maxObstacles) minObstacles = maxObstacles;
            // Deferred: loading assets inside OnValidate can fail while Unity is still importing them
            EditorApplication.delayCall += RefreshObstaclePrefabs;
        }

        /// Replaces the prefab list with every prefab in Obstacles Folder (and its subfolders).
        private void RefreshObstaclePrefabs() {
            if (this == null || obstaclesFolder == null) return; // destroyed before the delayed call ran, or no folder set

            string folder = AssetDatabase.GetAssetPath(obstaclesFolder);
            if (!AssetDatabase.IsValidFolder(folder)) {
                Debug.LogWarning($"{nameof(ObstacleSpawner)} on {name}: Obstacles Folder should be a folder, but {folder} isn't one.", this);
                return;
            }

            List<GameObject> found = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .OrderBy(path => path)
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(prefab => prefab != null)
                .ToList();

            if (obstaclePrefabs != null && found.SequenceEqual(obstaclePrefabs)) return;
            obstaclePrefabs = found;
            EditorUtility.SetDirty(this); // so the scene saves the new list, which builds use
        }

        // A folder named Obstacles anywhere under the scene's folder, nearest first
        private Object FindObstaclesFolder() {
            string scenePath = gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath)) return null; // not in a saved scene

            string sceneFolder = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
            string folder = sceneFolder != null ? FindSubFolder(sceneFolder, "Obstacles") : null;
            return folder != null ? AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder) : null;
        }

        private static string FindSubFolder(string parent, string folderName) {
            string[] subFolders = AssetDatabase.GetSubFolders(parent);
            foreach (string sub in subFolders) {
                if (string.Equals(Path.GetFileName(sub), folderName, System.StringComparison.OrdinalIgnoreCase)) return sub;
            }
            foreach (string sub in subFolders) {
                string found = FindSubFolder(sub, folderName);
                if (found != null) return found;
            }
            return null;
        }
#endif
    }
}
