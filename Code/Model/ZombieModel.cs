using EPOOutline;
using UnityEngine;

public class ZombieModel
{
    private float _hp = 40;
    private Transform _transform;
    private IState[] _states;
    private IState _activeState;
    private Transform _head;
    private CharacterState _characterState;
    private CharacterStatus _characterStatus;
    private TargetStatus _targetStatus;
    private Outlinable _outlinable;
    private Rigidbody[] _forceRigids;
    private FollowCamera _followCamera;
    private Collider _collider;
    private CharacterState _state;
    private float _distanceToAttack = 1f;
    private Rigidbody[] _ragdoll;

    public float HP => _hp;
    public Transform Transform => _transform;
    public IState ActiveState => _activeState;
    public TargetStatus TargetStatus => _targetStatus;
    public CharacterStatus CharacterStatus => _characterStatus;

    public float DistanceToAttack => _distanceToAttack;

    public ZombieModel(Transform t, Rigidbody[] forceRigids)
    {
        _transform = t;
        _forceRigids = forceRigids;
        _collider = t.GetComponent<Collider>();
        _collider.enabled = false;
        _outlinable = t.GetComponent<Outlinable>();
        _followCamera = Object.FindObjectOfType<FollowCamera>();
        var allRigids = t.GetComponentsInChildren<Rigidbody>();
        _ragdoll = allRigids;
        foreach (var rig in _ragdoll)
        {
            if (rig.transform != _transform)
            {
                rig.gameObject.SetActive(false);
                rig.isKinematic = true;
            }
        }
    }

    public void StateInitialization()
    {
        var controller = _transform.GetComponent<ZombieController>();
        _states = new IState[6];
        _states[0] = new ZombieReincornationState(controller, CharacterState.None);
        _states[1] = new ZombieStayState(controller, CharacterState.Stay);
        _states[2] = new ZombieMoveState(controller, CharacterState.Move);
        _states[3] = new ZombieAttackState(controller, CharacterState.Attack);
        _states[4] = new ZombieDamageState(controller, _head, _forceRigids, _ragdoll, CharacterState.Damage);
        _states[5] = new ZombieDeadState(controller, CharacterState.Dead);
    }

    public void SetSkin(Transform bodyPart, int id, bool isHead)
    {
        bodyPart = bodyPart.GetChild(id);
        bodyPart.gameObject.SetActive(true);
        _outlinable.TryAddTarget(new OutlineTarget(bodyPart.GetComponent<Renderer>()));
        if (isHead)
        {
            _head = bodyPart;
        }
    }

    public void ChangeHP(float dmg)
    {
        _hp -= dmg;
    }
    public void SetState(CharacterState state)
    {
        _characterState = state;
        for (int i = 0; i < _states.Length; i++)
        {
            if (_states[i].GetState() == state)
            {
                _activeState = _states[i];
                break;
            }
        }
    }
    public void SetStatus(CharacterStatus status)
    {
        _characterStatus = status;
    }


    public void SetTargetStatus(TargetStatus targetStatus)
    {
        _targetStatus = targetStatus;
        _outlinable.enabled = false;
        switch (targetStatus)
        {
            case TargetStatus.None:
                _followCamera.RemoveTarget(Transform);
                break;
            case TargetStatus.Detected:
                _followCamera.AddTarget(Transform, false);
                break;
            case TargetStatus.Focus:
                _outlinable.enabled = true;
                _followCamera.AddTarget(Transform, true);
                break;
        }
    }
}
