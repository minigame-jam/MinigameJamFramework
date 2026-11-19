using Marginagility;
using UnityEngine;

namespace Marginagility {
public class Lance : MonoBehaviour
{
    [SerializeField] MarginagilityPawn _owner;

    public MarginagilityPawn GetOwner() {
        return _owner;
    }
}
}
