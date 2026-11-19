using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Witch Spritesheet", menuName ="Hop Pot/Create Witch Spritesheet")]
public class WitchSpritesheet : ScriptableObject {
    [Header("Basic")]
    public Sprite idle;
    public List<Sprite> walk;
    public List<Sprite> backWalk;
    [Header("Grab")]
    public Sprite grabIdle;
    public List<Sprite> grabWalk;
    public List<Sprite> grabBackWalk;
}
