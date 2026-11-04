using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.MinigameFramework.Scripts {
    public class SceneLoader : MonoBehaviour
    {
        public void LoadScene(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }
        public void LoadScene(SceneField scene) {
            SceneManager.LoadScene(scene);
        }
    }
}
