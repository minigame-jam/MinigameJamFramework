using UnityEngine;
namespace GnomeSpace{
public class SprayTimer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]double cooldown = 0.5;
    double time = 0;
    public bool ticking = false;

    
    public void startTimer()
    {
        //time = cooldown;
        ticking = true;
    }

    void Start() {

    }

    // Update is called once per frame
    void Update()
    {
        if (ticking && time>0) {
            time -= Time.deltaTime;
        }
        if (time <= 0) {
            time = cooldown;
            print("ding");
            ticking = false;
            //startTimer();
        }
    }
}
}