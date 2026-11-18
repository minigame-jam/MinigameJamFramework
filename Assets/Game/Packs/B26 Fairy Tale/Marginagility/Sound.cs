using UnityEngine;

namespace Marginagility {
public class Sound : MonoBehaviour
{
    float _duration = 99999f;
    float _timer = 0.0f;
    bool _persistent;

    public void PlaySound(AudioClip sound, bool persistence = false) {
        _duration = sound.length;
        _timer = 0.0f;
        GetComponent<AudioSource>().PlayOneShot(sound);
        _persistent = persistence;
    }

    void Update() {
        if (_timer < _duration ) _timer += Time.deltaTime;
        if (_timer >= _duration && !_persistent) {
            Destroy(this.gameObject);
        }
    }

    public bool isComplete() {
        return _timer >= _duration;
    }
}
}