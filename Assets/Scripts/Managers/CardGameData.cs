using System.Collections.Generic;
using System.Linq;

public class CardGameData
{
    public enum GameState { Running, War, Ended }
    public GameState gameState = GameState.Running;
    
    public Player p1 = new();
    public Player p2 = new();
    public List<CardData> activeCards = new();
        
    public class Player
    {
        public List<CardData> deck = new();
        public List<CardData> reserveDeck = new();

        public CardData DrawCard()
        {
            // check main deck
            if (deck.Count > 0)
            {
                var topCard = deck.First();
                deck.Remove(topCard);
                return topCard;
            }

            // check reserve deck
            if (reserveDeck.Count > 0)
            {
                deck.AddRange(reserveDeck.Shuffle());
                reserveDeck.Clear();
                
                var topCard = deck.First();
                deck.Remove(topCard);
                return topCard;
            }

            // out of cards
            return null;
        }
    }

}