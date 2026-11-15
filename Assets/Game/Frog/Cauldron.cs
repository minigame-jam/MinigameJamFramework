using UnityEngine;


namespace Caul {

    public class Cauldron : MonoBehaviour {

        float flySpawnTime = 4f;
        float flySpawnCooldown = 4f;

        [SerializeField] private GameObject fly;

        void FixedUpdate() {
            if (flySpawnCooldown <= 0) {
                flySpawnCooldown = flySpawnTime;
                Instantiate(fly);
            }
            else {
                flySpawnCooldown-= Time.deltaTime;
            }
        }

    }
}
