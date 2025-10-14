using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// DeckSO represents full deck with sprite and effect colors
/// </summary>
/// 
[CreateAssetMenu(fileName = "DeckSO", menuName = "ScriptableObjects/DeckSO", order = 1)]
public class DeckSO : ScriptableObject
{
    [Header("Deck Properties")]
    public List<CardSO> cards;
    public Sprite back;
    
    [Header("Card Win Colors")]
    public Color _rareColor;
    public Color _epicColor;
    public Color _legendaryColor;

    public CardSO GetCardFromData(CardData data)
    {
        return cards.SingleOrDefault(c => c.cardData.rank.Equals(data.rank) && c.cardData.suit.Equals(data.suit));    
    }

    public Color GetCardEffectColor(CardSO card)
    {
        var cardValue = card.cardData.value;
        return GetCardEffectColor(cardValue);
    }
    
    public Color GetCardEffectColor(int cardValue)
    {
        switch (cardValue)
        {
            case > 10 and < 14:
                return _rareColor;
            case 14:
                return _epicColor;
            case >14:
                return _legendaryColor;
            default:
                return Color.white;
        }
    }
}
