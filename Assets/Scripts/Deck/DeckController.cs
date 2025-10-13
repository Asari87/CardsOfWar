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
    
    [Header("Card Win Colors")]
    [SerializeField] Color _rareColor;
    [SerializeField] Color _epicColor;
    [SerializeField] Color _legendaryColor;
    
    GameArea _gameArea;
    CardController _cardPrefab;
    
    Dictionary<CardController, CardSO> _activeCards = new();
    ObjectPool<CardController> _cardControllerPool;
    bool _isInDrawingSequence;
    bool _isGameOver;
    bool _p1DeckEmpty;
    bool _p2DeckEmpty;
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
        
            _gameArea.ToggleP1DeckVisual(response.p1DeckCount);
            _gameArea.ToggleP1SideDeckVisual(0);
            _gameArea.ToggleP2DeckVisual(response.p2DeckCount);
            _gameArea.ToggleP2SideDeckVisual(0);

            Debug.Log($"{GetType()} - Initialized!");   
        }
        else
        {
            Debug.LogError($"{GetType()} - Error starting new game! Check server log for details.");
        }
    }

    async UniTask RefreshDeckVisuals(VisualDeckStatus status, bool refreshDeck, bool refreshSideDeck)
    {
        if (_p1DeckEmpty && status.p1DeckCount > 0)
        {
            var p1Card = GetCardFromPool();
            _gameArea.ToggleP1SideDeckVisual(status.p1SideDeckCount);
            await new CardSequenceBuilder(p1Card, _gameArea.p1SideDeckPosition, _gameArea.p1DeckPosition, _cardBack)
                .WithCallback(() => _cardControllerPool.Release(p1Card))
                .Build()
                .Play();
            _gameArea.ToggleP1DeckVisual(status.p1DeckCount);
        }
        else
        {
            if (refreshDeck)
                _gameArea.ToggleP1DeckVisual(status.p1DeckCount);
            if (refreshSideDeck)
                _gameArea.ToggleP1SideDeckVisual(status.p1SideDeckCount);
        }
        
        if (_p2DeckEmpty && status.p2DeckCount > 0)
        {
            var p2Card = GetCardFromPool();
            _gameArea.ToggleP2SideDeckVisual(status.p2SideDeckCount);
            await new CardSequenceBuilder(p2Card, _gameArea.p2SideDeckPosition, _gameArea.p2DeckPosition, _cardBack)
                .WithCallback(() => _cardControllerPool.Release(p2Card))
                .Build()
                .Play();
            _gameArea.ToggleP2DeckVisual(status.p2DeckCount);
        }
        else
        {
            if (refreshDeck)
                _gameArea.ToggleP2DeckVisual(status.p2DeckCount);
            if (refreshSideDeck)
                _gameArea.ToggleP2SideDeckVisual(status.p2SideDeckCount);
        }

        _p1DeckEmpty = status.p1DeckCount == 0;
        _p2DeckEmpty = status.p2DeckCount == 0;
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
            if (step.isGameOver)
            {
                Debug.Log($"Game Over! Status {step.state}");
                _isGameOver = true;
                OnGameEnd?.Invoke(step.state);
                return;
            }

            await RefreshDeckVisuals(step.VisualDeckStatus, true, false);
            
            switch (step.state)
            {
                case FakeServerManager.RoundState.P1Win:
                    await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(1f));

                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerP1Win();
                    
                    await WinSequence(_gameArea.p1SideDeckPosition);
                    await RefreshDeckVisuals(step.VisualDeckStatus, false, true);
                    break;
            
                case FakeServerManager.RoundState.P2Win:
                    await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(1f));
  
                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerP2Win();
                    
                    await WinSequence(_gameArea.p2SideDeckPosition);
                    await RefreshDeckVisuals(step.VisualDeckStatus,false, true);
                    break;
            
                case FakeServerManager.RoundState.Tie:
                case FakeServerManager.RoundState.War:
                    var cards = await DrawCardsFromDeck(step.P1Card, step.P2Card, true);
                    await UniTask.Delay(TimeSpan.FromSeconds(1f));
  
                    if (_playAnimations)
                        await _gameArea.GameAreaAnimator.TriggerWar();

                    cards.Item1.transform.SetParent(null, true);
                    cards.Item2.transform.SetParent(null, true);
                    
                    await InitiateWarSequence();
                    break;
            }
        }
        
        _isInDrawingSequence = false;
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
                    step.P1Card, 
                    step.P2Card, 
                    false, 
                    sortingOrderOverride,
                    new Vector2(0.6f + 0.5f * stepCount, 0.4f * (_ongoingWarCount - 1))
                    );
                
                await RefreshDeckVisuals(step.VisualDeckStatus,true, false);
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

                switch (step.state)
                {
                    case FakeServerManager.RoundState.P1Win:
                        await DrawCardsFromDeck(step.P1Card, step.P2Card, true, stepCount);
                        await UniTask.Delay(TimeSpan.FromSeconds(1f));
 
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerP1Win();
                        
                        await WinSequence(_gameArea.p1SideDeckPosition);
                        await RefreshDeckVisuals(step.VisualDeckStatus,false, true);
                        break;

                    case FakeServerManager.RoundState.P2Win:
                        await DrawCardsFromDeck(step.P1Card, step.P2Card, true, stepCount);
                        await UniTask.Delay(TimeSpan.FromSeconds(1f));
  
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerP2Win();
                        
                        await WinSequence(_gameArea.p2SideDeckPosition);
                        await RefreshDeckVisuals(step.VisualDeckStatus,false, true);
                        break;

                    case FakeServerManager.RoundState.Tie:
                    case FakeServerManager.RoundState.War:
                        var cards = await DrawCardsFromDeck(
                            step.P1Card, 
                            step.P2Card, 
                            true, 
                            stepCount, 
                            new Vector2(0, 0.4f * _ongoingWarCount)
                            );
                        await UniTask.Delay(TimeSpan.FromSeconds(1f));
                        
                        if (_playAnimations)
                            await _gameArea.GameAreaAnimator.TriggerWar();
                        
                        await UniTask.Delay(TimeSpan.FromSeconds(1f));
                        
                        cards.Item1.transform.SetParent(null, true);
                        cards.Item2.transform.SetParent(null, true);

                        await InitiateWarSequence();
                        break;
                }
            }
        }
    }


    
    async UniTask<(CardController, CardController)> DrawCardsFromDeck(CardSO p1CardData, CardSO p2CardData, bool isVisible, int sortingOrder = 0, Vector2 offest = default)
    {
        var p1Card = GetCardFromPool();
        _activeCards.Add(p1Card, p1CardData);
        
        var p1Sequence = new CardSequenceBuilder(p1Card, _gameArea.p1DeckPosition, _gameArea.p1PlacementPosition, _cardBack, p1CardData)
            .WithSortingOrder(sortingOrder)
            .WithFacingUp(isVisible)
            .WithOffset(offest)
            .Build();
        
        var p2Card = GetCardFromPool();
        _activeCards.Add(p2Card, p2CardData);
        
        var p2Sequence = new CardSequenceBuilder(p2Card, _gameArea.p2DeckPosition, _gameArea.p2PlacementPosition, _cardBack, p2CardData)
            .WithSortingOrder(sortingOrder)
            .WithFacingUp(isVisible)
            .WithOffset(-offest)
            .Build();

        await UniTask.WhenAll(
            p1Sequence.Play(),
            p2Sequence.Play()
            );
        
        return (p1Card, p2Card);
    }

    async UniTask WinSequence(Transform winningSide)
    {
        _ongoingWarCount = 0;
        List<UniTask> tasks = new List<UniTask>();

        if(_activeCards.Count > 2)
        {
            List<CardController> warLootCards = new List<CardController>();
            if (_activeCards.Keys.Any(c => !c.IsFacingUp))
            {
                foreach (var card in _activeCards)
                {
                    if (card.Key.IsFacingUp) continue;
                    card.Key.ToggleCardVisibility(true);
                    warLootCards.Add(card.Key);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            foreach (var card in warLootCards)
            {
                if (card.CardValue > 10)
                {
                    if (card.CardValue < 14)
                        card.ShowBorder(_rareColor, 3f);
                    else
                        card.ShowBorder(card.CardValue > 14 ? _legendaryColor : _epicColor, 3f);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(3f));
        }
        
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

    
    CardController GetCardFromPool()
    {
        var card = _cardControllerPool.Get();
        card.transform.localScale = Vector3.one;
        card.transform.rotation = Quaternion.Euler(Vector3.zero);
        return card;
    }
}