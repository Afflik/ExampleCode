using Animancer.Examples.StateMachines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerEquipState : IState
{
    private PlayerController _controller;
    private WeaponData _weaponData;
    private Weapon _weapon;
    private CombatType _type;
    private CharacterState _state;
    bool _isInterrupted;

    public PlayerEquipState(PlayerController controller, Weapon weapon, WeaponData weaponData, CharacterState state)
    {
        _state = state;
        _weapon = weapon;
        _controller = controller;
        _weaponData = weaponData;
    }

    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _isInterrupted = false;
        _type = _controller.GetCombatType();
        _controller.SetStatus(CharacterStatus.Equipping);
        if (_type == CombatType.Melee)
        {
            _weapon.EnableWeapon();
            _controller.SetAnimationParam("Pistol", true);
            _controller.SetCombatType(CombatType.Range);
        }
        else
        {
            _weapon.DisableWeapon();
            _controller.SetAnimationParam("Pistol", false);
            _controller.SetCombatType(CombatType.Melee);
        }
        AsyncInvoke(CalculateStateLayerRange, 0.1f);
    }
    public void StateUpdate()
    {
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateDone()
    {
        _controller.SetState(CharacterState.Move);
    }
    public void StateExit()
    {
    }
    public void SwitchWeaponVisual()
    {
        if (_type == CombatType.Melee)
        {
            _weapon.EquipWeaponVisual();
        }
        else
        {
            _weapon.UnequipWeaponVisual();
        }
    }
    private void CalculateStateLayerRange()
    {
        float timeRemaining = _controller.GetAnimationTimeRemaining(1);
        AsyncInvoke(StateDone, timeRemaining);
    }
    private async void AsyncInvoke(Action action, float delaySeconds)
    {
        float timer = 0;
        while (timer < delaySeconds)
        {
            timer += Time.deltaTime;
            await Task.Yield();
        }
        if (_isInterrupted) return;
        action.Invoke();
    }

}
