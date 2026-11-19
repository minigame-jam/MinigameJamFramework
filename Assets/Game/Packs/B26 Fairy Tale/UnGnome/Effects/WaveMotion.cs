using UnityEngine;

namespace GnomeSpace {
    public class WaveMotion : MonoBehaviour
    {
        // Used to determined offset
        Vector3 initialPosition;
        float timeAlive = 0f;

        // Allows waves to be differentiated
        [SerializeField] private float cycleProgressOffset = 0f;
        [SerializeField] private float amplitude = .2f;
        [SerializeField] private float speed = 2f;
        
        // Records the initialPosition
        void Start() {
            initialPosition = transform.position;
            timeAlive = 2 * Mathf.PI * cycleProgressOffset;
        }

        void Update() {
            // Record new offset
            timeAlive += Time.deltaTime * speed;
            Vector3 offset = new Vector3(Mathf.Cos(timeAlive), Mathf.Sin(timeAlive), 0);
            offset *= amplitude;

            // Apply it to position
            transform.position = initialPosition + offset;
        }
    }
}