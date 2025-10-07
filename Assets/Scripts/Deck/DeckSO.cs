using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DeckSO", menuName = "ScriptableObjects/DeckSO", order = 1)]
public class DeckSO : ScriptableObject
{
    public List<CardSO> cards;
    public Sprite back;
}
