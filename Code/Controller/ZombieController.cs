using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class ZombieController: MonoBehaviour, IObserver
{
    [SerializeField][ColorUsage(true, true)] private Color _focusColor;
    [SerializeField][ColorUsage(true, true)] private Color _detectedColor;
    [SerializeField] private Rigidbody[] _forceRigids;
    [SerializeField] private Transform _bodies;
    [SerializeField] private Transform _heads;
    [SerializeField] private Transform _legs;

    private PlayerController _playerController;
    private ZombieModel _model;
    private ZombieView _view;
    private Collider _collider;

    private float _playerAttackDistance;
    private CharacterState _playerState;
    private CharacterStatus _playerStatus;

    public float HP => _model.HP;
    public CharacterStatus CharacterStatus =>_model.CharacterStatus;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _playerController = FindObjectOfType<PlayerController>();
        _model = new ZombieModel(transform, _forceRigids);
        _view = GetComponent<ZombieView>();
        Vector3 target = _playerController.transform.position;
        target.y = transform.position.y;
        transform.LookAt(target);
       CustomizateZombie();
        PlayAudioIdleZombie();
    }
    private void Start()
    {
        _model.StateInitialization();
        SetState(CharacterState.None);

        AddPlayerObserver();
        UpdatePlayerObserver(ObserverPlayerType.CharacterState);
        UpdatePlayerObserver(ObserverPlayerType.CharacterStatus);
        UpdatePlayerObserver(ObserverPlayerType.PlayerAttackDistance);
    }

    private void Update()
    {
        if (_model.ActiveState == null) return;
        _model.ActiveState.StateUpdate();
    }
    private void LateUpdate()
    {
        if (_model.ActiveState.GetState() == CharacterState.Dead) enabled = false;
    }
    private void CustomizateZombie()
    {
        if (_heads)
        {
            _model.SetSkin(_heads, Random.Range(0, _heads.childCount), true);
        }
        if (_bodies)
        {
            _model.SetSkin(_bodies, Random.Range(0, _bodies.childCount), false);
        }
        if (_legs)
        {
            _model.SetSkin(_legs, Random.Range(0, _legs.childCount), false);
        }
    }
    public void ChangeHP(float dmg)
    {
        _model.ChangeHP(dmg);
    }
    public void SetState(CharacterState state)
    {
        if (_model.ActiveState != null) _model.ActiveState.StateExit();
        _model.SetState(state);
        _model.ActiveState.StateEnter();
    }

    public void SetInterruptState(CharacterState state)
    {
        if (_model.ActiveState != null)
        {
            _model.ActiveState.StateInterrupt();
        }
        _model.SetState(state);
        _model.ActiveState.StateEnter();
    }
    public void SetStatus(CharacterStatus status)
    {
        _model.SetStatus(status);
        switch (status)
        {
            case CharacterStatus.Free:
                PlayAudioIdleZombie();
                break;
            case CharacterStatus.Attack:
                SwitchAudioClip(ZombieAudioType.Attack);
                PlayAudioState(true);
                break;
            case CharacterStatus.Damage:
                SwitchAudioClip(ZombieAudioType.Hit);
                PlayAudioState(true);
                break;
            case CharacterStatus.Dead:
                SwitchAudioClip(ZombieAudioType.Death);
                PlayAudioState(true);
                break;
            case CharacterStatus.Immune:
                SetTargetStatus(TargetStatus.None);
                break;
            case CharacterStatus.Knockdown:
                SetTargetStatus(TargetStatus.None);
                break;
        }
    }
    public void SetTargetStatus(TargetStatus targetStatus)
    {
        _model.SetTargetStatus(targetStatus);
    }

    public void SwitchColliderState(bool b)
    {
        _collider.enabled = b;
    }
    public void StartDamageExecution(string hitName, float animationTime)
    {
        _view.PlayAnimationClip(hitName, animationTime);
        SetInterruptState(CharacterState.Damage);
    }
    public CharacterStatus DoDamageStandard(float dmg, Vector3 hitForce, Vector3 ragdollForce, string hitName, bool isKnockdown)
    {
        SetInterruptState(CharacterState.Damage);
        ZombieDamageState damageState = _model.ActiveState as ZombieDamageState;
        damageState.GetDamageStandard(dmg, hitForce, ragdollForce, hitName, isKnockdown);
        return _model.CharacterStatus;
    }
    public CharacterStatus DoDamageExecution(Vector3 ragdollForce)
    {
        ZombieDamageState damageState = _model.ActiveState as ZombieDamageState;
        damageState.GetDamageExecution(ragdollForce);
        return _model.CharacterStatus;
    }
    public CharacterStatus DoDamageHeadshot(Vector3 ragdollForce)
    {
        SetInterruptState(CharacterState.Damage);
        ZombieDamageState damageState = _model.ActiveState as ZombieDamageState;
        damageState.GetDamageHeadshot(ragdollForce);
        return _model.CharacterStatus;
    }

    public void RotateToPlayer()
    {
        Vector3 target = _playerController.transform.position;
        target -= transform.position;
        target.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), 0.2f);
    }
    public void CheckDistanceToPlayer()
    {
        if (_model.ActiveState.GetState() != CharacterState.Move && _model.ActiveState.GetState() != CharacterState.Stay) return;
        float distanceToPlayer = Vector3.Distance(transform.position, _playerController.transform.position);
        if (distanceToPlayer < _playerAttackDistance)
        {
            if (_model.TargetStatus != TargetStatus.Focus) SetTargetStatus(TargetStatus.Detected);
        }
        else
        {
            _model.SetTargetStatus(TargetStatus.None);
        }
        if (distanceToPlayer < _model.DistanceToAttack)
        {
            SetState(CharacterState.Attack);
        }
    }
    public void InvokeZombieMethod(string MethodName)
    {
        var method = _model.GetType().GetMethod(MethodName);
        if (method != null)
        {
            method.Invoke(_model.ActiveState, null);
        }
    }
    public void SetAnimationParam<T>(string nameParam, T value)
    {
        _view.SetAnimationParam(nameParam, value);
    }
    public void PlayAnimationClip(string clipName, int layer)
    {
        _view.PlayAnimationClip(clipName, layer);
    }
    public void PlayAnimationClip(string clipName, float animationTime)
    {
        _view.PlayAnimationClip(clipName, animationTime);
    }
    public float GetAnimationTimeRemaining()
    {
        return _view.GetAnimationTimeRemaining();
    }
    public float GetAnimationTimePassed()
    {
        return _view.GetAnimationTimePassed();
    }
    private void SwitchAudioClip(ZombieAudioType type)
    {
        _view.SwitchAudioClip(type);
    }
    private void PlayAudioState(bool b)
    {
        _view.PlayAudioState(b);
    }
    private float GetAudioLength()
    {
        return _view.GetAudioLength();
    }
    private void PlayAudioIdleZombie()
    {
        CancelInvoke(nameof(PlayAudioIdleZombie));
        _view.SwitchAudioClip(ZombieAudioType.Idle);
        _view.PlayAudioState(true);
        Invoke(nameof(PlayAudioIdleZombie), _view.GetAudioLength());
    }
    private void AddPlayerObserver()
    {
        _playerController.AddPlayerObserver(this);
    }
    public void ObserverUpdate<T>(T valueChanged, ObserverPlayerType type)
    {
        switch (type)
        {
            case ObserverPlayerType.CharacterState:
                _playerState= (CharacterState)(object)valueChanged;
                break;
            case ObserverPlayerType.CharacterStatus:
                _playerStatus = (CharacterStatus)(object)valueChanged;
                
                if (_playerStatus == CharacterStatus.Free &&
                    _model.ActiveState.GetState() == CharacterState.Stay &&
                     _model.CharacterStatus == CharacterStatus.Immune)
                {
                    SetStatus(CharacterStatus.Free);
                }
                if (_playerStatus == CharacterStatus.Immune && _model.ActiveState.GetState() == CharacterState.Move)
                {
                    SetStatus(CharacterStatus.Immune);
                    SetState(CharacterState.Stay);
                }
        break;
            case ObserverPlayerType.PlayerAttackDistance:
                _playerAttackDistance= (float)(object)valueChanged;
                break;
        }
    }
    public void UpdatePlayerObserver(ObserverPlayerType type)
    {
        _playerController.UpdatePlayerObserver(this,  type);
    }
    public void StopPlayerObserver()
    {
        _playerController.RemovePlayerObserver(this);
    }

}
