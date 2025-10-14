using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extentions 
{
    public static void ClearChildren(this Transform transform, params GameObject[] exclude)
    {
        var count = transform.childCount;
        var excluded = 0;
        while (count > 0)
        {
            var child = transform.GetChild(excluded);
            if(exclude.Contains(child.gameObject))
            {
                excluded++;
                count--;
                continue;
            }
#if UNITY_EDITOR
            if(Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
#else
            Object.Destroy(child.gameObject);
#endif
            count--;
        }
    }

    public static T GetChildComponent<T>(this Transform transform)
    {
        if (transform.childCount == 0) return default;
        
        return transform.GetChild(transform.childCount - 1).TryGetComponent<T>(out var component) ? component : default;
    }
    
    public static List<T> Shuffle<T>(this List<T> deck)
    {
        List<T> cpyDeck = new List<T>(deck);
        List<T> tempDeck = new List<T>();
        
        while (cpyDeck.Count > 0)
        {
            var rndIndex = Random.Range(0, cpyDeck.Count);
            tempDeck.Add(cpyDeck[rndIndex]);
            cpyDeck.RemoveAt(rndIndex);
        }

        return tempDeck;
    }
    
    public static bool IsEmpty(this List<CardSO> deck)
    {
        return deck.Count == 0;
    }

}
