using UnityEngine;
using TMPro;

namespace GnomeSpace{
    public class GameTimer : MonoBehaviour
    {
        // TextMeshPro textBox;
        // // Start is called once before the first execution of Update after the MonoBehaviour is created
        // void Start()
        // {
        //     textBox = ;
        // }

        // Update is called once per frame
        void Update()
        {
            GetComponent<TextMeshProUGUI>().text =((int)( GnomeGameManager.instance.getTime())).ToString();
           
        }
    }
}