using UnityEngine;


namespace HopPot {

    public class Cauldron : MonoBehaviour {

        float flySpawnTime = 4f;
        float flySpawnCooldown = 4f;
        [SerializeField] private float spawnHeight = 3f;

        [SerializeField] private GameObject fly;

        void Update() {
            if (flySpawnCooldown <= 0) {
                flySpawnCooldown = flySpawnTime;
                Vector3 spawnPosition = new Vector3(transform.position.x, spawnHeight,transform.position.z);
                Instantiate(fly, spawnPosition, Quaternion.identity);
            }
            else {
                flySpawnCooldown-= Time.deltaTime;
            }
        }

    }
}
