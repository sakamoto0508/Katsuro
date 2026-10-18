using UnityEngine;

/// <summary>敵のPattern進行と既存Animator・武器・移動の接続。単発攻撃の実行経路とは独立。</summary>
public partial class EnemyController
{
    private EnemyAttackPatternRunner _patternRunner;
    public bool IsAttackPatternActive => _patternRunner != null && _patternRunner.IsActive;

    private void EnsurePatternRunner()
    {
        if (_patternRunner == null)
            _patternRunner = new EnemyAttackPatternRunner(StartPatternClip,
                () => _attacker?.DisableWeaponHitbox(), FinishAttackPattern,
                pattern => _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.PlaybackSpeed));
    }

    private bool BeginAttackPattern(EnemyAttackPattern pattern)
    {
        if (IsTraining || _dead || pattern == null || !pattern.IsValid || _animator == null || _attacker == null ||
            (_game != null && !_game.IsCombatActive) || (_enemyAnimController != null && _enemyAnimController.IsReacting)) return false;
        if (!_animator.HasState(0, pattern.StateHash) || !_attacker.CanPerformAttack(pattern.Attack)) return false;
        bool speedConfigured = false;
        foreach (var parameter in _animator.parameters)
            speedConfigured |= parameter.name == "EnemyComboPlaybackSpeed" && parameter.type == AnimatorControllerParameterType.Float;
        if (!speedConfigured) return false;
        EnsurePatternRunner();
        _attackCompletionPending = false;
        if (!_patternRunner.Begin(pattern)) return false;
        _mover?.SetAttackRootMotionScale(pattern.RootMotionScale);
        _attacker.DisableWeaponHitbox();
        _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.WindupSpeed);
        return true;
    }

    private bool StartPatternClip(EnemyAttackPattern pattern)
    {
        if (_dead || _animator == null || !_animator.isActiveAndEnabled ||
            (_game != null && !_game.IsCombatActive) || (_enemyAnimController != null && _enemyAnimController.IsReacting) ||
            !_animator.HasState(0, pattern.StateHash)) return false;
        // 中断直後に同じ旧StateがまだBlend中なら、そのEventを新しい段として受け付けない。
        var current = _animator.GetCurrentAnimatorStateInfo(0);
        if (current.fullPathHash == pattern.StateHash) return false;
        // コンボ中は向きを瞬間変更せず、開始時に決めた方向へ振り切る。
        return _attacker != null && _attacker.PerformAttack(pattern.Attack);
    }

    private void FinishAttackPattern(bool success)
    {
        ClearPatternExecution();
        _enemyAnimController?.InterruptAttackForHitReaction();
        _mover?.ReleaseMovementAfterAttack();
        if (success) _ai?.OnAttackFinished();
        else _ai?.OnActionFailed();
    }

    /// <summary>追撃/死亡/戦闘終了/強制中断でコンボ予約を消去。被弾の終了まで通常抽選は再開しない。</summary>
    public void CancelAttackPattern(bool resumeAI = false)
    {
        if (!IsAttackPatternActive) return;
        var pattern = _patternRunner.Pattern;
        _patternRunner.Cancel();
        ClearPatternExecution();
        _mover?.ReleaseMovementAfterAttack();
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            // 既存の強制中断入口と同じく、遅れて消費されるTriggerを残さない。
            if (pattern != null && pattern.Attack != null && _enemyAnimController != null && _enemyAnimController.HasTrigger(pattern.Attack.AnimatorTrigger))
                _animator.ResetTrigger(pattern.Attack.AnimatorTrigger);
            int idle = Animator.StringToHash("Base Layer.Idle");
            if (_animator.HasState(0, idle)) _animator.CrossFadeInFixedTime(idle, .08f, 0, 0f);
        }
        if (resumeAI) _ai?.OnActionFailed();
        else _ai?.OnActionCancelled();
    }

    private void ClearPatternExecution()
    {
        _pendingAction = null;
        _attackCompletionPending = false;
        _attackAnimationHash = 0;
        _attacker?.DisableWeaponHitbox();
        _mover?.EndAttackRootMotion();
        _mover?.SetAttackRootMotionScale(null);
    }

    private void ObserveAttackPatternAnimation()
    {
        if (!IsAttackPatternActive || _animator == null) return;
        bool next = _animator.IsInTransition(0);
        var state = next ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        if (!_patternRunner.OwnsAnimationState(state.fullPathHash)) return;
        var clips = next ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length > 0) _patternRunner.ObserveClipTime(state.normalizedTime * clips[0].clip.length, state.fullPathHash);
    }

    private bool AcceptPatternEvent(AnimationEvent evt)
    {
        if (evt == null || _dead || (_game != null && !_game.IsCombatActive) ||
            (_enemyAnimController != null && _enemyAnimController.IsReacting) || !IsAttackPatternActive) return false;
        // 低FPSで予兆終端を跨いだフレームも、実Clip時刻で本体へ進めて初撃Eventを落とさない。
        _patternRunner.ObserveClipTime(evt.time, evt.animatorStateInfo.fullPathHash);
        return _patternRunner.AcceptsState(evt.animatorStateInfo.fullPathHash);
    }

    public void AnimEvent_ComboEnableWeaponHitbox(AnimationEvent evt)
    {
        if (AcceptPatternEvent(evt) && _patternRunner.OpenWindow(evt.intParameter, evt.animatorStateInfo.fullPathHash)) _attacker?.EnableWeaponHitbox();
    }

    public void AnimEvent_ComboDisableWeaponHitbox(AnimationEvent evt)
    {
        if (AcceptPatternEvent(evt)) _patternRunner.CloseWindow(evt.intParameter, evt.animatorStateInfo.fullPathHash);
    }

    public void AnimEvent_ComboFinished(AnimationEvent evt)
    {
        if (!AcceptPatternEvent(evt)) return;
        if (_patternRunner.EndClip(evt.animatorStateInfo.fullPathHash))
        {
            _attackAnimationHash = 0;
            _mover?.EndAttackRootMotion();
        }
    }

    public void AnimEvent_ComboSound(AnimationEvent evt)
    {
        if (AcceptPatternEvent(evt) && _patternRunner.WindowActive && _patternRunner.WindowIndex == evt.intParameter) _audio?.PlaySE(evt.stringParameter);
    }
}
