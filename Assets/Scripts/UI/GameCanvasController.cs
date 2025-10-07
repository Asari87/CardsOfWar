using UnityEngine;

public class GameCanvasController : MonoBehaviour
{
    [SerializeField] WinPanelController _winPanelController;
    void Awake()
    {
        _winPanelController.gameObject.SetActive(false);
        WarGameManager.Instance.OnGameEnded += HandleGameEnded;
    }

    void HandleGameEnded(FakeServerManager.RoundState state)
    {
        _winPanelController.Init(state);
        _winPanelController.gameObject.SetActive(true);
    }
}
