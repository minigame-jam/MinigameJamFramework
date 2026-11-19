using UnityEngine;

using TMPro;

using Game.MinigameFramework.Scripts.Framework.PlayerInfo;

namespace HopPot {
    public class TimerText : MonoBehaviour
    {
        [SerializeField] GameManager gm;
        [SerializeField] public TMP_Text timerText;
        



        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            timerText.text = (60 - gm.getTime()).ToString();
            
        }
        
    }
}