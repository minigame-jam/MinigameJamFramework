using UnityEngine;

namespace SnowDay.SnowFight {
    public class Billboard : MonoBehaviour {
        void OnEnable() => Camera.onPreRender += HandlePreRender;
        void OnDisable() => Camera.onPreRender -= HandlePreRender;

        private void HandlePreRender(Camera camera) {
            transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward,
                    camera.transform.rotation * Vector3.up);
        }
    }
}