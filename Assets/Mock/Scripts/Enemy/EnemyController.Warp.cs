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
        PlayWarpCue(pattern, false);
        _preparingWarp = null;
        _animator.SetFloat("EnemyComboPlaybackSpeed", pattern.WindupSpeed);
        if (!_patternRunner.Begin(pattern)) { ClearPatternExecution(); _mover.ReleaseMovementAfterAttack(); _ai?.OnActionFailed(); }
    }

    private void PlayWarpCue(EnemyAttackPattern pattern, bool disappear)
    {
        _audio?.PlaySE(disappear ? pattern.DisappearSound : pattern.ReappearSound, .6f);
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
    }
}
