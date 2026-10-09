using UnityEngine;

/// <summary>追撃State専用倍率。Animator全体のSlow/HitStop倍率には触れない。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class JustAvoidCounterAnimation : MonoBehaviour
{
    public const string SpeedParameter = "JustAvoidCounterSpeed";
    private static readonly int SpeedHash = Animator.StringToHash(SpeedParameter);
    /// <summary>Just Avoid追撃の振りかぶり区間のAnimator速度倍率。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃の振りかぶり区間のAnimator速度倍率。")]
    [SerializeField, Range(.7f, .85f)] private float _counterWindupSpeed = .78f;
    /// <summary>Just Avoid追撃の斬撃区間のAnimator速度倍率。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃の斬撃区間のAnimator速度倍率。")]
    [SerializeField, Range(1f, 1.15f)] private float _counterSlashSpeed = 1.10f;
    private Animator _animator;
    private int _stateHash;
    private bool _finished, _contacted;

    /// <summary>速度パラメータを操作するAnimatorを同じオブジェクトから取得する。</summary>
    private void Awake() => _animator = GetComponent<Animator>();

    /// <summary>追撃Stateの所有権を記録し、命中・終了フラグと振りかぶり速度を初期化する。</summary>
    public void BeginCounter(int stateHash)
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        _stateHash = stateHash;
        _finished = _contacted = false;
        _animator.SetFloat(SpeedHash, _counterWindupSpeed);
    }

    /// <summary>終了したStateが現在の追撃所有者の場合だけ速度を戻す。連続追撃へのBlendを保護する。</summary>
    public void ExitCounter(int stateHash)
    {
        // 追撃1から追撃2へBlendした場合、出ていくStateが新Stateの倍率を消さない。
        if (_stateHash == stateHash) ResetSpeed();
    }

    /// <summary>現在の追撃Clipからの有効Eventであれば、振りかぶりから斬撃速度へ切り替える。</summary>
    public void AnimEvent_JustAvoidCounterSlash(AnimationEvent animationEvent)
    {
        if (!AcceptEvent(animationEvent) || _finished || _contacted) return;
        _animator.SetFloat(SpeedHash, _counterSlashSpeed);
    }

    /// <summary>現在の追撃ClipからのEventだけを受け付けて速度倍率を通常へ戻す。</summary>
    public void AnimEvent_JustAvoidCounterResetSpeed(AnimationEvent animationEvent)
    {
        if (AcceptEvent(animationEvent)) ResetSpeed();
    }

    // 既存終了Eventも受け取り、Clip追加Eventが省略された場合にも復帰する。
    /// <summary>追撃の終了Eventで速度倍率を解除し、Clip側の追加Eventがなくても復帰させる。</summary>
    public void AnimEvent_OnAttackFinished(AnimationEvent animationEvent)
    {
        if (AcceptEvent(animationEvent)) ResetSpeed();
    }

    /// <summary>Event発生元のStateと現在の追撃所有者が一致するか検証する。</summary>
    /// <returns>現在の追撃Stateから届いた有効Eventならtrue。</returns>
    private bool AcceptEvent(AnimationEvent animationEvent) => _stateHash != 0 &&
        animationEvent.animatorStateInfo.fullPathHash == _stateHash && IsCurrentCounter();

    /// <summary>現在または遷移先のAnimator Stateが所有中の追撃か確認する。</summary>
    /// <returns>有効なAnimatorで追撃タグとStateハッシュが一致すればtrue。</returns>
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

    /// <summary>追撃の所有権を消去し、Animatorの専用速度パラメータを1へ戻す。</summary>
    public void ResetSpeed()
    {
        _finished = true;
        _stateHash = 0;
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetFloat(SpeedHash, 1f);
    }

    /// <summary>追撃Stateから離れた場合の取り残された速度倍率を解除する。</summary>
    private void LateUpdate()
    {
        if (_stateHash != 0 && !IsCurrentCounter()) ResetSpeed();
    }
    /// <summary>停止時に追撃速度を通常倍率へ戻す。</summary>
    private void OnDisable() => ResetSpeed();
}
