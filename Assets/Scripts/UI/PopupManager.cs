using System.Collections.Generic;
using UnityEngine;

public class PopupManager : GenericSingelton<PopupManager>
{
    [SerializeField] WarResultPopup _warResultPopupPrefab;

    public WarResultPopup ShowWarResultPopup(bool warWon, DeckSO deckSettings, List<CardSO> p1Cards, List<CardSO> p2Cards)
    {
        var warResultPopup = Instantiate(_warResultPopupPrefab, transform);
        warResultPopup.Init(warWon, deckSettings, p1Cards, p2Cards);
        return warResultPopup;
    }
    
}
