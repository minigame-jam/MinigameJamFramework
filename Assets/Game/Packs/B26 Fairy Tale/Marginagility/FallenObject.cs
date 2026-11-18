using System;
using UnityEngine;

namespace Marginagility {
public class FallenObject : MonoBehaviour
{
    [SerializeField] SpriteRenderer _sprite;
    float _time;

    void Start() {
        _time = 0.0f;
    }

    void Update()
    {
        _time += Time.deltaTime;
        if (_time >= 1.5f) Destroy(this.gameObject);
    }

    public SpriteRenderer GetSprite() {
        return _sprite;
    }
}
}
