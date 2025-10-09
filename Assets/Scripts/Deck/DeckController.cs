using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

public class DeckController : MonoBehaviour
{
    [SerializeField] GameArea _gameAreaPrefab;
    [SerializeField] Sprite _cardBack;
    [SerializeField] bool _playAnimations = true;
    
    GameArea _gameArea;
    CardController _cardPrefab;
    
    Dictionary<CardController, CardSO> _activeCards = new();
    ObjectPool<CardController> _cardControllerPool;
    bool _isInDrawingSequence;
    bool _isGameOver;
    int _ongoingWarCount = 0;
    
    public Action<FakeServerManager.RoundState> OnGameEnd;
    
    void Awake()
    {
        _cardPrefab = Resources.Load<CardController>("Prefabs/CardPrefab");
        _cardControllerPool = new(CreateNewCard);
        _gameArea = Instantiate(_gameAreaPrefab);
    }

    CardController CreateNewCard()
    {
        return Instantiate(_cardPrefab);
    }

    public async UniTask Initialize()
    {
        var response = await FakeServerManager.Instance.NewGameRequest();
        if (response is {success: true})
        {
            _isGameOver = false;
        
            _gameArea.ToggleP1DeckVisual(true);
            _gameArea.ToggleP1SideDeckVisual(false);
            _gameArea.ToggleP2DeckVisual(true);
            _gameArea.ToggleP2SideDeckVisual(false);

            Debug.Log($"{GetType()} - Initialized!");   
        }
        else
        {
            Debug.LogError($"{GetType()} - Error starting new game! Check server log for details.");
        }
    }

    void RefreshDeckVisuals(VisualDeckStatus status)
    {
        _gameArea.ToggleP1DeckVisual(status.p1DeckCount > 0);
        _gameArea.ToggleP1SideDeckVisual(status.p1SideDeckCount > 0);
        _gameArea.ToggleP2DeckVisual(status.p2DeckCount > 0);
        _gameArea.ToggleP2SideDeckVisual(status.p2SideDeckCount > 0);
    }

    public void DrawCards()
    {
        if (_isGameOver) return;
        if (_isInDrawingSequence) return;
        _isInDrawingSequence = true;

        Draw().SuppressCancellationThrow().Forget();
    } 

    async UniTask Draw()
    {
        var response = await FakeServerManager.Instance.DrawNextCardRequest();
        foreach (var step in response.steps)
        {
            RefreshDeckVisuals(step.VisualDeckStatus);

            if (step.isGameOver)
            {
                Debug.Log($"Game Over! Status {step.state}");
                _isGameOver = true;
                OnGameEnd?.Invoke(step.state);
                return;
            }
        
            switch (step.state)
            {
                case FakeServerManager.RoundState.P1Win:
                    await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerP1Win();
                    
                    await WinSequence(_gameArea.p1SideDeckPosition);
                    break;
            
                case FakeServerManager.RoundState.P2Win:
                    await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
  
                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerP2Win();
                    
                    await WinSequence(_gameArea.p2SideDeckPosition);
                    break;
            
                case FakeServerManager.RoundState.Tie:
                case FakeServerManager.RoundState.War:
                    await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
  
                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerWar();
                    
                    await InitiateWarSequence();
                    break;
            }
        }
        
        _isInDrawingSequence = false;
    }
    
    async UniTask DrawCardsFromDeck(CardSO p1CardData, CardSO p2CardData, bool isVisible, int sortingOrder = 0, Vector2 offest = default)
    {
        var card = _cardControllerPool.Get();
        _activeCards.Add(card, p1CardData);
        
        var p1Sequence = new CardSequenceBuilder(card, _gameArea.p1DeckPosition, _gameArea.p1PlacementPosition, _cardBack, p1CardData)
            .WithSortingOrder(sortingOrder)
            .WithFacingUp(isVisible)
            .WithOffset(offest)
            .Build();
        
        card = _cardControllerPool.Get();
        _activeCards.Add(card, p2CardData);
        
        var p2Sequence = new CardSequenceBuilder(card, _gameArea.p2DeckPosition, _gameArea.p2PlacementPosition, _cardBack, p2CardData)
            .WithSortingOrder(sortingOrder)
            .WithFacingUp(isVisible)
            .WithOffset(-offest)
            .Build();

        await UniTask.WhenAll(
            p1Sequence.Play(),
            p2Sequence.Play()
            );
    }

    async UniTask WinSequence(Transform winningSide)
    {
        _ongoingWarCount = 0;
        List<UniTask> tasks = new List<UniTask>();

        float durationOffset = 0;
        foreach (var activeCard in _activeCards)
        {
            if(activeCard.Key.transform.parent)
                activeCard.Key.transform.parent.rotation = Quaternion.Euler(Vector3.zero);
            
            durationOffset += 0.075f;
            activeCard.Key.transform.SetParent(null, true);
            var task = activeCard.Key.transform.DOMove(winningSide.position, 0.2f + durationOffset).OnComplete(() =>
            {
                activeCard.Key.ToggleCardVisibility(false);
                activeCard.Key.gameObject.SetActive(false);
                _cardControllerPool.Release(activeCard.Key);
            }).ToUniTask();
            tasks.Add(task);
        }

        
        await UniTask.WhenAll(tasks);
        _activeCards.Clear();
    }

    async UniTask InitiateWarSequence()
    {
        _ongoingWarCount++;
        var response = await FakeServerManager.Instance.DrawNextCardRequest();

        var sortingOrderOverride = _activeCards.Count;
        var stepCount = 0;
        foreach (var step in response.steps)
        {
            stepCount++;
            sortingOrderOverride++;
            
            if (step.isGameOver)
            {
                Debug.Log($"Game Over! Status {step.state}");
                _isGameOver = true;
                OnGameEnd?.Invoke(step.state);
                return;
            }
        
            if(step.ignoreStepCalculation)
            {
                await DrawCardsFromDeck(
                    null, 
                    null, 
                    false, 
                    sortingOrderOverride,
                    new Vector2(1 + 0.3f * stepCount, 0.2f * (_ongoingWarCount - 1))
                    );
                
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

                switch (step.state)
                {
                    case FakeServerManager.RoundState.P1Win:
                        await DrawCardsFromDeck(step.P1Card, step.P2Card, true, stepCount);
                        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
 
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerP1Win();
                        
                        await WinSequence(_gameArea.p1SideDeckPosition);
                        break;

                    case FakeServerManager.RoundState.P2Win:
                        await DrawCardsFromDeck(step.P1Card, step.P2Card, true, stepCount);
                        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
  
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerP2Win();
                        
                        await WinSequence(_gameArea.p2SideDeckPosition);
                        break;

                    case FakeServerManager.RoundState.Tie:
                    case FakeServerManager.RoundState.War:
                        await DrawCardsFromDeck(step.P1Card, step.P2Card, true, stepCount);
                        
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerWar();
                        
                        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                        await InitiateWarSequence();
                        break;
                }
            }
        }
    }
}

public class CardSequenceBuilder
{
    CardController _cardController;
    Transform _startPosition;
    Transform _endPosition;
    
    int _sortingOrder = 0;
    bool _isFacingUp = false;
    bool _selfDestruct = false;
    float _selfDestructDelay = 1;
    float _transitionTime = 0.2f;
    Tween _cardTween;
    Vector3 _offest;
    
    public CardSequenceBuilder(CardController cardInstance, Transform startTransform, Transform endTransform, Sprite cardBack, CardSO cardData = null)
    {
        _cardController = cardInstance;
        _startPosition = startTransform;
        _endPosition = endTransform;
        
        _cardController.Initialize(cardBack, cardData, false);
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
    
    public CardSequenceBuilder WithOffset(float transitionTime)
    {
        _transitionTime = transitionTime;
        return this;
    }

    public CardSequenceBuilder WithOffset(bool selfDestruct, float selfDestructDelay)
    {
        _selfDestruct = selfDestruct;
        _selfDestructDelay = selfDestructDelay;
        return this;
    }



    public CardSequenceBuilder Build()
    {
        _cardController.OverrideSortingOrder(_sortingOrder);
        _cardController.transform.position = _startPosition.position;
        _cardController.gameObject.SetActive(true);

        _cardTween = _cardController.transform
            .DOMove(_endPosition.position + _offest, _transitionTime)
            .OnComplete(async () =>
            {
                if (_isFacingUp)
                {
                    _cardController.transform.SetParent(_endPosition);
                    _cardController.ToggleCardVisibility(_isFacingUp);
                }

                if (_selfDestruct)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_selfDestructDelay));
                    
#if UNITY_EDITOR
                    Object.DestroyImmediate(_cardController.gameObject);
#else
                    Object.Destroy(_startPosition.gameObject);
#endif
                }
            });
        
        return this;
    }

    public async UniTask Play()
    {
        await _cardTween.Play();
    }
}
