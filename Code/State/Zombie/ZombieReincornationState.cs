using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class ZombieReincornationState : IState
{
    private bool _isInterrupted;
    private ZombieController _controller;
    private CharacterState _state;

    public ZombieReincornationState(ZombieController controller, CharacterState state)
    {
        _controller = controller;
        _state = state;
    }

    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _controller.GetComponent<Collider>().enabled = false;
        _controller.GetComponent<NavMeshAgent>().isStopped = false;
        _controller.SetStatus(CharacterStatus.Immune);
        AsyncInvoke(CalculateStateDone, 0.1f);
    }

    public void StateDone()
    {
        _controller.SetState(CharacterState.Stay);
    }

    public void StateExit()
    {
        _controller.SetStatus(CharacterStatus.Free);
        _controller.GetComponent<Collider>().enabled = true;
        _controller.GetComponent<NavMeshAgent>().enabled = true;
    }

    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateUpdate()
    {
    }
    private void CalculateStateDone()
    {
        float timeRemaining = _controller.GetAnimationTimeRemaining();
        AsyncInvoke(StateDone, timeRemaining);
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
