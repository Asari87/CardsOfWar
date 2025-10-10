using System;
using UnityEngine;

public class WarGameManager : GenericSingelton<WarGameManager>
{
    [SerializeField] DeckController _deckControllerPrefab;

    DeckController _deckController;

    public Action<FakeServerManager.RoundState> OnGameEnded;
    
    void Start()
    {
        _deckController = Instantiate(_deckControllerPrefab);
        _deckController.OnGameEnd += HandleGameEnded;
        _deckController.Initialize();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if(_deckController)
            _deckController.OnGameEnd -= HandleGameEnded;
    }

    void HandleGameEnded(FakeServerManager.RoundState state)
    {
        OnGameEnded?.Invoke(state);
    }

    void Update()
    {
        var hasTouchInput = false;
        
        if (Input.touchCount > 0)
        {
            hasTouchInput = Input.GetTouch(0).phase == TouchPhase.Began;
        }
        
        if (Input.GetKeyDown(KeyCode.Space) || hasTouchInput)
        {
            _deckController.DrawCards();
        }
    }
}
