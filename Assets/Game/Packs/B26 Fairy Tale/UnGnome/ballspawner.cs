using UnityEngine;
using UnityEngine.UIElements;

namespace GnomeSpace {
    public class ballspawner : MonoBehaviour {
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        public static Vector3 p;
        public static Vector3 getPosition() {
            return p;
        }
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            p = this.transform.position;
        }


    }
}