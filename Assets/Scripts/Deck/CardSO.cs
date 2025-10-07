using UnityEngine;

[CreateAssetMenu(fileName = "CardSO", menuName = "ScriptableObjects/CardSO", order = 2)]
public class CardSO : ScriptableObject
{
    public enum CardSuit { Diamond, Heart, Spade, Club, Joker }
    public Sprite sprite;
    public CardSuit suit;
    public string rank;
    public int value;
}
