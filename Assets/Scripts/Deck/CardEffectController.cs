using UnityEngine;

public class CardEffectController : MonoBehaviour
{
    [SerializeField] BorderEffect _borderEffect;
    
    public void ShowBorder(Color color, float duration)
    {
        _borderEffect.ToggleBorderEffect(color, duration);
    }
}
