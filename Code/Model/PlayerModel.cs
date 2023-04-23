using Animancer.Examples.StateMachines;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlayerModel: ISubject
{
    [SerializeField] public float _hp = 100;
    [SerializeField] public float _movementSpeed = 1.5f;
    [SerializeField] public MeleeData _meleeData;
    [SerializeField] public WeaponData _weaponData;
    private List<IObserver> _observers = new List<IObserver>();


    private Transform _transform;
    private IState[] _states;
    private IState _activeState;
    private CharacterState _characterState;
    private CharacterStatus _characterStatus;
    private CombatType _combatType;
    private float _attackDistance;
    private ZombieController _focusTarget;
    private Weapon _weapon;

    public float HP => _hp;
    public MeleeData MeleeData => _meleeData;
    public WeaponData WeaponData => _weaponData;
    public ZombieController FocusTarget => _focusTarget;

    public IState ActiveState => _activeState;
    public CombatType CombatType => _combatType;
    public float AttackDistance => _attackDistance;
    public Transform Transform=> _transform;

    public PlayerModel(Transform t, float hp, float speed, MeleeData meleeData, WeaponData weaponData)
    {
        _transform = t;
        _hp = hp;
        _meleeData = Object.Instantiate(meleeData);
        _weaponData = Object.Instantiate(weaponData);
        _movementSpeed = speed;
        _attackDistance = _meleeData.AttackDistance;
        _characterState = CharacterState.Move;
        _combatType = CombatType.Melee;
        _attackDistance = _weaponData.AttackDistance;
    }

    public void SetWeapon(Transform hand, Transform[] unequipRoot)
    {
        _weapon = new Weapon(_weaponData, hand, unequipRoot);
    }

    public void StateInitialization()
    {
        _states = new IState[3];
        var controller = _transform.GetComponent<PlayerController>();
        _states[0] = new PlayerMoveState(controller, _movementSpeed, CharacterState.Move);
        _states[1] = new PlayerAttackState(controller, _weapon, _meleeData, _weaponData, CharacterState.Attack);
        _states[2] = new PlayerEquipState(controller, _weapon, _weaponData, CharacterState.Equip);
        SetState(CharacterState.Move);
    }

    public void RegisterObserver(IObserver observer)
    {
        _observers.Add(observer);
    }

    public void RemoveObserver(IObserver observer)
    {
        _observers.Remove(observer);
    }
    public void NotifyObserver(IObserver observer, ObserverPlayerType type)
    {
        switch(type)
        {
            case ObserverPlayerType.CharacterState:
                observer.ObserverUpdate(_characterState, type);
                break;
            case ObserverPlayerType.CharacterStatus:
                observer.ObserverUpdate(_characterStatus, type);
                break;
            case ObserverPlayerType.PlayerAttackDistance:
                observer.ObserverUpdate(_attackDistance, type);
                break;
        }
    }

    public void NotifyObservers<T>(T valueChanged, ObserverPlayerType type)
    {
        for (int i = 0; i < _observers.Count; i++)
        {
            _observers[i].ObserverUpdate(valueChanged, type);
        }
    }

    public void SetState(CharacterState state)
    {
        _characterState = state;
        for (int i = 0; i < _states.Length; i++)
        {
            if (_states[i].GetState() == state)
            {
                _activeState = _states[i];
            }
        }
        NotifyObservers(_characterState, ObserverPlayerType.CharacterState);
    }

    public void SetStatus(CharacterStatus status)
    {
        _characterStatus = status;
        NotifyObservers(_characterStatus, ObserverPlayerType.CharacterStatus);
    }
    public void ResetTarget()
    {
        if(_focusTarget) _focusTarget.SetTargetStatus(TargetStatus.None);
        _focusTarget = default;
    }

    public void EquipWeapon()
    {
        _combatType = CombatType.Range;
        _attackDistance = _weaponData.AttackDistance;
        NotifyObservers(_attackDistance, ObserverPlayerType.PlayerAttackDistance);
    }
    public void UnequipWeapon()
    {
        _combatType = CombatType.Melee;
        _attackDistance = _meleeData.AttackDistance;
        NotifyObservers(_attackDistance, ObserverPlayerType.PlayerAttackDistance);
    }

    public void SetFocusTarget(ZombieController target)
    {
        if (_focusTarget) ResetTarget();
        _focusTarget = target;
    }
}
