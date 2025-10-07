using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinPanelController : MonoBehaviour
{
    [SerializeField] TMP_Text _playerWonText;
    [SerializeField] Button _playAgainButton;

    const string PLAYER_WON_TEXT = "Player {0} Won!";

    void Awake()
    {
        _playAgainButton.onClick.AddListener(HandlePlayAgain);
    }

    void OnDestroy()
    {
        _playAgainButton.onClick.RemoveListener(HandlePlayAgain);
    }

    void HandlePlayAgain()
    {
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }

    public void Init(FakeServerManager.RoundState state)
    {
        _playerWonText.text = string.Empty;
        switch (state)
        {
            case FakeServerManager.RoundState.P1Win:
                _playerWonText.text = string.Format(PLAYER_WON_TEXT, "1");
                break;
            
            case FakeServerManager.RoundState.P2Win:
                _playerWonText.text = string.Format(PLAYER_WON_TEXT, "2");
                break;
            
            case FakeServerManager.RoundState.War:
            case FakeServerManager.RoundState.Tie:
                _playerWonText.text = "Tie!";
                break;
        }
    }

}
