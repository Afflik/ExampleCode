using UnityEngine;
using UnityEngine.AI;

public class PlayerMoveState : IState
{
    private CharacterState _state;
    private PlayerController _controller;
    private Vector3 _startMousePosition;
    private float _moveMax;
    private float _speed;
    private NavMeshAgent _agent;
    private bool _isInterrupted;

    public CharacterState GetState() { return _state; }

    public PlayerMoveState(PlayerController controller, float speed, CharacterState state)
    {
        _controller = controller;
        _speed = speed;
        _state = state;
        _agent = _controller.GetComponent<NavMeshAgent>();
    }

    public void StateEnter()
    {
        _controller.SetStatus(CharacterStatus.Free);
        _isInterrupted = false;
    }
    public void StateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartMoving();
        }
        if (Input.GetMouseButton(0))
        {
            JoystickMoving();
        }
        if (Input.GetMouseButtonUp(0))
        {
            StopMoving(); 
        }
        JoystickRotating();
    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateExit()
    {
    }
    public void StartMoving()
    {
        _startMousePosition = Input.mousePosition;
    }
    public void StopMoving()
    {
        _controller.SetAnimationParam("MoveZ", 0f);
        _controller.SetAnimationParam("MoveX", 0f);
        _controller.SetAnimationParam("Stay", 0f);
        _controller.SetAnimationParam("Moving", false);
    }


    public void JoystickMoving()
    {
        if (_startMousePosition == Input.mousePosition) return;
        _controller.SetAnimationParam("Moving", true);
        if (_controller.GetFocusTarget())
        {
            _moveMax = 2;
        }
        else
        {
            _moveMax = 1;
        }

        var y = Mathf.Atan2(_startMousePosition.y - Input.mousePosition.y, Input.mousePosition.x - _startMousePosition.x) * Mathf.Rad2Deg + 90 - _controller.transform.rotation.eulerAngles.y;
        var direction = Quaternion.Euler(0, y, 0) * _controller.transform.forward;
        if (_agent) _agent.SetDestination(_controller.transform.position += direction.normalized * _speed * Time.deltaTime);
        else _controller.transform.position += direction.normalized * _speed * Time.deltaTime;

        float move = _controller.GetAnimationParam<float>("Move", AnimatonParameterType.Float);
        if (move < _moveMax) move += Time.deltaTime * 8;
        if (move > _moveMax) move -= Time.deltaTime * 8;
        _controller.SetAnimationParam("Move", move);
        float z = Vector3.Dot(_controller.transform.forward.normalized, direction.normalized);
        _controller.SetAnimationParam("MoveZ", z);
        float x = Vector3.Dot(_controller.transform.right.normalized, direction.normalized);
        _controller.SetAnimationParam("MoveX", x);
    }
    public void JoystickRotating()
    {
        if (_startMousePosition == Input.mousePosition) return;
        var mouseDirection = (_startMousePosition - Input.mousePosition).normalized;
        var rotateY = Mathf.Atan2(-mouseDirection.x, -mouseDirection.y) * Mathf.Rad2Deg + Camera.main.transform.rotation.eulerAngles.y;
        var rotationAngle = Quaternion.Euler(0, rotateY, 0);
        if (_controller.GetFocusTarget())
        {
            var target = _controller.GetFocusTarget().transform.position - _controller.transform.position;
            target.y = _controller.transform.position.y;
            _controller.transform.rotation = Quaternion.Slerp(_controller.transform.rotation, Quaternion.LookRotation(target), 0.2f);

        }
        if (!_controller.GetFocusTarget())
        {
            _controller.transform.rotation = Quaternion.RotateTowards(_controller.transform.rotation, rotationAngle, Time.deltaTime * 700);
        }
    }
}
