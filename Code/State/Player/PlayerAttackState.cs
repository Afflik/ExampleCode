using Animancer.Examples.StateMachines;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class PlayerAttackState : IState
{
    private CombatType _type;
    private CharacterState _state;
    private PlayerController _controller;
    private ZombieController _focusTarget;
    private NavMeshAgent _agent;
    private AttackData _attackData;
    private MeleeData _meleeData;
    private WeaponData _weaponData;
    private Weapon _weapon;
    private CameraShake _shakeCam;
    private Vector3 _pointAttackPosition;
    private bool _isCanMoveToEnemy;
    private bool _isInterrupted;
    private Vector3 _velocity;

    private List<ZombieController> _comboTargets = new List<ZombieController>();
    private List<AttackData> _meleeAttacks = new List<AttackData>();
    private List<AttackData> _procAttacks = new List<AttackData>();
    private List<AttackData> _comboAttacks = new List<AttackData>();
    private List<AttackData> _executionAttacks = new List<AttackData>();
    private List<AttackData> _dashAttacks = new List<AttackData>();

    public CharacterState GetState() { return _state; }
    public PlayerAttackState(PlayerController controller, Weapon weapon, MeleeData meleeData, WeaponData weaponData, CharacterState state)
    {
        _controller = controller;
        _meleeData = meleeData;
        _weaponData = weaponData;
        _weapon = weapon;
        _state = state;
        _agent = _controller.GetComponent<NavMeshAgent>();
        _shakeCam = Object.FindObjectOfType<CameraShake>();

        foreach (var attack in _meleeData.Attacks)
        {
            if (attack.Type == AttackType.Melee && !attack.IsCanProc)
            {
                _meleeAttacks.Add(attack);
            }
            else
            if (attack.IsCanProc)
                {
                _procAttacks.Add(attack);
            }
            else
            if (attack.Type == AttackType.Finish)
            {
                _executionAttacks.Add(attack);
            }
            else
            if (attack.Type == AttackType.Dash)
            {
                _dashAttacks.Add(attack);
            }
        }
    }

    public void StateEnter()
    {
        _isInterrupted = false;
        _type = _controller.GetCombatType();
        _focusTarget = _controller.GetFocusTarget();
        PrepareAttack();
        _controller.SetStatus(CharacterStatus.Attack);
        if (_type == CombatType.Melee)
        {
            AsyncInvoke(CalculateStateLayerMelee, 0.1f);
        }
        if (_type == CombatType.Range)
        {
            AsyncInvoke(CalculateStateLayerRange, 0.1f);
        }
    }
    public void StateUpdate()
    {
        RotateToTarget();
        if (_isCanMoveToEnemy)
        {
            FlashToTarget();
        }
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }
    private void StateDone()
    {
        if (_type == CombatType.Range && _weapon.Ammo == 0)
        {
            _controller.SetState(CharacterState.Equip);
        }
        else
        {
            _controller.SetState(CharacterState.Move);
        }
    }
    public void StateExit()
    {
        _attackData = default;
        _comboTargets.Clear();
        MoveToTargetState(false);
    }
    private void RotateToTarget()
    {
        var target = _focusTarget.transform.position;
        target -= _controller.transform.position;
        target.y = _controller.transform.position.y;
        _controller.transform.rotation = Quaternion.Slerp(_controller.transform.rotation, Quaternion.LookRotation(target), 0.3f);
    }
    private void FlashToTarget()
    {
        var result = Vector3.SmoothDamp(_controller.transform.position, _pointAttackPosition, ref _velocity, 0.1f);
        _controller.transform.position = result;
        _agent.SetDestination(result);
    }

    private void PrepareAttack()
    {
        if (!_focusTarget) return;
        if (_type == CombatType.Range)
        {
            StartShooting();
        }
        else
        {
            bool isHaveAttack = false;
            if (_meleeData.Attacks.Exists(x => x.Type == AttackType.Combo))
            {
                _meleeData.Attacks.Sort((p1, p2) => p1.ComboCount.CompareTo(p2.ComboCount));
                int minCombo = _meleeData.Attacks.Find(x => x.ComboCount > 0).ComboCount;
                Collider[] hits = Physics.OverlapSphere(_controller.transform.position, _meleeData.AttackDistance, 1 << 6);
                if (hits.Length >= minCombo)
                {
                    var comboChance = Random.Range(0, 101);
                    if (comboChance <= _meleeData.ComboChance)
                    {
                        isHaveAttack = true;
                        AttackCombo(hits);
                    }
                }
            }
            if (!isHaveAttack && _focusTarget.HP <= 30)
            {
                var rngChanceFinalAttack = Random.Range(1, 101);
                if (rngChanceFinalAttack <= 10)
                {
                    isHaveAttack = true;
                    AttackExecution();
                }
            }
            if (!isHaveAttack)
            {
                AttackStandard();
            }
        }
    }

    public void StartExecution()
    {
        _controller.transform.LookAt(_focusTarget.transform.position);
        float timePassed = _controller.GetAnimationTimePassed(0);
        _focusTarget.StartDamageExecution(_attackData.TriggerName, timePassed);
        _shakeCam.Shake();
        SoundPunch();
    }
    private void AttackStandard()
    {
        float distanceToTarget = Vector3.Distance(_controller.transform.position, _focusTarget.transform.position);
        if (distanceToTarget > _meleeData.AttackDistance + _meleeData.DashOffset)
        {
            _attackData = _meleeData.Attacks.Find(x => x.Type == AttackType.Dash);
        }
        else
        {
            float totalProcPercent = 0;
            for (int i = 0; i < _procAttacks.Count; i++)
            {
                if (_procAttacks[i].IsCanProc)
                {   
                    totalProcPercent += _procAttacks[i].ProcPercent;
                }
            }
            if (_procAttacks.Count > 0)
            {
                float rpocPercent = Random.Range(0, 101);
                if (rpocPercent <= totalProcPercent)
                {
                    _attackData = _procAttacks[Random.Range(0, _procAttacks.Count)];
                }
            }
            if (!_attackData)
            {
                _attackData = _meleeAttacks[Random.Range(0, _meleeAttacks.Count)];
            }
        }
        _controller.PlayAnimationClip(_attackData.TriggerName,0);
        MoveToTargetState(true);
    }
    private void AttackExecution()
    {
        _controller.SetStatus(CharacterStatus.Immune);
        _attackData = _executionAttacks[Random.Range(0, _executionAttacks.Count)];
        _controller.PlayAnimationClip(_attackData.TriggerName, 0);
        MoveToTargetState(true);
    }
    private void AttackCombo(Collider[] hits)
    {
        _comboAttacks.Clear();
        _controller.SetStatus(CharacterStatus.Immune);
        foreach (var attack in _meleeData.Attacks)
        {
            if (attack.Type == AttackType.Combo && hits.Length >= attack.ComboCount) _comboAttacks.Add(attack);
        }
        _attackData = _comboAttacks[Random.Range(0, _comboAttacks.Count)];
        Array.Sort(hits, (enemy1, enemy2) =>
        Vector3.Distance(enemy1.transform.position, _controller.transform.position)
                      .CompareTo(Vector3.Distance(enemy2.transform.position, _controller.transform.position)));
        for (int i = 0; i < hits.Length; i++)
        {
            _comboTargets.Add(hits[i].GetComponent<ZombieController>());
        }
        _controller.PlayAnimationClip(_attackData.TriggerName, 0);
    }
    private void MoveToTargetState(bool b)
    {
        _velocity = Vector3.zero;
        _isCanMoveToEnemy = b;
        if(b)
        {
            _pointAttackPosition = _focusTarget.transform.position - (_focusTarget.transform.position - _controller.transform.position).normalized * _attackData.Distance;
        }
    }
    public void NextComboTarget()
    {
        _focusTarget = _comboTargets[0];
        _comboTargets.Remove(_focusTarget);
        MoveToTargetState(true);
    }
    private void StartShooting()
    {
        _controller.PlayAnimationClip("Shot", 1);
    } 
    public void Shot()
    {
        if (_weapon.Ammo > 0)
        {
            _weapon.Shot();
            float distanceToTarget = Vector3.Distance(_controller.transform.position, _focusTarget.transform.position);
            AsyncInvoke(DoDamage, _weaponData.DamageDelay * distanceToTarget);
            AsyncInvoke(_shakeCam.Shake, _weaponData.DamageDelay * distanceToTarget);
        }
    }

    public void DoDamage()
    {
        if (_type == CombatType.Range)
        {
            RangeDamage();
        }
        else
        {
            MeleeDamage();
        }
    }
    private void RangeDamage()
    {
        float distanceForce = _weaponData.RagdollForceDistanceScale / Vector3.Distance(_focusTarget.transform.position, _controller.transform.position);
        if (distanceForce > _weaponData.RagdollForceDistanceScale) distanceForce = _weaponData.RagdollForceDistanceScale;
        Vector3 ragdollForce = distanceForce * GetCalculateForce(_weaponData.ForceDirection);
        _focusTarget.DoDamageHeadshot(ragdollForce);
        _controller.ResetTarget();
    }
    private void MeleeDamage()
    {
        MoveToTargetState(false);
        CharacterStatus targetStatus = CharacterStatus.Damage;
        Vector3 hitForce = GetCalculateForce(_attackData.HitForce);
        Vector3 ragdollForce = GetCalculateForce(_attackData.RagdollForce);

        if (_attackData.Type == AttackType.Finish)
        {
            _focusTarget.DoDamageExecution(ragdollForce);
            _controller.ResetTarget();
            _weapon.AddKill();
        }
        else
        {
            if (_attackData.IsCanKnockdown)
            {
                targetStatus = _focusTarget.DoDamageStandard(10, hitForce, ragdollForce, _attackData.TriggerName, true);
            }
            else
            {
                targetStatus = _focusTarget.DoDamageStandard(10, hitForce, ragdollForce, _attackData.HitName, false);
            }
        }
        if (targetStatus == CharacterStatus.Dead)
        {
            if (_weaponData) 
            {
                _weapon.AddKill();
            }
            _controller.ResetTarget();
        }
        _shakeCam.Shake();
        SoundPunch();
    }


    public void SoundPunch()
    {
        var sound = new GameObject().AddComponent<AudioSource>();
        sound.transform.position = _controller.transform.position;
        sound.clip = _attackData.SoundEffect;
        sound.Play();
        Object.Destroy(sound.gameObject, 2);
    }
    private void CalculateStateLayerMelee()
    {
        float timeRemaining = _controller.GetAnimationTimeRemaining(0);
        AsyncInvoke(StateDone, timeRemaining * _attackData.AttackCooldownModifer);
    }
    private void CalculateStateLayerRange()
    {
        float timeRemaining = _controller.GetAnimationTimeRemaining(1);
        AsyncInvoke(StateDone, timeRemaining * _weaponData.FireRateModifer);
    }

    private Vector3 GetCalculateForce(Vector3 force)
    {
        Vector3 ForceX = _focusTarget.transform.right.normalized * force.x;
        Vector3 ForceY = _focusTarget.transform.up.normalized * force.y;
        Vector3 ForceZ = _focusTarget.transform.forward.normalized * force.z;
        return ForceX + ForceY + ForceZ;
    }
    private async void AsyncInvoke(Action action, float delaySeconds)
    {
        float timer = 0;
        while (timer < delaySeconds && !_isInterrupted)
        {
            timer += Time.deltaTime;
            await Task.Yield();
        }
        if (_isInterrupted)
        {
            _isInterrupted = false;
        }
        else action?.Invoke();
    }
}
