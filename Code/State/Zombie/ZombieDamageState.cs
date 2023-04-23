using EPOOutline;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public class ZombieDamageState : IState
{
    private ZombieController _controller;
    private PlayerController _player;
    private CharacterState _state;
    private NavMeshAgent _agent;
    private Rigidbody[] _forceRigids;
    private Rigidbody[] _ragdoll;
    private SkinnedMeshRenderer _head;
    private bool _isInterrupted;
    private Vector3 _directionForce = Vector3.zero;
    private bool _isStartForce;
    private Vector3 _velocity;

    public ZombieDamageState(ZombieController controller, Transform head, Rigidbody[] forceRigids, Rigidbody[] ragdoll, CharacterState state)
    {
        _state= state;
        _head = head.GetComponent<SkinnedMeshRenderer>();
        _controller = controller;
        _forceRigids = forceRigids;
        _ragdoll = ragdoll;
        _agent = _controller.GetComponent<NavMeshAgent>();    
        _player = Object.FindObjectOfType<PlayerController>();
    }
    public CharacterState GetState()
    {
        return _state;
    }

    public void StateEnter()
    {
        _isStartForce = false;
        _controller.SetStatus(CharacterStatus.Damage);
        _controller.SetAnimationParam("Move", false);
        _controller.transform.LookAt(_player.transform);
    }
    public void StateUpdate()
    {
        if (_isStartForce)
        {
            var result = Vector3.SmoothDamp(_controller.transform.position, _directionForce, ref _velocity, 0.05f);
            _controller.transform.position = result;
            _agent.SetDestination(result);

            if (Vector3.Distance(_controller.transform.position, _directionForce) < 0.1f)
            {
                _isStartForce = false;
            }
        }

    }
    public void StateInterrupt()
    {
        _isInterrupted = true;
    }

    public void StateExit()
    {
        _controller.SetStatus(CharacterStatus.Free);
    }

    public void StateDone()
    {
        _controller.SetState(CharacterState.Stay);
    }

    public void GetDamageStandard(float dmg, Vector3 hitForce, Vector3 deadForce, string hitName, bool isKnockdown)
    {
        HitFX(1, false);
        _controller.ChangeHP(dmg);
        if (_controller.HP <= 0)
        {
            Dead(deadForce);
        }
        else
        {
            if (isKnockdown)
            {
                Knockdown();
            }
            else
            {
                _controller.PlayAnimationClip(hitName, 0);
                AsyncInvoke(CalculateStateDone, 0.1f);
            }
            StartDamageForce(hitForce);
        }
    }

    public void StartExecution(string hitName, float animationTime)
    {
        HitFX(1, false);
        _controller.SwitchColliderState(false);
        _controller.SetStatus(CharacterStatus.Immune);
        _controller.transform.LookAt(_player.transform.position);
        _controller.PlayAnimationClip(hitName, animationTime);
        AsyncInvoke(CalculateStateDone, 0.1f);
    }

    public void GetDamageHeadshot(Vector3 deadForce)
    {
        HitFX(0, true);
        Dead(deadForce);
    }
    public void GetDamageExecution(Vector3 force)
    {
        HitFX(1, false);
        Dead(force);
    }
    private void Knockdown()
    {
        _controller.SwitchColliderState(false);
        _controller.SetAnimationParam("hit_knockdown_hard", true);
        _controller.PlayAnimationClip("hit_knockdown_hard", 0);
        _controller.SetStatus(CharacterStatus.Knockdown);
        AsyncInvoke(StartWakeUp, 2);
    }

    private void HitFX(int fxID, bool isHeadshot)
    {
        foreach (var rig in _forceRigids)
        {
            if (rig.CompareTag("Head"))
            {
                if(isHeadshot) _head.enabled = false;
                if (isHeadshot) rig.gameObject.SetActive(true);
                var explosionFX = Object.Instantiate(rig.transform.GetChild(fxID).gameObject, rig.transform);
                explosionFX.transform.position = rig.transform.GetChild(fxID).position;
                explosionFX.transform.rotation = rig.transform.GetChild(fxID).rotation;
                explosionFX.gameObject.SetActive(true);
                Object.Destroy(explosionFX, 2);
                if (!isHeadshot)
                {
                    explosionFX.transform.SetParent(null);
                    FollowFX(explosionFX, rig.transform.GetChild(fxID));
                }
            }
        }
    }
    private void StartWakeUp()
    {
        _controller.SetAnimationParam("hit_knockdown_hard", false);
        AsyncInvoke(CalculateStateDone,0.1f);
    }
    private void CalculateStateDone()
    {
        float timeRemaining = _controller.GetAnimationTimeRemaining();
        AsyncInvoke(StateDone, timeRemaining * 0.9f);
    }
    public void RagdollActivate(Vector3 force)
    {
        _controller.transform.GetComponent<Animator>().enabled = false;
        foreach (var rig in _forceRigids)
        {
            rig.AddForce(force, ForceMode.VelocityChange);
        }
    }
    private void Dead(Vector3 force)
    {
        foreach (var rig in _ragdoll)
        {
            rig.gameObject.SetActive(true);
            rig.isKinematic = false;
        }
        RagdollActivate(force);
        _controller.SetInterruptState(CharacterState.Dead);
    }
    private void StartDamageForce(Vector3 force)
    {
        _isStartForce = true;
        _directionForce = _controller.transform.position + force;
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
    private async void FollowFX(GameObject fx, Transform follow)
    {
        while (fx)
        {
            fx.transform.position = follow.position;
            fx.transform.rotation = follow.rotation;
            await Task.Yield();
        }
    }
}
