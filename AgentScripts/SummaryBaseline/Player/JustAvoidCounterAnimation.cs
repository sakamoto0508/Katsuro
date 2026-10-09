using UnityEngine;

/// <summary>追撃State専用倍率。Animator全体のSlow/HitStop倍率には触れない。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class JustAvoidCounterAnimation : MonoBehaviour
{
    public const string SpeedParameter = "JustAvoidCounterSpeed";
    private static readonly int SpeedHash = Animator.StringToHash(SpeedParameter);
    [SerializeField, Range(.7f, .85f)] private float _counterWindupSpeed = .78f;
    [SerializeField, Range(1f, 1.15f)] private float _counterSlashSpeed = 1.10f;
    private Animator _animator;
    private int _stateHash;
    private bool _finished, _contacted;

    private void Awake() => _animator = GetComponent<Animator>();

    public void BeginCounter(int stateHash)
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        _stateHash = stateHash;
        _finished = _contacted = false;
        _animator.SetFloat(SpeedHash, _counterWindupSpeed);
    }

    public void ExitCounter(int stateHash)
    {
        // 追撃1から追撃2へBlendした場合、出ていくStateが新Stateの倍率を消さない。
        if (_stateHash == stateHash) ResetSpeed();
    }

    public void AnimEvent_JustAvoidCounterSlash(AnimationEvent animationEvent)
    {
        if (!AcceptEvent(animationEvent) || _finished || _contacted) return;
        _animator.SetFloat(SpeedHash, _counterSlashSpeed);
    }

    public void AnimEvent_JustAvoidCounterResetSpeed(AnimationEvent animationEvent)
    {
        if (AcceptEvent(animationEvent)) ResetSpeed();
    }

    // 既存終了Eventも受け取り、Clip追加Eventが省略された場合にも復帰する。
    public void AnimEvent_OnAttackFinished(AnimationEvent animationEvent)
    {
        if (AcceptEvent(animationEvent)) ResetSpeed();
    }

    private bool AcceptEvent(AnimationEvent animationEvent) => _stateHash != 0 &&
        animationEvent.animatorStateInfo.fullPathHash == _stateHash && IsCurrentCounter();

    private bool IsCurrentCounter()
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return false;
        var state = _animator.IsInTransition(0) ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        return state.fullPathHash == _stateHash && state.IsTag(PlayerAttacker.JustAvoidCounterTag);
    }

    /// <summary>受理された追撃Damageから呼ぶ。HitStop中に倍率を1へ戻し、復帰後は通常速度。</summary>
    public void OnCounterHit()
    {
        if (_finished || !IsCurrentCounter()) return;
        _contacted = true;
        _animator.SetFloat(SpeedHash, 1f);
    }

    public void ResetSpeed()
    {
        _finished = true;
        _stateHash = 0;
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetFloat(SpeedHash, 1f);
    }

    private void LateUpdate()
    {
        if (_stateHash != 0 && !IsCurrentCounter()) ResetSpeed();
    }
    private void OnDisable() => ResetSpeed();
}
