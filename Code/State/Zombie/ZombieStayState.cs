using UnityEngine;
using UnityEngine.AI;

public class ZombieStayState : IState
{
    private ZombieController _controller;
    private CharacterState _state;
    private bool _isInterrupted;
    private NavMeshAgent _agent;
    public ZombieStayState(ZombieController controller, CharacterState state)
    {
        _controller = controller;
        _state = state;
        _agent = _controller.GetComponent<NavMeshAgent>();
    }

    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _isInterrupted = false;
        _agent.isStopped = true;
        _controller.SwitchColliderState(true);
        _controller.SetAnimationParam<float>("IdleID", Random.Range(0, 4));
        _controller.SetAnimationParam("Move", false);
    }
    public void StateUpdate()
    {
        _controller.RotateToPlayer();
       _controller.CheckDistanceToPlayer();
        if (_controller.CharacterStatus == CharacterStatus.Free)
        {
            _controller.SetState(CharacterState.Move);
        }
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateExit()
    {
        _agent.isStopped = false;
    }

}
