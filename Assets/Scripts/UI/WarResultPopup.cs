using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarResultPopup : MonoBehaviour
{
    [Header("Player Cards")]
    [SerializeField] Transform _p1CardParent;
    [SerializeField] Transform _p2CardParent;
    [SerializeField] CardUiController _cardUiController;

    [Header("Popup Refs")]
    [SerializeField] Animator _animator;
    [SerializeField] TMP_Text _warResultText;
    [SerializeField] Image _warResultImage;
    [SerializeField] Button _closeButton;
    
    [Header("Asset Refs")]
    [SerializeField] Sprite _warWonSprite;
    [SerializeField] Sprite _warLostSprite; 
    
    bool _popupInitComplete = false;
    
    void Awake()
    {
        _p1CardParent.ClearChildren();
        _p2CardParent.ClearChildren();
        transform.localScale = Vector3.zero;
        
        _closeButton.onClick.AddListener(HandleCloseButton);
    }

    void OnDestroy()
    {
        _closeButton.onClick.RemoveListener(HandleCloseButton);
    }

    void HandleCloseButton()
    {
        if (!_popupInitComplete) return;
        transform.DOScale(Vector3.zero, 0.5f)
            .OnComplete(() => Destroy(gameObject));
    }

    public void Init(bool warWon, DeckSO deckSettings, List<CardSO> p1Cards, List<CardSO> p2Cards)
    {
        _popupInitComplete = false;
        _warResultText.text = warWon ? "War won!" : "War lost!";
        _warResultImage.sprite = warWon ? _warWonSprite : _warLostSprite;

        foreach (CardSO card in p1Cards)
        {
            var cardUi = Instantiate(_cardUiController, _p1CardParent);
            var backgroundColor = deckSettings.GetCardEffectColor(card);
            cardUi.Init(backgroundColor, card.sprite);
        }
        
        foreach (CardSO card in p2Cards)
        {
            var cardUi = Instantiate(_cardUiController, _p2CardParent);
            var backgroundColor = deckSettings.GetCardEffectColor(card);
            cardUi.Init(backgroundColor, card.sprite);
        }

        transform.DOScale(Vector3.one, 0.5f)
            .OnComplete(() => _popupInitComplete = true);
    }
}
