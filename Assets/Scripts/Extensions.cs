using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    // Fisher-Yates Shuffle Algorithm
    // uses UnityEngine.Random
    // public static void Shuffle<T>(this IList<T> list)
    // {
    //     int n = list.Count;
    //     while (n > 1)
    //     {
    //         n--;
    //         int k = Random.Range(0, n + 1);
            
    //         // Swap elements
    //         T value = list[k];
    //         list[k] = list[n];
    //         list[n] = value;
    //     }
    // }
    // uses System.Random
    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}