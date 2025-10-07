using UnityEngine;
using UnityEngine.Rendering;

public class CardController : MonoBehaviour
{
    [SerializeField] SpriteRenderer _frontSide;
    [SerializeField] SpriteRenderer _backSide;
    [SerializeField] SortingGroup  _sortingGroup;
    
    CardSO _cardData;
    
    public int CardValue => _cardData.value;

    public void Initialize(Sprite backSide, CardSO cardData, bool isVisible)
    {
        _backSide.sprite = backSide;
        ToggleCardVisibility(isVisible);
        
        if (!cardData) return;
        
        _cardData = cardData;
        _frontSide.sprite = _cardData.sprite;
    }

    public void ToggleCardVisibility(bool visible)
    {
        _frontSide.gameObject.SetActive(visible);
        _backSide.gameObject.SetActive(!visible);
    }

    public void OverrideSortingOrder(int order)
    {
        _sortingGroup.sortingOrder = order;
    }
}
