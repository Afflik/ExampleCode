using UnityEngine;
using UnityEngine.AI;

public class ZombieDeadState : IState
{
    private ZombieController _controller;
    private CharacterState _state;
    private bool _isInterrupted;

    public ZombieDeadState(ZombieController controller, CharacterState state)
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
        _controller.StopPlayerObserver();
        _controller.SwitchColliderState(false);
        _controller.SetStatus(CharacterStatus.Dead);
        _controller.SetAnimationParam("Dead", true);
        _controller.SetTargetStatus(TargetStatus.None);
        _controller.GetComponent<NavMeshAgent>().enabled = false;
    }
    public void StateUpdate()
    {
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateExit()
    {
    }

}
