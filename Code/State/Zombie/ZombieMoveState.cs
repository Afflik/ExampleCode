using UnityEngine;
using UnityEngine.AI;

public class ZombieMoveState : IState
{
    private NavMeshAgent _agent;
    private ZombieController _controller;
    private PlayerController _player;
    private CharacterState _state;
    private bool _isInterrupted;

    public ZombieMoveState(ZombieController controller, CharacterState state)
    {
        _state = state;
        _controller = controller;
        _agent = _controller.GetComponent<NavMeshAgent>();
        _player = Object.FindObjectOfType<PlayerController>();
    }
    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _isInterrupted = false;
        _agent.isStopped = false;
        _controller.SetAnimationParam<float>("MoveID", Random.Range(0, 4));
        _controller.SetAnimationParam("Move", true);
    }
    public void StateUpdate()
    {
        _controller.RotateToPlayer();
        _controller.CheckDistanceToPlayer();
        Moving();
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateExit()
    {
    }

    public void Moving()
    {
        _agent.SetDestination(_player.transform.position);
    }
}
