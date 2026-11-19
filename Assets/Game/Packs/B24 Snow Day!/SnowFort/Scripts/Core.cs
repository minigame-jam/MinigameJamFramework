using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SnowDay.Snowfort
{
    public class Core : MonoBehaviour
    {
        public bool rightTeam;

        public Color[] healthColors;

        void Start()
        {
            GetComponent<SpriteRenderer>().color = healthColors[healthColors.Length-1];

            GetComponent<Breakable>().onDamage += (h) =>
            { 
                if (h == 0)
                {
                    Debug.Log("Team " + ((rightTeam) ? "Right " : "Left ") + "has lost a core!");
                    FindObjectOfType<GameManager>().CoreDestroyed(rightTeam);
                }
                else
                    GetComponent<SpriteRenderer>().color = healthColors[h - 1];
            };
        }
    }
}