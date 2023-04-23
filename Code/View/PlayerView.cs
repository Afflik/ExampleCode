using System;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public event Action WeaponEquipped;
    private WeaponUI _weaponUI;
    private Animator _anim;

    void Start()
    {
        _anim = GetComponent<Animator>();
        _weaponUI = FindObjectOfType<WeaponUI>();
        if(_weaponUI) _weaponUI.SetView(this);
    }

    public void EquipWeapon()
    {
        WeaponEquipped.Invoke();
    }
    public void SetAnimationParam<T>(string nameParam, T value)
    {
        if(value is bool) _anim.SetBool(nameParam, (bool)(object)value);
        if (value is int) _anim.SetInteger(nameParam, (int)(object)value);
        if (value is float) _anim.SetFloat(nameParam, (float)(object)value);
    }
    public T  GetAnimationParam<T>(string nameParam, AnimatonParameterType type)
    {
        T result = default;
        switch (type)
        {
            case AnimatonParameterType.Bool:
                result = (T)(object)_anim.GetBool(nameParam);
                break;
            case AnimatonParameterType.Int:
                result = (T)(object)_anim.GetInteger(nameParam);
                break;
            case AnimatonParameterType.Float:
                result = (T)(object)_anim.GetFloat(nameParam);
                break;
        }
        return result;
    }
    public void PlayAnimationClip(string clipName, int layer)
    {
        _anim.Play(clipName, layer);
    }
    public float GetAnimationTimeRemaining(int layer)
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(layer);
        float animationLength = stateInfo.length;
        float timePassed = stateInfo.normalizedTime;
        float timeRemaining = animationLength * (1 - timePassed);
        return timeRemaining * _anim.speed;
    }
    public float GetAnimationTimePassed(int layer)
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.normalizedTime * _anim.speed;
    }
}
