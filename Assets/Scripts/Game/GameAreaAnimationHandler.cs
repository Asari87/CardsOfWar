using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameAreaAnimationHandler : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] float _attackAnimationDuration;
    [SerializeField] float _warAnimationDuration;
    
    [Header("Sounds")]
    [SerializeField] AudioClip _attackClip;
    [SerializeField] AudioClip _warClip;
    
    static readonly int P1Attack = Animator.StringToHash("p1attack");
    static readonly int P2Attack = Animator.StringToHash("p2attack");
    static readonly int War = Animator.StringToHash("war");



    public async UniTask TriggerP1Win()
    {
        _animator.SetTrigger(P1Attack);
        await UniTask.Delay(TimeSpan.FromSeconds(_attackAnimationDuration));
    }
    
    public async UniTask TriggerP2Win()
    {
        _animator.SetTrigger(P2Attack);
        await UniTask.Delay(TimeSpan.FromSeconds(_attackAnimationDuration));
    }
    
    public async UniTask TriggerWar()
    {
        _animator.SetTrigger(War);
        await UniTask.Delay(TimeSpan.FromSeconds(_warAnimationDuration));
    }

    public void PlayAttackSound()
    {
        SoundManager.Instance.PlayEffect(_attackClip);
    }

    public void PlayWarSound()
    {
        SoundManager.Instance.PlayEffect(_warClip);
    }
    
}
