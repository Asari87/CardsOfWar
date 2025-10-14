using UnityEngine;
using UnityEngine.Rendering;

public class CardController : MonoBehaviour
{
    [SerializeField] SpriteRenderer _frontSide;
    [SerializeField] SpriteRenderer _backSide;
    [SerializeField] SortingGroup  _sortingGroup;
    [SerializeField] CardEffectController  _effectController;
    [SerializeField] Animator _animator;
    
    static readonly int FacingUp = Animator.StringToHash("IsFacingUp");
    
    CardSO _cardData;

    public bool IsFacingUp => _frontSide?.gameObject.activeInHierarchy ?? false;
    public int CardValue => _cardData?.cardData.value ?? -1;

    public void Initialize(CardSO cardData, bool isVisible)
    {
        ToggleCardVisibility(isVisible);
        
        if (!cardData) return;
        _cardData = cardData;
    }

    public void SetCardGraphics(Sprite backSide, Sprite frontSide = null)
    {
        _frontSide.sprite = frontSide;
        _backSide.sprite = backSide;
    }
 
    public void ToggleCardVisibility(bool visible)
    {
        _animator?.SetBool(FacingUp, visible);
        // _frontSide.gameObject.SetActive(visible);
        // _backSide.gameObject.SetActive(!visible);
    }

    public void OverrideSortingOrder(int order)
    {
        _sortingGroup.sortingOrder = order;
    }
    
    public void ShowBorder(Color color, float duration = 0.5f)
    {
        _effectController.ShowBorder(color, duration);
    }
}
