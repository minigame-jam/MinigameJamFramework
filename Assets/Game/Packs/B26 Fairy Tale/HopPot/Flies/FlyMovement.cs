using UnityEngine;

namespace HopPot {
    public class FlyMovement : MonoBehaviour
    {

        float turnTimer = 0f;
        float speed = 10f;
        float maxSpeed = 10f;
        Rigidbody body;
        SpriteRenderer sprite;
        Vector3 randomDirection;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            sprite = GetComponent<SpriteRenderer>();
            transform.position = new Vector3(0f,0f,0f);
        }

        // Update is called once per frame
        void Update()
        {
            if (turnTimer <= 0) {
                turnTimer = Random.Range(1f, 1.5f);
                randomDirection = new Vector3(Random.Range(-1f,1f), 0f, Random.Range(-1f,1f)).normalized;
                if (transform.position.x > 15) {
                    randomDirection.x = -Mathf.Abs(randomDirection.x);
                }
                else if (transform.position.x < -15) {
                    randomDirection.x = Mathf.Abs(randomDirection.x);
                }

                if (transform.position.z > 15) {
                    randomDirection.z = -Mathf.Abs(randomDirection.z);
                }
                if (transform.position.z < -15) {
                    randomDirection.z = Mathf.Abs(randomDirection.z);
                }

                body.linearVelocity = randomDirection * speed;
                speed = maxSpeed;
                
                if (body.linearVelocity.x > 0) {
                    sprite.flipX = true;
                }
                else {
                    sprite.flipX = false;
                }
            }
            else {
                turnTimer -= Time.deltaTime;
                if (speed > 0) {
                    speed -= Time.deltaTime * maxSpeed;
                }
                body.linearVelocity = randomDirection * speed;
            }
        }
    }
}