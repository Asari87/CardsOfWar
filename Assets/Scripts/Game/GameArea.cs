using System;
using TMPro;
using UnityEngine;

public class GameArea : MonoBehaviour
{
    [SerializeField] BoxCollider2D _collider2D;
    [SerializeField] GameAreaAnimationHandler _animationHandler;
    
    [Header("Player 1 References")]
    public Transform p1DeckPosition;
    public Transform p1SideDeckPosition;
    public Transform p1PlacementPosition;
    [SerializeField] GameObject p1DeckPlaceholder;
    [SerializeField] GameObject p1SideDeckPlaceholder;
    [SerializeField] TMP_Text p1DeckCount;
    [SerializeField] TMP_Text p1SideDeckCount;
    
    [Header("Player 2 References")]
    public Transform p2DeckPosition;
    public Transform p2SideDeckPosition;
    public Transform p2PlacementPosition;
    [SerializeField] GameObject p2DeckPlaceholder;
    [SerializeField] GameObject p2SideDeckPlaceholder;
    [SerializeField] TMP_Text p2DeckCount;
    [SerializeField] TMP_Text p2SideDeckCount;

    public GameAreaAnimationHandler GameAreaAnimator => _animationHandler;
    
    void Awake()
    {
        SetupCamera();
        ToggleP1DeckVisual(0);
        ToggleP1SideDeckVisual(0);
        ToggleP2DeckVisual(0);
        ToggleP2SideDeckVisual(0);
        
        p1PlacementPosition.ClearChildren();
        p2PlacementPosition.ClearChildren();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        SetupCamera();
    }
#endif
    
    void SetupCamera()
    {
        var mainCamera = Camera.main;
        if (!mainCamera) return;
        if (!_collider2D) return;
        
        var areaSize = _collider2D.bounds.size;
        if (Screen.width >= Screen.height)
            mainCamera.orthographicSize = areaSize.y / 2f; // horizontal case 
        else
            mainCamera.orthographicSize = areaSize.x / 2f; // vertical case
    }

    public void ToggleP1DeckVisual(int cardCount)
    {
        p1DeckCount.text = cardCount.ToString();
        p1DeckPlaceholder.SetActive(cardCount > 0);
    }
    
    public void ToggleP1SideDeckVisual(int cardCount)
    {
        p1SideDeckCount.text = cardCount.ToString();
        p1SideDeckPlaceholder.SetActive(cardCount > 0);
    }
    
    public void ToggleP2DeckVisual(int cardCount)
    {
        p2DeckCount.text = cardCount.ToString();
        p2DeckPlaceholder.SetActive(cardCount > 0);
    }
    
    public void ToggleP2SideDeckVisual(int cardCount)
    {
        p2SideDeckCount.text = cardCount.ToString();
        p2SideDeckPlaceholder.SetActive(cardCount > 0);
    }
}
