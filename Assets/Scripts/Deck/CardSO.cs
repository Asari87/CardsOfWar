using UnityEngine;

[CreateAssetMenu(fileName = "CardSO", menuName = "ScriptableObjects/CardSO", order = 2)]
public class CardSO : ScriptableObject
{
    public CardData cardData = new();
    public Sprite sprite;
}