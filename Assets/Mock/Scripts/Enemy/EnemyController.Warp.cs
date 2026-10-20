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
    private bool _warpAppearStarted, _warpSlashPending;

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
        _warpAppearStarted = _warpSlashPending = false;
        _animator.CrossFadeInFixedTime(departure, .08f, 0, 0);
        return true;
    }

    private void TickAttackPattern(float deltaTime)
    {
        if (_preparingWarp == null)
        {
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
            if (_warpElapsed < pattern.WindupDuration) return;
            // 先に検証。候補が無効なら見えるまま中止する。
            if (!TryFindWarpDestination(pattern, out _)) { CancelAttackPattern(true); return; }
            PlayWarpCue(pattern, true);
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                _warpRenderers[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
            _warpHidden = true;
            _warpElapsed = 0f;
            return;
        }
        if (_warpElapsed < pattern.DisappearDuration)
        {
            // 実体が現れる少し前から、出現予定地点で青白い光を収束させる。
            float lead = Mathf.Min(pattern.WarpAppearLead, pattern.DisappearDuration);
            if (!_warpAppearStarted && pattern.WarpAppearVFX != null && _warpElapsed >= pattern.DisappearDuration - lead &&
                TryFindWarpDestination(pattern, out var upcoming))
                PlayWarpAppear(pattern, upcoming);
            return;
        }
        // 消失中にPlayerが動いても、現在の背後を再検証してからWarpする。
        if (!TryFindWarpDestination(pattern, out var destination) || !_mover.TryTeleportDuringAttack(destination))
        { CancelAttackPattern(true); return; }
        CompleteWarpReappearance(pattern);
    }

    private void CompleteWarpReappearance(EnemyAttackPattern pattern)
    {
        RestoreWarpRenderers();
        _mover.FacePlayerInstant();
        PlayWarpCue(pattern, false);
        _warpSlashPending = pattern.WarpSlashVFX != null;
        _preparingWarp = null;
        _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.WindupSpeed);
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
        _warpAppearStarted = _warpSlashPending = false;
    }

    /// <summary>Enemyと一緒に、使い回していたワープVFXのインスタンスも破棄する。</summary>
    private void DestroyWarpVFX()
    {
        foreach (var vfx in new[] { _warpVanish, _warpAppear, _warpSlash }) if (vfx != null) Destroy(vfx.gameObject);
    }
}
