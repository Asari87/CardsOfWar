using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FakeServerManager : GenericSingelton<FakeServerManager>
{
    [SerializeField] DeckSO _deck;
    
    [Header("Debug")]
    [SerializeField] int _pingDelayMs;
    public enum RoundState { P1Win, P2Win, War, Tie }
    public enum GameState { Running, War, Ended }
    
    GameState _gameState;
    
    Player p1;
    Player p2;
    CardSO _p1TopCard;
    CardSO _p2TopCard;
    List<CardSO> _activeCards;

    bool _isGameRunning;

    void Start()
    {
        _isGameRunning = false;
    }
    
    public async UniTask<DrawCardResponse> DrawNextCardRequest()
    {
        await UniTask.Delay(_pingDelayMs);
        return _gameState is GameState.War ? await HandleWarState() : await HandleRunningState();
    }

    UniTask<DrawCardResponse> HandleRunningState()
    {
        var stepData = CreateGameStep();
        var response = new DrawCardResponse();
        response.steps.Add(stepData);
        return UniTask.FromResult(response);
    }

    GameStepData CreateGameStep()
    {
        var stepData = new GameStepData();
        
        var p1Card = p1.DrawCard();
        var p2Card = p2.DrawCard();

        Debug.Log($"{GetType()} - Drawing: P1 ({p1Card?.suit}_{p1Card?.rank}) P2 ({p2Card?.suit}_{p2Card?.rank})");
        
        // game end checks
        if(!p1Card || !p2Card)
        {
            _gameState = GameState.Ended;
            stepData.isGameOver = true;

            if (!p1Card && !p2Card)
            {
                stepData.state = RoundState.Tie;
            }
            else if (!p1Card)
            {
                stepData.state = RoundState.P2Win;
            }
            else if (!p2Card)
            {
                stepData.state = RoundState.P1Win;
            }
        }
        else
        {
            if (p1Card.value == p2Card.value)
            {
                stepData.state = RoundState.War;
                _gameState = GameState.War;
                _activeCards.Add(p1Card);
                _activeCards.Add(p2Card);
            }
            else if (p1Card.value > p2Card.value)
            {
                stepData.state = RoundState.P1Win;
                _gameState = GameState.Running;
                p1.sideDeck.Add(p1Card);
                p1.sideDeck.Add(p2Card);
                if(_activeCards.Count > 0)
                {
                    p1.sideDeck.AddRange(_activeCards);
                    _activeCards.Clear();
                }
            }
            else
            {
                stepData.state = RoundState.P2Win;
                _gameState = GameState.Running;
                p2.sideDeck.Add(p1Card);
                p2.sideDeck.Add(p2Card);
                if(_activeCards.Count > 0)
                {
                    p2.sideDeck.AddRange(_activeCards);
                    _activeCards.Clear();
                }
            }
        }
        
        stepData.P1Card = p1Card;
        stepData.P2Card = p2Card;
        stepData.VisualDeckStatus = new VisualDeckStatus()
        {
            p1DeckCount = p1.deck.Count,
            p2DeckCount = p2.deck.Count,
            p1SideDeckCount = p1.sideDeck.Count,
            p2SideDeckCount = p2.sideDeck.Count
        };
        return stepData;
    }

    UniTask<DrawCardResponse> HandleWarState()
    {
        var response = new DrawCardResponse();
        
        Debug.Log($"{GetType()} - Starting war sequence");
        
        for (int i = 0; i < 3; i++)
        {
            var stepData = CreateGameStep();
            stepData.ignoreStepCalculation = true;
            response.steps.Add(stepData);

            if (stepData.isGameOver)
            {
                //game ends here
                Debug.Log($"{GetType()} - Game ended during war sequence");
                return UniTask.FromResult(response);
            }
            
            _activeCards.Add(stepData.P1Card);
            _activeCards.Add(stepData.P2Card);
        }

        Debug.Log($"{GetType()} - Drawing final cards in war sequence");
        var finalStepData = CreateGameStep();
        response.steps.Add(finalStepData);
        return UniTask.FromResult(response);
    }

    public UniTask<NewGameResponse> NewGameRequest()
    {
        Debug.Log($"{GetType()} - New game started");
        
        p1 = new Player();
        p2 = new Player();
        _activeCards = new List<CardSO>();
        _gameState = GameState.Running;
        ShuffleDeck(_deck.cards);
        
        return UniTask.FromResult(new NewGameResponse()
        {
            success = true
        });
    }
    
    void ShuffleDeck(List<CardSO> cards)
    {
        var shuffledDeck = cards.Shuffle();
        
        p1.deck.AddRange(shuffledDeck.Where(c => shuffledDeck.IndexOf(c) % 2 == 0));
        p2.deck.AddRange(shuffledDeck.Where(c => shuffledDeck.IndexOf(c) % 2 != 0));
    }
}

/// <summary>
/// On a larger scale, server should send additional identifier for the game's instance
/// </summary>
public class NewGameResponse
{
    public bool success;
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
    public CardSO P1Card;
    public CardSO P2Card;
    public VisualDeckStatus VisualDeckStatus;
}

public class VisualDeckStatus
{
    public int p1DeckCount;
    public int p1SideDeckCount;
    public int p2DeckCount;
    public int p2SideDeckCount;
}

public class Player
{
    public List<CardSO> deck = new();
    public List<CardSO> sideDeck = new();

    public CardSO DrawCard()
    {
        // check main deck
        if (deck.Count > 0)
        {
            var topCard = deck.First();
            deck.Remove(topCard);
            return topCard;
        }

        // check reserve deck
        if (sideDeck.Count > 0)
        {
            deck.AddRange(sideDeck.Shuffle());
            sideDeck.Clear();
            return DrawCard();
        }

        // out of cards
        return null;
    }
}
