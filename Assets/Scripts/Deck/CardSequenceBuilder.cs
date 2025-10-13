using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CardSequenceBuilder
{
    CardController _cardController;
    Transform _startPosition;
    Transform _endPosition;
    
    int _sortingOrder = 0;
    bool _isFacingUp = false;
    float _transitionTime = 0.2f;
    Tween _cardTween;
    Vector3 _offest;
    Action _callback;
    
    public CardSequenceBuilder(CardController cardInstance, Transform startTransform, Transform endTransform, Sprite cardBack, CardSO cardData = null)
    {
        _cardController = cardInstance;
        _startPosition = startTransform;
        _endPosition = endTransform;
        
        _cardController.SetCardGraphics(cardBack, cardData?.sprite);
        _cardController.Initialize(cardData, false);
    }

    public CardSequenceBuilder WithSortingOrder(int sortingOrder)
    {
        _sortingOrder = sortingOrder;
        return this;
    }
    
    public CardSequenceBuilder WithFacingUp(bool isFacingUp)
    {
        _isFacingUp = isFacingUp;
        return this;
    }
    
    public CardSequenceBuilder WithOffset(Vector3 offset)
    {
        _offest = offset;
        return this;
    }

    public CardSequenceBuilder WithCallback(Action callback)
    {
        _callback = callback;
        return this;
    }
    
    public CardSequenceBuilder Build()
    {
        _cardController.OverrideSortingOrder(_sortingOrder);
        _cardController.transform.position = _startPosition.position;
        _cardController.gameObject.SetActive(true);

        _cardTween = _cardController.transform
            .DOMove(_endPosition.position + _offest, _transitionTime)
            .OnComplete(() =>
            {
                if (_isFacingUp)
                {
                    _cardController.transform.SetParent(_endPosition);
                    _cardController.ToggleCardVisibility(_isFacingUp);
                }

                _callback?.Invoke();
            });
        
        return this;
    }

    public async UniTask Play()
    {
        await _cardTween.Play();
    }
}