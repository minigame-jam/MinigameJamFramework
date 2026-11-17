using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions {
    /// <summary>
    /// Shuffles the elements of a list in-place using the Fisher-Yates algorithm.
    /// </summary>
    public static void Shuffle<T>(this List<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            // Pick a random index from 0 to i
            int randomIndex = Random.Range(0, i + 1);

            // Swap elements
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}