using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float _hp = 100;
    [SerializeField] public float _movementSpeed = 1.5f;
    [SerializeField] public MeleeData _meleeData;
    [SerializeField] public WeaponData _weaponData;
    [SerializeField] private Transform _weaponHand;
    [SerializeField] private Transform[] _weaponRoots;

    private PlayerModel _model;
    private PlayerView _view;
    private EnemySearchSystem _enemySearchSystem;

    private void Awake()
    {;
        _model = new PlayerModel(transform, _hp, _movementSpeed, _meleeData, _weaponData);
        _model.SetWeapon(_weaponHand, _weaponRoots);
        _view = GetComponent<PlayerView>();
        _view.WeaponEquipped += ActivateWeapon;
        var camOffset = _model.Transform.position - Camera.main.transform.position;
        FindObjectOfType<FollowCamera>().SetOffset(camOffset);
        FindObjectOfType<FollowCamera>().AddTarget(_model.Transform);
        _enemySearchSystem = new EnemySearchSystem(_model.Transform);
        _enemySearchSystem.UpdateAttackDistance(_model.AttackDistance);
    }

    private void Start()
    {
        _model.StateInitialization();
    }
    private void Update()
    {
        EnemySearch();
        _model.ActiveState.StateUpdate();

        if (Input.GetMouseButtonUp(0))
        {
            if (_model.FocusTarget && _model.ActiveState.GetState() == CharacterState.Move)
            {
                SetState(CharacterState.Attack);
            }
        }
    }
    public ZombieController GetFocusTarget()
    {
        return _model.FocusTarget;
    }

    public CombatType GetCombatType()
    {
        return _model.CombatType; 
    }

    public void EnemySearch()
    {
        if (!_model.FocusTarget) ResetTarget();
        if (_model.ActiveState.GetState() == CharacterState.Move)
        {
            ZombieController getFocus = _enemySearchSystem.GetFocusTarget();
            if (getFocus && _model.FocusTarget != getFocus)
            {
                _model.SetFocusTarget(getFocus);
                SetAnimationParam("Focus", true);
            }
            if (_model.FocusTarget) _model.FocusTarget.SetTargetStatus(TargetStatus.Focus);
            if (!getFocus) ResetTarget();
        }
    }


    public void SetState(CharacterState state)
    {
        if (state == _model.ActiveState.GetState()) return;
        _model.ActiveState.StateExit();
        _model.SetState(state);
        _model.ActiveState.StateEnter();
    }
    public void SetStatus(CharacterStatus status)
    {
        _model.SetStatus(status);
    }
    public void SetCombatType(CombatType type)
    {
        switch (type)
        {
            case CombatType.Melee:
                _model.UnequipWeapon();
                break;
            case CombatType.Range:
                _model.EquipWeapon();
                break;
        }
        _enemySearchSystem.UpdateAttackDistance(_model.AttackDistance);
    }
    public void ActivateWeapon()
    {
        if (_model.ActiveState.GetState() == CharacterState.Move) SetState(CharacterState.Equip);
    }

    public void ResetTarget()
    {
        _model.ResetTarget();
        _enemySearchSystem.ResetFocusTarget();
        SetAnimationParam("Focus", false);
    }

    private void InvokeStateMethod(string MethodName)
    {
        var method = _model.ActiveState.GetType().GetMethod(MethodName);
        if (method != null)
        {
            method.Invoke(_model.ActiveState, null);
        }
    }
    public void SetAnimationParam<T>(string nameParam, T value)
    {
        _view.SetAnimationParam(nameParam, value);
    }
    public T GetAnimationParam<T>(string nameParam, AnimatonParameterType type)
    {
        var result = _view.GetAnimationParam<T>(nameParam, type);
        return result;
    }
    public void PlayAnimationClip(string clipName, int layer)
    {
        _view.PlayAnimationClip(clipName, layer);
    }
    public float GetAnimationTimeRemaining(int layer)
    {
        return _view.GetAnimationTimeRemaining(layer);
    }
    public float GetAnimationTimePassed(int layer)
    {
        return _view.GetAnimationTimePassed(layer);
    }

    public void AddPlayerObserver(ZombieController zombie)
    {
        _model.RegisterObserver(zombie);
    }
    public void UpdatePlayerObserver(ZombieController zombie, ObserverPlayerType type)
    {
        _model.NotifyObserver(zombie, type);
    }

    public void RemovePlayerObserver(ZombieController zombie)
    {
        _model.RemoveObserver(zombie);
    }
}
