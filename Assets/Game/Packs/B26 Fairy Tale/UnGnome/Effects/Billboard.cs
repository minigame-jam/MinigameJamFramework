using UnityEngine;

// Makes the object always orient itself towards the active camera


namespace GnomeSpace {
    public class Billboard : MonoBehaviour
    {
        Camera currentCamera;

        void Start() {
            currentCamera = FindAnyObjectByType<Camera>();
        }


        // After everything else has been processed, garauntee this object's rotation
        void LateUpdate() {
            transform.LookAt(currentCamera.transform.position);
        }
    }
}