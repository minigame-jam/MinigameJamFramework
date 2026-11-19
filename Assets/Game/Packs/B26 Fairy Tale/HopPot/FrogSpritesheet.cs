using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Frog Spritesheet", menuName ="Hop Pot/Create Frog Spritesheet")]
public class FrogSpritesheet : ScriptableObject {
    public Sprite idle;
    public Sprite jump;
    public Sprite back;
    public List<Sprite> captured;

}
