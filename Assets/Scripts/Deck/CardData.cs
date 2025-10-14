
using System;

[Serializable]
public class CardData
{
    public enum CardSuit { Diamond, Heart, Spade, Club, Joker }
    public CardSuit suit;
    public string rank;
    public int value;
}