using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class ZombieAttackState : IState
{
    private ZombieController _controller;
    private float _cooldownAttack = 1.5f;
    private CharacterState _state;
    private bool _isInterrupted;

    public ZombieAttackState(ZombieController controller, CharacterState state)
    {
        _state = state;
        _controller = controller;
    }
    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _isInterrupted = false;
        var rngAttack = Random.Range(1, 4);
        _controller.SetAnimationParam("Move", false);
        _controller.PlayAnimationClip("Attack_" + rngAttack, 0);
        _controller.SetStatus(CharacterStatus.Attack);
        AsyncInvoke(StateDone, _cooldownAttack);
    }
    public void StateUpdate()
    {
        _controller.SetStatus(CharacterStatus.Free);
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }
    private void StateDone()
    {
        _controller.SetState(CharacterState.Stay);
    }

    public void StateExit()
    {
    }


    public void DoDamage()
    {
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
