using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

public class DeckController : MonoBehaviour
{
    [SerializeField] GameArea _gameAreaPrefab;
    [SerializeField] Sprite _cardBack;
    [SerializeField] bool _playAnimations = true;
    
    GameArea _gameArea;
    CardController _cardPrefab;
    
    Dictionary<CardSO, CardController> _activeCards = new();
    ObjectPool<CardController> _cardControllerPool;
    bool _isInDrawingSequence;
    bool _isGameOver;
    
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
    
    async UniTask DrawCardsFromDeck(CardSO p1CardData, CardSO p2CardData, bool isVisible, int sortingOrder = 0)
    {
        var p1Card = _cardControllerPool.Get();
        p1Card.OverrideSortingOrder(sortingOrder);
        p1Card.transform.position = _gameArea.p1DeckPosition.position;
        p1Card.gameObject.SetActive(true);
        if(p1CardData)
        {
            p1Card.Initialize(_cardBack, p1CardData, false);
            _activeCards.Add(p1CardData, p1Card);
        }
        else
        {
            p1Card.Initialize(_cardBack, null, false);
        }
                    
        var p2Card = _cardControllerPool.Get();
        p2Card.OverrideSortingOrder(sortingOrder);
        p2Card.transform.position = _gameArea.p2DeckPosition.position;
        p2Card.gameObject.SetActive(true);
        if(p2CardData)
        {
            p2Card.Initialize(_cardBack, p2CardData, false);
            _activeCards.Add(p2CardData, p2Card);
        }
        else
        {
            p2Card.Initialize(_cardBack, null, false);
        }
        
        await UniTask.WhenAll(
            p1Card.transform
                .DOMove(_gameArea.p1PlacementPosition.position, 0.2f)
                .OnComplete(async () =>
                {
                    if (p1CardData)
                    {
                        p1Card.transform.SetParent(_gameArea.p1PlacementPosition);
                        p1Card.ToggleCardVisibility(isVisible);
                        return;
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(1f));
                    Destroy(p1Card.gameObject);
                }).ToUniTask(),
            
            p2Card.transform
                .DOMove(_gameArea.p2PlacementPosition.position, 0.2f)
                .OnComplete(async () =>
                {
                    if (p2CardData)
                    {
                        p2Card.transform.SetParent(_gameArea.p2PlacementPosition);
                        p2Card.ToggleCardVisibility(isVisible);
                        return;
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(1f));
                    Destroy(p2Card.gameObject);
                }).ToUniTask()
        );
    }

    async UniTask WinSequence(Transform winningSide)
    {
        List<UniTask> tasks = new List<UniTask>();

        float durationOffset = 0;
        foreach (var activeCard in _activeCards)
        {
            durationOffset += 0.075f;
            activeCard.Value.transform.parent.rotation = Quaternion.Euler(Vector3.zero);
            activeCard.Value.transform.SetParent(null, true);
            var task = activeCard.Value.transform.DOMove(winningSide.position, 0.2f + durationOffset).OnComplete(() =>
            {
                activeCard.Value.ToggleCardVisibility(false);
                activeCard.Value.gameObject.SetActive(false);
                _cardControllerPool.Release(activeCard.Value);
            }).ToUniTask();
            tasks.Add(task);
        }

        await UniTask.WhenAll(tasks);
        _activeCards.Clear();
    }

    async UniTask InitiateWarSequence()
    {
        var response = await FakeServerManager.Instance.DrawNextCardRequest();

        var stepCount = _activeCards.Count;
        foreach (var step in response.steps)
        {
            stepCount++;
            if (step.isGameOver)
            {
                Debug.Log($"Game Over! Status {step.state}");
                _isGameOver = true;
                OnGameEnd?.Invoke(step.state);
                return;
            }
        
            if(step.ignoreStepCalculation)
            {
                await DrawCardsFromDeck(null, null, false, stepCount);
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
