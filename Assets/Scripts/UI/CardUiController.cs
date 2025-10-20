using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardUiController : MonoBehaviour
{
    [SerializeField] Image _cardBackgroundImage;
    [SerializeField] Image _cardImage;

    public void Init(Color cardBackground, Sprite cardSprite)
    {
        _cardBackgroundImage.color = cardBackground;
        _cardImage.sprite = cardSprite;
    }
}
