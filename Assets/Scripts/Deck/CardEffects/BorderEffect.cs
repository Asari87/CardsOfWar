using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BorderEffect : MonoBehaviour
{
    static readonly int BorderOpacity = Shader.PropertyToID("_BorderOpacity");
    static readonly int BorderColor = Shader.PropertyToID("_BorderColor");

    [SerializeField] SpriteRenderer _borderSprite;
    
    Material _spriteMaterial;
    Tween _opacityTween;
    
    void Awake()
    {
        _spriteMaterial = GetComponent<SpriteRenderer>().material;
        SetBorderColor(Color.white);
        SetOpacityValue(0, 0);
        _borderSprite.enabled = false;
    }

    void SetBorderColor(Color color)
    {
        _spriteMaterial.SetColor(BorderColor, color * 5);
    }
    
    Tween SetOpacityValue(float value, float duration)
    {
        var clampedValue =  Mathf.Clamp01(value);
        if (_opacityTween != null)
        {
            _opacityTween.Kill();
            _opacityTween = null;
        }
        
        var currentValue = _spriteMaterial.GetFloat(BorderOpacity);
        _opacityTween = DOTween.To(() => currentValue, (x) => currentValue = x, clampedValue, duration)
            .OnStart(() =>
            {
                if (value > 0)
                    _borderSprite.enabled = true;
            })
            .OnUpdate(() => _spriteMaterial.SetFloat(BorderOpacity, currentValue))
            .OnComplete(() =>
            {
                if (value < 1)
                    _borderSprite.enabled = false;
            });
        return _opacityTween;
    }

    public void ToggleBorderEffect(Color color, float effectDuration = 0.5f)
    {
        SetBorderColor(color);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(SetOpacityValue(1, effectDuration * 0.2f));
        sequence.AppendInterval(effectDuration * 0.6f);
        sequence.Append(SetOpacityValue(0,effectDuration * 0.2f));
    }
}
