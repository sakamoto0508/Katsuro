using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyController
{
    private EnemyAttackPattern _preparingWarp;
    private float _warpElapsed;
    private bool _warpHidden;
    private readonly Dictionary<Renderer, bool> _warpRenderers = new();
    private readonly List<GameObject> _warpEffects = new();
    private KatsuroTimedVFX _warpVanish, _warpAppear, _warpSlash;
    private bool _warpAppearStarted, _warpSlashPending, _warpVanishStarted, _warpFormStarted;
    private WarpGhostPresenter _warpGhost;

    /// <summary>黒い幽霊の表現（体の揺らぎ・影の崩れ・影からの形成）。設定で無効なら null。</summary>
    private WarpGhostPresenter WarpGhost(EnemyAttackPattern pattern)
    {
        if (pattern == null || pattern.WarpGhost == null || !pattern.WarpGhost.Enabled) return null;
        if (_warpGhost == null)
        {
            _warpGhost = gameObject.AddComponent<WarpGhostPresenter>();
            _warpGhost.Initialize(pattern.WarpGhost);
        }
        return _warpGhost;
    }

    private bool TryFindWarpDestination(EnemyAttackPattern pattern, out Vector3 target)
    {
        target = default;
        return _mover != null && _mover.CanTeleportDuringAttack &&
            EnemyWarpDestination.TryFind(_attackTarget, GetComponent<CapsuleCollider>(), GetComponent<NavMeshAgent>(), pattern, out target);
    }

    private bool BeginWarpPreparation(EnemyAttackPattern pattern)
    {
        int departure = Animator.StringToHash("Base Layer.Attacks.WarpDeparture");
        if (!TryFindWarpDestination(pattern, out _) || !_animator.HasState(0, departure)) return false;
        _preparingWarp = pattern;
        _warpElapsed = 0f;
        _warpHidden = false;
        _warpAppearStarted = _warpSlashPending = _warpVanishStarted = _warpFormStarted = false;
        _animator.CrossFadeInFixedTime(departure, .08f, 0, 0);
        // 予兆：刀を挙げて溜める間に、体へ黒い揺らぎを乗せていく。
        WarpGhost(pattern)?.BeginWarning(pattern.WarpDepartureDuration);
        return true;
    }

    private static readonly int WarpDepartureHash = Animator.StringToHash("Base Layer.Attacks.WarpDeparture");
    private float _warpStrikeHold = -1f;

    /// <summary>
    /// 出現後の構え：刀を挙げ切った姿勢で Animator の再生速度を0にして待ち、残り時間が尽きたら通常速度に戻して振り下ろす。
    /// timeScale には触れない。万一 Clip が頭から再生し直されていたら、挙げ切った姿勢へ戻す。
    /// </summary>
    private void TickWarpStrikeHold(float deltaTime)
    {
        if (_warpStrikeHold < 0f) return;
        var pattern = _patternRunner != null ? _patternRunner.Pattern : null;
        if (pattern == null || !pattern.IsWarp || !IsAttackPatternActive) { _warpStrikeHold = -1f; return; }
        var state = _animator.GetCurrentAnimatorStateInfo(0);
        var clips = _animator.GetCurrentAnimatorClipInfo(0);
        if (state.fullPathHash == pattern.StateHash && clips.Length > 0 && !_animator.IsInTransition(0))
        {
            float length = clips[0].clip.length;
            if (state.normalizedTime * length < pattern.WarpRaiseTime - .03f)
                _animator.Play(pattern.StateHash, 0, pattern.WarpRaiseTime / Mathf.Max(.01f, length));
        }
        _warpStrikeHold -= deltaTime;
        if (_warpStrikeHold > 0f) { _animator.SetFloat("EnemyComboPlaybackSpeed", 0f); return; }
        _warpStrikeHold = -1f;
        _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.PlaybackSpeed);
    }

    /// <summary>WarpDeparture（WarpAttack と同じ Clip）が刀を挙げ切る時刻に達したら再生を止め、その姿勢で溜める。</summary>
    private void HoldWarpRaise(EnemyAttackPattern pattern)
    {
        bool next = _animator.IsInTransition(0);
        var state = next ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != WarpDepartureHash) return;
        var clips = next ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0) return;
        float clipTime = state.normalizedTime * clips[0].clip.length;
        // The Animator advances after Update, so stop one frame early to land on the raised pose rather than past it.
        if (clipTime + Time.deltaTime >= pattern.WarpRaiseTime) _animator.SetFloat("EnemyComboPlaybackSpeed", 0f);
    }

    private void TickAttackPattern(float deltaTime)
    {
        if (_preparingWarp == null)
        {
            TickWarpStrikeHold(deltaTime);
            ObserveAttackPatternAnimation();
            _patternRunner?.Tick(deltaTime);
            return;
        }
        if (!float.IsFinite(deltaTime) || deltaTime <= 0) return;
        var pattern = _preparingWarp;
        if (!pattern.IsValid || _animator == null || !_animator.isActiveAndEnabled) { CancelAttackPattern(true); return; }
        _warpElapsed += deltaTime;
        if (!_warpHidden)
        {
            if (!_warpVanishStarted)
            {
                // 刀を挙げ切ったら止めて「間」を取る（今から攻撃するという予兆）。
                HoldWarpRaise(pattern);
                if (_warpElapsed < pattern.WarpDepartureDuration) return;
                if (!TryFindWarpDestination(pattern, out _)) { CancelAttackPattern(true); return; }
                PlayWarpCue(pattern, true);
                _warpVanishStarted = true;
                _warpElapsed = 0f;
                return;
            }
            if (_warpElapsed < (pattern.UsesArtWarpVFX ? pattern.WarpVanishLead : 0f)) return;
            // 先に検証。候補が無効なら見えるまま中止する。
            if (!TryFindWarpDestination(pattern, out _)) { CancelAttackPattern(true); return; }
            // 消失：今の姿勢を黒い影に焼き付けて足元から崩す。実体はこの直後に隠す。
            WarpGhost(pattern)?.Vanish();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                _warpRenderers[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
            _warpHidden = true;
            _warpElapsed = 0f;
            return;
        }
        // 出現前：出現先で黒い影が足元から形を取り始める（青白い収束光より少し早い）。
        var ghost = WarpGhost(pattern);
        float formLead = ghost != null ? Mathf.Min(pattern.WarpGhost.FormLead, pattern.DisappearDuration) : 0f;
        if (ghost != null && !_warpFormStarted && formLead > 0f && _warpElapsed >= pattern.DisappearDuration - formLead &&
            TryFindWarpDestination(pattern, out var formAt))
        {
            Vector3 toTarget = _attackTarget != null ? _attackTarget.position - formAt : transform.forward;
            toTarget.y = 0f;
            var facing = toTarget.sqrMagnitude > .0001f ? Quaternion.LookRotation(toTarget.normalized, Vector3.up) : transform.rotation;
            ghost.Form(formAt, facing, pattern.DisappearDuration - _warpElapsed);
            _warpFormStarted = true;
        }
        // A long frame can skip the entire lead window. Start convergence before
        // testing reappearance, and preserve its lead time even in that case.
        float lead = Mathf.Min(pattern.WarpAppearLead, pattern.DisappearDuration);
        if (!_warpAppearStarted && pattern.WarpAppearVFX != null &&
            _warpElapsed >= pattern.DisappearDuration - lead &&
            TryFindWarpDestination(pattern, out var upcoming))
        {
            PlayWarpAppear(pattern, upcoming);
            _warpElapsed = Mathf.Min(_warpElapsed, pattern.DisappearDuration - lead);
            if (lead > 0f) return;
        }
        if (_warpElapsed < pattern.DisappearDuration) return;
        // 消失中にPlayerが動いても、現在の背後を再検証してからWarpする。
        if (!TryFindWarpDestination(pattern, out var destination) || !_mover.TryTeleportDuringAttack(destination))
        { CancelAttackPattern(true); return; }
        CompleteWarpReappearance(pattern);
    }

    private void CompleteWarpReappearance(EnemyAttackPattern pattern)
    {
        RestoreWarpRenderers();
        _mover.FacePlayerInstant();
        // 出現：形成中の影を実体に替え、体に残った黒い揺らぎを少しずつ抜く。
        WarpGhost(pattern)?.Appear();
        PlayWarpCue(pattern, false);
        _warpSlashPending = pattern.WarpSlashVFX != null;
        _preparingWarp = null;
        // 出現後は刀を挙げた姿勢で完全に止めて構え（速度0）、構え時間が終わったら通常速度で振り下ろす。
        _animator.SetFloat("EnemyComboPlaybackSpeed", 0f);
        _warpStrikeHold = pattern.ReappearWindupDuration;
        if (!_patternRunner.Begin(pattern)) { ClearPatternExecution(); _mover.ReleaseMovementAfterAttack(); _ai?.OnActionFailed(); }
    }

    private void PlayWarpCue(EnemyAttackPattern pattern, bool disappear)
    {
        _audio?.PlaySE(disappear ? pattern.DisappearSound : pattern.ReappearSound, .6f);
        if (pattern.UsesArtWarpVFX)
        {
            if (disappear)
            {
                if (pattern.WarpVanishVFX != null)
                    EnsureWarpVFX(ref _warpVanish, pattern.WarpVanishVFX).Play(transform.position, transform.rotation);
            }
            else if (pattern.WarpAppearVFX != null)
            {
                // 収束が先に始まっていれば、再検証後の実位置へ合わせるだけ。
                if (_warpAppearStarted && _warpAppear != null && _warpAppear.IsPlaying) _warpAppear.MoveTo(WarpEffectPoint(pattern));
                else PlayWarpAppear(pattern, transform.position);
            }
            return;
        }
        if (pattern.WarpEffectPrefab == null) return;
        var effect = Instantiate(pattern.WarpEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.localScale *= .65f;
        foreach (var binder in effect.GetComponentsInChildren<UnityEngine.VFX.Utility.VFXBinderBase>(true))
        {
            var visual = binder.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (visual != null && binder.IsValid(visual)) binder.UpdateBinding(visual);
        }
        foreach (var visual in effect.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true)) { visual.Reinit(); visual.Play(); }
        foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true)) particles.Play();
        _warpEffects.Add(effect);
        Destroy(effect, 2f);
    }

    private void PlayWarpAppear(EnemyAttackPattern pattern, Vector3 feet)
    {
        Vector3 point = feet + Vector3.up * pattern.WarpEffectHeight;
        var view = Camera.main;
        var rotation = view != null ? KatsuroVFXOrientation.FacingCamera(point, view) : transform.rotation;
        EnsureWarpVFX(ref _warpAppear, pattern.WarpAppearVFX).Play(point, rotation);
        _warpAppearStarted = true;
    }

    /// <summary>出現後の最初の攻撃判定が開いた瞬間に一閃を出す。Warp以外・2回目以降の判定では何もしない。</summary>
    private void OnWarpHitboxOpened(EnemyAttackPattern pattern)
    {
        if (!_warpSlashPending || pattern == null || !pattern.IsWarp || pattern.WarpSlashVFX == null) return;
        _warpSlashPending = false;
        Vector3 point = WarpEffectPoint(pattern) + transform.forward * pattern.WarpSlashForward;
        Vector3 direction = Quaternion.AngleAxis(pattern.WarpSlashRoll, transform.forward) * transform.right;
        var view = Camera.main;
        var rotation = view != null ? KatsuroVFXOrientation.FacingCamera(point, direction, view)
            : Quaternion.LookRotation(-transform.forward, Vector3.Cross(-transform.forward, direction));
        EnsureWarpVFX(ref _warpSlash, pattern.WarpSlashVFX).Play(point, rotation, pattern.WarpSlashScale);
    }

    private Vector3 WarpEffectPoint(EnemyAttackPattern pattern) => transform.position + Vector3.up * pattern.WarpEffectHeight;

    private KatsuroTimedVFX EnsureWarpVFX(ref KatsuroTimedVFX instance, KatsuroTimedVFX prefab)
    {
        if (instance == null)
        {
            instance = Instantiate(prefab);
            instance.name = prefab.name;
            instance.Stop();
        }
        return instance;
    }

    private void RestoreWarpRenderers()
    {
        foreach (var pair in _warpRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
        _warpRenderers.Clear();
        _warpHidden = false;
    }
    private void ClearWarpPresentation()
    {
        _preparingWarp = null;
        RestoreWarpRenderers();
        foreach (var effect in _warpEffects) if (effect != null) Destroy(effect);
        _warpEffects.Clear();
        _warpElapsed = 0f;
        // 中断・被弾・死亡で出現光が浮いたまま残らないようにする。消失の黒霧は自然に消えるまで残す。
        if (_warpAppear != null) _warpAppear.Stop();
        // 予兆中・消失中の中断は揺らぎと形成中の影をすぐ消す。出現後に抜けていく揺らぎだけは自然に終わらせる。
        _warpGhost?.Interrupt();
        _warpAppearStarted = _warpSlashPending = _warpVanishStarted = _warpFormStarted = false;
        _warpStrikeHold = -1f;
    }

    /// <summary>Enemyと一緒に、使い回していたワープVFXのインスタンスも破棄する。</summary>
    private void DestroyWarpVFX()
    {
        foreach (var vfx in new[] { _warpVanish, _warpAppear, _warpSlash }) if (vfx != null) Destroy(vfx.gameObject);
    }
}
