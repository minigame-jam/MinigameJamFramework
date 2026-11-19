using UnityEngine;

namespace GnomeSpace
{
    public class ProjectileCollision : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (Random.Range(0, 3) == 0) {
                GetComponent<AudioSource>().Play();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
        
        void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.layer == 20) {
                        Destroy(this.gameObject);
            }
            if (collision.gameObject.layer == 12) {
                Destroy(this.gameObject);
            }
        }
    }
}
