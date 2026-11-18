using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Marginagility;

namespace Marginagility {
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField]TextMeshProUGUI[] _scoreDisps;
    [SerializeField]TextMeshProUGUI[] _respawnDisps;

    [SerializeField]TextMeshProUGUI _timer;

    List<int> _scores;

    void Awake() {
        _scores = new List<int>();

        for (int i = 0; i < 4; i++) {
            _scores.Add(-999999);
            SetScore(i, -999999, false);
            SetRespawn(i, -1);
        }
    }

    public void SetScore(int player, int score, bool enabled) {
        if (player == -1) return;
        _scores[player] = score;
        _scoreDisps[player].text = _scores[player].ToString();
        _scoreDisps[player].enabled = enabled;
    }

    public void SetRespawn(int player, float time) {
        if (player == -1) return;
        int dispTime = Mathf.CeilToInt(time);
        _respawnDisps[player].text = ("Spawn in " + dispTime.ToString());
        _respawnDisps[player].enabled = (dispTime > 0);
    }

    public void AddToScore(int player, int score) {
        if (player == -1) return;
        _scores[player] += score;
        _scoreDisps[player].text = _scores[player].ToString();
        _scoreDisps[player].enabled = true;
    }

    public void UpdateTimer(float time) {
        int dispTime = Mathf.CeilToInt(time);
        _timer.text = dispTime.ToString();
    }

    public List<int> GetScores() {
        return _scores;
    }
}
}