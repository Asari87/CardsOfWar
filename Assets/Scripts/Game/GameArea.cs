using System;
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
    
    [Header("Player 2 References")]
    public Transform p2DeckPosition;
    public Transform p2SideDeckPosition;
    public Transform p2PlacementPosition;
    [SerializeField] GameObject p2DeckPlaceholder;
    [SerializeField] GameObject p2SideDeckPlaceholder;

    public GameAreaAnimationHandler GameAreaAnimator => _animationHandler;
    
    void Awake()
    {
        SetupCamera();
        ToggleP1DeckVisual(false);
        ToggleP1SideDeckVisual(false);
        ToggleP2DeckVisual(false);
        ToggleP2SideDeckVisual(false);
        
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

    public void ToggleP1DeckVisual(bool hasCards)
    {
        p1DeckPlaceholder.SetActive(hasCards);
    }
    
    public void ToggleP1SideDeckVisual(bool hasCards)
    {
        p1SideDeckPlaceholder.SetActive(hasCards);
    }
    
    public void ToggleP2DeckVisual(bool hasCards)
    {
        p2DeckPlaceholder.SetActive(hasCards);
    }
    
    public void ToggleP2SideDeckVisual(bool hasCards)
    {
        p2SideDeckPlaceholder.SetActive(hasCards);
    }
}
