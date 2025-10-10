using System;
using DG.Tweening;
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
    
    int _prevP1DeckCount;
    int _prevP1SideDeckCount;
    int _prevP2DeckCount;
    int _prevP2SideDeckCount;
    
    ScreenOrientation _prevScreenOrientation;

    const float OrientationCheckInterval = 0.25f;
    float _timeSinceLastCheck = float.MaxValue;
    
    Camera _mainCamera;
    
    void Awake()
    {
        _mainCamera ??= Camera.main;
        SetupCamera();
        ToggleP1DeckVisual(0);
        ToggleP1SideDeckVisual(0);
        ToggleP2DeckVisual(0);
        ToggleP2SideDeckVisual(0);
        
        p1PlacementPosition.ClearChildren();
        p2PlacementPosition.ClearChildren();

        _prevScreenOrientation = Screen.orientation;
    }

    void Update()
    {
        _timeSinceLastCheck += Time.deltaTime;
        if (_timeSinceLastCheck >= OrientationCheckInterval)
        {
            _timeSinceLastCheck = 0;
            if (_prevScreenOrientation == Screen.orientation) return;
            
            _prevScreenOrientation = Screen.orientation;
            SetupCamera();
        }
    }

    void SetupCamera()
    {
        if (!_mainCamera) return;
        if (!_collider2D) return;

        /*        Math by GPT        */
        var size = _collider2D.bounds.size;
        var w = size.x;
        var h = size.y;
        var a = _mainCamera.aspect;

        // Width and height constraints at the worst-case rotation angle
        // Derived from bounding box formula for a rotated rectangle
        float theta = Mathf.Atan2(h, w);
        float maxH = 0.5f * (h * Mathf.Abs(Mathf.Cos(theta)) + w * Mathf.Abs(Mathf.Sin(theta)));
        float maxW = 0.5f * (w * Mathf.Abs(Mathf.Cos(theta)) + h * Mathf.Abs(Mathf.Sin(theta)));
        /*****************************/
        
        if (Screen.width >= Screen.height)
            _mainCamera.DOOrthoSize(maxW / a, .1f); // horizontal case 
        else
            _mainCamera.DOOrthoSize(maxH * 2, .1f); // vertical case
    }

    public void ToggleP1DeckVisual(int cardCount)
    {
        DoRunningNumberAnimation(_prevP1DeckCount, cardCount, p1DeckPlaceholder, p1DeckCount).Play();
        _prevP1DeckCount = cardCount;
    }
    
    public void ToggleP1SideDeckVisual(int cardCount)
    {
        DoRunningNumberAnimation(_prevP1SideDeckCount, cardCount, p1SideDeckPlaceholder, p1SideDeckCount).Play();
        _prevP1SideDeckCount = cardCount;
    }
    
    public void ToggleP2DeckVisual(int cardCount)
    {
        DoRunningNumberAnimation(_prevP2DeckCount, cardCount, p2DeckPlaceholder, p2DeckCount).Play();
        _prevP2DeckCount = cardCount;
    }
    
    public void ToggleP2SideDeckVisual(int cardCount)
    {
        DoRunningNumberAnimation(_prevP2SideDeckCount, cardCount, p2SideDeckPlaceholder, p2SideDeckCount).Play();
        _prevP2SideDeckCount = cardCount;
    }

    Tween DoRunningNumberAnimation(int startFrom, int target, GameObject deckPlaceholder, TMP_Text deckCountText)
    {
        return DOTween.To(() => startFrom, x => startFrom = x, target, 0.1f)
            .OnUpdate(() =>
            {
                deckCountText.text = startFrom.ToString();
                deckPlaceholder.SetActive(target > 0);
            })
            .OnComplete(() =>
            {
                deckCountText.text = target.ToString();
                deckPlaceholder.SetActive(target > 0);
            });
    }
}
