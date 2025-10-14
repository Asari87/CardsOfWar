using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FakeServerManager : GenericSingelton<FakeServerManager>
{
    [SerializeField] ServerDeckSO _deck;
    
    [Header("Debug")]
    [SerializeField] int _pingDelayMs;
    public enum RoundState { P1Win, P2Win, War, Tie }

    CardGameData _activeGameData;
    
    public async UniTask<DrawCardResponse> DrawNextCardRequest()
    {
        await UniTask.Delay(_pingDelayMs);
        return _activeGameData.gameState is CardGameData.GameState.War ? await HandleWarState() : await HandleRunningState();
    }

    UniTask<DrawCardResponse> HandleRunningState()
    {
        var stepData = CreateGameStep();
        var response = new DrawCardResponse();
        response.steps.Add(stepData);
        return UniTask.FromResult(response);
    }

    GameStepData CreateGameStep(bool calculateResult = true)
    {
        var stepData = new GameStepData();
        
        var p1Card = _activeGameData.p1.DrawCard();
        var p2Card = _activeGameData.p2.DrawCard();

        Debug.Log($"{GetType()} - Drawing: P1 ({p1Card?.suit}_{p1Card?.rank}) P2 ({p2Card?.suit}_{p2Card?.rank})");
        
        // game end checks
        if(p1Card == null || p2Card == null)
        {
            _activeGameData.gameState = CardGameData.GameState.Ended;
            stepData.isGameOver = true;

            switch (p1Card)
            {
                case null when p2Card == null:
                    stepData.state = RoundState.Tie;
                    break;
                case null:
                    stepData.state = RoundState.P2Win;
                    break;
                default:
                {
                    stepData.state = RoundState.P1Win;
                    break;
                }
            }
        }
        else if (calculateResult)
        {
            if (p1Card.value == p2Card.value)
            {
                stepData.state = RoundState.War;
                _activeGameData.gameState = CardGameData.GameState.War;
                _activeGameData.activeCards.Add(p1Card);
                _activeGameData.activeCards.Add(p2Card);
            }
            else if (p1Card.value > p2Card.value)
            {
                stepData.state = RoundState.P1Win;
                _activeGameData.gameState = CardGameData.GameState.Running;
                _activeGameData.p1.reserveDeck.Add(p1Card);
                _activeGameData.p1.reserveDeck.Add(p2Card);
                if(_activeGameData.activeCards.Count > 0)
                {
                    _activeGameData.p1.reserveDeck.AddRange(_activeGameData.activeCards);
                    _activeGameData.activeCards.Clear();
                }
            }
            else
            {
                stepData.state = RoundState.P2Win;
                _activeGameData.gameState = CardGameData.GameState.Running;
                _activeGameData.p2.reserveDeck.Add(p1Card);
                _activeGameData.p2.reserveDeck.Add(p2Card);
                if(_activeGameData.activeCards.Count > 0)
                {
                    _activeGameData.p2.reserveDeck.AddRange(_activeGameData.activeCards);
                    _activeGameData.activeCards.Clear();
                }
            }
        }
        else
        {
            _activeGameData.gameState = CardGameData.GameState.War;
            _activeGameData.activeCards.Add(p1Card);
            _activeGameData.activeCards.Add(p2Card);
        }
        
        stepData.P1Card = p1Card;
        stepData.P2Card = p2Card;
        stepData.VisualDeckStatus = new VisualDeckStatus()
        {
            p1DeckCount = _activeGameData.p1.deck.Count,
            p2DeckCount = _activeGameData.p2.deck.Count,
            p1SideDeckCount = _activeGameData.p1.reserveDeck.Count,
            p2SideDeckCount = _activeGameData.p2.reserveDeck.Count
        };
        return stepData;
    }

    UniTask<DrawCardResponse> HandleWarState()
    {
        var response = new DrawCardResponse();
        
        Debug.Log($"{GetType()} - Starting war sequence");
        
        for (int i = 0; i < 3; i++)
        {
            var stepData = CreateGameStep(false);
            stepData.ignoreStepCalculation = true;
            response.steps.Add(stepData);

            if (stepData.isGameOver)
            {
                //game ends here
                Debug.Log($"{GetType()} - Game ended during war sequence");
                return UniTask.FromResult(response);
            }
        }

        Debug.Log($"{GetType()} - Drawing final cards in war sequence");
        var finalStepData = CreateGameStep();
        response.steps.Add(finalStepData);
        return UniTask.FromResult(response);
    }

    public UniTask<NewGameResponse> NewGameRequest()
    {
        Debug.Log($"{GetType()} - New game started");
        
        _activeGameData = new CardGameData();
        ShuffleDeck(_deck.cards);
        
        return UniTask.FromResult(new NewGameResponse()
        {
            success = true,
            p1DeckCount = _activeGameData.p1.deck.Count,
            p2DeckCount = _activeGameData.p2.deck.Count,
        });
    }
    
    void ShuffleDeck(List<CardData> cards)
    {
        var shuffledDeck = cards.Shuffle();
        
        _activeGameData.p1.deck.AddRange(shuffledDeck.Where(c => shuffledDeck.IndexOf(c) % 2 == 0));
        _activeGameData.p2.deck.AddRange(shuffledDeck.Where(c => shuffledDeck.IndexOf(c) % 2 != 0));
    }
}

/// <summary>
/// On a larger scale, server should send additional identifier for the game's instance
/// </summary>
public class NewGameResponse
{
    public bool success;
    public int p1DeckCount;
    public int p2DeckCount;
}

public class DrawCardResponse
{
    public List<GameStepData> steps = new();
}

public class GameStepData
{
    public bool isGameOver;
    public bool ignoreStepCalculation;
    public FakeServerManager.RoundState state;
    public CardData P1Card;
    public CardData P2Card;
    public VisualDeckStatus VisualDeckStatus;
}

public class VisualDeckStatus
{
    public int p1DeckCount;
    public int p1SideDeckCount;
    public int p2DeckCount;
    public int p2SideDeckCount;
}

