using System;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using TMPro;
using UnityEngine;

/// <summary>
/// Player Number UI that automatically updates color and text corresponding to player index. 
/// </summary>
[ExecuteAlways]
public class PlayerIndicator : MonoBehaviour {
    private TextMeshProUGUI text;
    
    [SerializeProperty("_PlayerIndex")]
    [SerializeField] private int index = -1;
    private int _PlayerIndex {
        get=>index;
        set {
            index = Math.Clamp(value, 0, 3);
            text.text = $"P{index + 1}";
            text.color = colors[index];
        }
    }
    [SerializeField] List<Color> colors = new() { };

    void Awake() {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }
    void Start(){
        if(index>-1) return;
        Pawn pawn = GetComponentInParent<Pawn>();
        if(pawn!=null) {
            if(pawn.playerIndex != -1) _PlayerIndex = pawn.playerIndex;
            else pawn.onPlayerIndexChanged.AddListener((value) => {_PlayerIndex = value;});
        }
    }
}