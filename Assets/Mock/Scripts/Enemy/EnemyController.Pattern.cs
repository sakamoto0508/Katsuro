using UnityEngine;

/// <summary>敵のPattern進行と既存Animator・武器・移動の接続。単発攻撃の実行経路とは独立。</summary>
public partial class EnemyController
{
    private EnemyAttackPatternRunner _patternRunner;
    private Transform _attackTarget;
    private readonly System.Collections.Generic.Dictionary<EnemyAttackPattern, float> _patternCooldowns = new();
    public bool CanSelectAttackCandidate(EnemyDecisionConfig.ActionWeight candidate) => candidate.Pattern == null || CanUseAttackPattern(candidate.Pattern);
    private bool CanUseAttackPattern(EnemyAttackPattern pattern)
    {
        if (pattern == null || !pattern.IsValid || (_patternCooldowns.TryGetValue(pattern, out float ready) && Time.time < ready)) return false;
        if (_animator == null || _attacker == null || !_animator.HasState(0, pattern.StateHash) || !_attacker.CanPerformAttack(pattern.Attack)) return false;
        if (_attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, _attackTarget.position);
            if (distance < pattern.MinAttackDistance || distance > pattern.MaxAttackDistance) return false;
        }
        return !pattern.IsWarp || TryFindWarpDestination(pattern, out _);
    }
    public bool IsAttackPatternActive => _preparingWarp != null || (_patternRunner != null && _patternRunner.IsActive);

    private void EnsurePatternRunner()
    {
        if (_patternRunner == null)
            _patternRunner = new EnemyAttackPatternRunner(StartPatternClip,
                () => _attacker?.DisableWeaponHitbox(), FinishAttackPattern,
                pattern => _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.UsesDashTuning ? pattern.DashPlaybackSpeed : pattern.PlaybackSpeed));
    }

    private bool BeginAttackPattern(EnemyAttackPattern pattern)
    {
        if (IsTraining || _dead || pattern == null || !pattern.IsValid || _animator == null || _attacker == null ||
            (_game != null && !_game.IsCombatActive) || (_enemyAnimController != null && _enemyAnimController.IsReacting)) return false;
        if (!_animator.HasState(0, pattern.StateHash) || !_attacker.CanPerformAttack(pattern.Attack)) return false;
        if (!CanUseAttackPattern(pattern)) return false;
        bool speedConfigured = false;
        foreach (var parameter in _animator.parameters)
            speedConfigured |= parameter.name == "EnemyComboPlaybackSpeed" && parameter.type == AnimatorControllerParameterType.Float;
        if (!speedConfigured) return false;
        EnsurePatternRunner();
        _attackCompletionPending = false;
        if (IsAttackPatternActive) return false;
        if (pattern.IsWarp && !BeginWarpPreparation(pattern)) return false;
        if (!pattern.IsWarp && !_patternRunner.Begin(pattern)) return false;
        _mover?.SetAttackRootMotionScale(pattern.RootMotionScale);
        _dashPreviousClipTime = 0f;
        if (pattern.UsesDashTuning) _mover?.SetAttackRootMotionScale(0f);
        _mover?.SetAttackTravelLimit(pattern.MaximumTravelDistance);
        _patternCooldowns[pattern] = Time.time + pattern.Cooldown;
        _attacker.DisableWeaponHitbox();
        _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.IsWarp ? .1f / Mathf.Max(.1f, pattern.WindupDuration) : pattern.WindupSpeed);
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
        bool started = _attacker != null && _attacker.PerformAttack(pattern.Attack);
        if (started)
        {
            ApplyPatternWeaponGrip(pattern);
            _attacker.SetSweptHits(pattern.UsesDashTuning);
        }
        return started;
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
        var pattern = _preparingWarp != null ? _preparingWarp : _patternRunner.Pattern;
        _preparingWarp = null;
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
        RestorePatternWeaponGrip();
        _pendingAction = null;
        _attackCompletionPending = false;
        _attackAnimationHash = 0;
        _attacker?.DisableWeaponHitbox();
        _attacker?.SetSweptHits(false);
        _mover?.EndAttackRootMotion();
        _mover?.SetAttackRootMotionScale(null);
        _mover?.SetAttackTravelLimit(0f);
        _dashPreviousClipTime = 0f;
        if (_animator != null) _animator.SetFloat("EnemyComboPlaybackSpeed", 1f);
        ClearWarpPresentation();
    }

    private void ObserveAttackPatternAnimation()
    {
        if (!IsAttackPatternActive || _animator == null) return;
        bool next = _animator.IsInTransition(0);
        var state = next ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        if (!_patternRunner.OwnsAnimationState(state.fullPathHash)) return;
        var clips = next ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length > 0)
        {
            float time = state.normalizedTime * clips[0].clip.length;
            _patternRunner.ObserveClipTime(time, state.fullPathHash);
            UpdateDashPlayback(time);
        }
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
        if (AcceptPatternEvent(evt) && _patternRunner.OpenWindow(evt.intParameter, evt.animatorStateInfo.fullPathHash))
        {
            MaintainPatternWeaponGrip();
            _attacker?.EnableWeaponHitbox();
            OnWarpHitboxOpened(_patternRunner.Pattern);
        }
    }

    public void AnimEvent_ComboDisableWeaponHitbox(AnimationEvent evt)
    {
        if (!AcceptPatternEvent(evt)) return;
        if (_patternRunner.Pattern.UsesDashTuning && _patternRunner.WindowActive && _patternRunner.WindowIndex == evt.intParameter)
        {
            MaintainPatternWeaponGrip();
            _attacker?.FlushSweptHits();
        }
        _patternRunner.CloseWindow(evt.intParameter, evt.animatorStateInfo.fullPathHash);
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
    public void AnimEvent_StopAttackRootMotion(AnimationEvent evt)
    {
        if (!AcceptPatternEvent(evt)) return;
        if (_patternRunner.Pattern.UsesDashTuning) return; // Final evaluated delta must reach FixedUpdate; subsequent scale is zero.
        _attackAnimationHash = 0; _mover?.EndAttackRootMotion();
    }

    private float _dashPreviousClipTime;

    // Only the Rush state speed parameter changes; global Animator.speed remains owned by HitStop.
    private void UpdateDashPlayback(float time)
    {
        var pattern = _patternRunner?.Pattern;
        if (pattern == null || !pattern.UsesDashTuning) return;
        float speed = time < pattern.WindupEndTime - .00001f ? pattern.WindupSpeed :
            time < pattern.DashEndTime - .00001f ? pattern.DashPlaybackSpeed : pattern.PlaybackSpeed;
        _animator.SetFloat("EnemyComboPlaybackSpeed", speed);
    }

    public void AnimEvent_DashRecovery(AnimationEvent evt)
    {
        if (AcceptPatternEvent(evt)) UpdateDashPlayback(evt.time);
    }

    // OnAnimatorMove consumes the last evaluated segment, including the final event-crossing frame.
    // Weight boundary-spanning samples rather than dropping the final dash delta at low FPS.
    private void PrepareDashRootMotion()
    {
        var pattern = _patternRunner?.Pattern;
        if (pattern == null || !pattern.UsesDashTuning || _animator == null) return;
        bool next = _animator.IsInTransition(0);
        var state = next ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != pattern.StateHash) return;
        var clips = next ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0) return;
        float time = state.normalizedTime * clips[0].clip.length;
        float dt = time - _dashPreviousClipTime;
        float overlap = Mathf.Max(0f, Mathf.Min(time, pattern.DashEndTime) - Mathf.Max(_dashPreviousClipTime, pattern.WindupEndTime));
        _mover?.SetAttackRootMotionScale(dt > .000001f ? pattern.DashRootMotionScale * overlap / dt : 0f);
        _dashPreviousClipTime = time;
        UpdateDashPlayback(time);
    }
}
