using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeveloperComment : MonoBehaviour {
    [TextArea(3,10)]
    [SerializeField] private string comment;
}
