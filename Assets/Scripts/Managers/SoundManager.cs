using UnityEngine;

public class SoundManager : GenericSingelton<SoundManager>
{
    [SerializeField] AudioSource _musicSource;
    [SerializeField] AudioSource _effectSource;


    public void PlayEffect(AudioClip clip)
    {
        if(_effectSource.isPlaying)
            _effectSource.Stop();
        
        _effectSource.clip = clip;
        _effectSource.pitch = Random.Range(0.8f, 1.2f);
        _effectSource.Play();
    }

    public void PlayMusic(AudioClip clip)
    {
        if(_musicSource.isPlaying)
            _musicSource.Stop();
        
        _musicSource.clip = clip;
        _musicSource.Play();
    }
    
}
