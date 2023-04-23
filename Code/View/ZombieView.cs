using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieView : MonoBehaviour
{
    private AudioSource _audio;
    private Animator _anim;
    [SerializeField] private AudioClip[] _soundsIdle;
    [SerializeField] private AudioClip[] _soundsHits;
    [SerializeField] private AudioClip[] _soundsAttacks;
    [SerializeField] private AudioClip[] _soundsDeath;

    private void Awake()
    {
        _anim= GetComponent<Animator>();
        _audio= GetComponent<AudioSource>();
    }
    public void SetAnimationParam<T>(string nameParam, T value)
    {
        if (value is bool) _anim.SetBool(nameParam, (bool)(object)value);
        if (value is int) _anim.SetInteger(nameParam, (int)(object)value);
        if (value is float) _anim.SetFloat(nameParam, (float)(object)value);
    }
    public void PlayAnimationClip(string clipName, int layer)
    {
        _anim.Play(clipName, layer);
    }
    public void PlayAnimationClip(string clipName, float animationTime)
    {
        _anim.Play(clipName, 0, animationTime);
    }
    public float GetAnimationTimeRemaining()
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        float timePassed = stateInfo.normalizedTime;
        float timeRemaining = animationLength * (1 - timePassed);
        return timeRemaining * _anim.speed;
    }
    public float GetAnimationTimePassed()
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime * _anim.speed;
    }
    public void SwitchAudioClip(ZombieAudioType type)
    {
        switch(type)
        {
            case ZombieAudioType.Idle:
                _audio.clip = _soundsIdle[Random.Range(0, _soundsIdle.Length)];
                break;
            case ZombieAudioType.Hit:
                _audio.clip = _soundsHits[Random.Range(0, _soundsHits.Length)];
                break;
            case ZombieAudioType.Attack:
                _audio.clip = _soundsAttacks[Random.Range(0, _soundsAttacks.Length)];
                break;
            case ZombieAudioType.Death:
                _audio.clip = _soundsDeath[Random.Range(0, _soundsDeath.Length)];
                break;
        }
    }
    public void PlayAudioState(bool b)
    {
        if(b)_audio.Play();
        else _audio.Stop();
    }
    public float GetAudioLength()
    {
        return _audio.clip.length;
    }
}
