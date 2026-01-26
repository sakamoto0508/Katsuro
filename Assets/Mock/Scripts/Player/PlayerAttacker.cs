using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの攻撃アニメーションや武器ヒット判定を統括するクラス。
/// 抜刀状態の管理や、アニメーション用トリガー発行を担当する。
/// </summary>
public sealed class PlayerAttacker : IDisposable
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName,
        PlayerWeapon playerWeapon, PlayerStatus status, PlayerPassiveBuffSet passiveBuffSet, Transform ownerTransform)
    {
        _animController = animController;
        _animName = animName;
        _weapon = playerWeapon;
        _status = status;
        _ownerTransform = ownerTransform;

        ApplyPassiveBuffSet(passiveBuffSet);
        _weapon?.RegisterHitObserver(HandleWeaponHit);
    }

    public bool IsSwordReady => _isSwordReady;
    public bool IsDrawingSword => _isDrawingSword;

    private readonly PlayerAnimationController _animController;
    private readonly AnimationName _animName;
    private readonly PlayerWeapon _weapon;
    private readonly PlayerStatus _status;
    private readonly Transform _ownerTransform;
    private PlayerPassiveBuffSet _passiveBuffSet;
    private bool _isSwordReady;
    private bool _isDrawingSword;
    private bool _isHitboxActive;
    private readonly HashSet<int> _hitTargets = new();

    public void DrawSword()
    {
        if (_isSwordReady || _isDrawingSword)
        {
            return;
        }

        _isDrawingSword = true;

        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
        {
            _animController?.PlayTrigger(_animName.IsDrawingSword);
        }
    }

    public void CompleteDrawSword()
    {
        if (!_isDrawingSword)
        {
            return;
        }

        _isDrawingSword = false;
        _isSwordReady = true;
    }

    public void PlayLightAttack() => PlayLightAttack(0, false);

    public void PlayLightAttack(int comboStep) => PlayLightAttack(comboStep, false);

    public void PlayLightAttack(int comboStep, bool isLockOnVariant)
    {
        ApplyLockOnFlag(isLockOnVariant);
        PlayAttackTrigger(_animName?.LightAttack, comboStep);
    }

    public void PlayStrongAttack() => PlayStrongAttack(0);

    public void PlayStrongAttack(int comboStep) => PlayAttackTrigger(_animName?.StrongAttack, comboStep);

    public void EndAttack()
    {
        DisableWeaponHitbox();
    }

    public void EnableWeaponHitbox()
    {
        _hitTargets.Clear();
        _isHitboxActive = true;
        _weapon?.EnableHitbox();
    }

    public void DisableWeaponHitbox()
    {
        _isHitboxActive = false;
        _weapon?.DisableHitbox();
    }

    public void ApplyPassiveBuffSet(PlayerPassiveBuffSet passiveBuffSet)
    {
        _passiveBuffSet = passiveBuffSet;
    }

    public void Dispose()
    {
        _weapon?.UnregisterHitObserver(HandleWeaponHit);
        _hitTargets.Clear();
        _isHitboxActive = false;
    }

    private void PlayAttackTrigger(string triggerName, int comboStep)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning("PlayerAttacker: Attack trigger is not assigned.");
            return;
        }

        ApplyComboStep(comboStep);
        _animController?.PlayTrigger(triggerName);
    }

    private void ApplyComboStep(int comboStep)
    {
        if (string.IsNullOrEmpty(_animName?.ComboStep))
        {
            return;
        }

        _animController?.SetInteger(_animName.ComboStep, comboStep);
    }

    private void ApplyLockOnFlag(bool isLockOn)
    {
        if (string.IsNullOrEmpty(_animName?.IsLockOn))
        {
            return;
        }

        _animController?.PlayBool(_animName.IsLockOn, isLockOn);
    }

    private void HandleWeaponHit(Collider other)
    {
        if (!_isHitboxActive || other == null)
        {
            return;
        }

        if (_ownerTransform != null && other.transform.IsChildOf(_ownerTransform))
        {
            return;
        }

        int instanceId = other.GetInstanceID();
        if (!_hitTargets.Add(instanceId))
        {
            return;
        }

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        Vector3 origin = _ownerTransform != null ? _ownerTransform.position : other.bounds.center;
        Vector3 hitPoint = other.ClosestPoint(origin);
        Vector3 hitNormal = (hitPoint - origin).normalized;

        if (hitNormal.sqrMagnitude < 0.0001f)
        {
            hitNormal = _ownerTransform != null ? _ownerTransform.forward : Vector3.forward;
        }

        DamageInfo damageInfo = new DamageInfo(ResolveDamageAmount(), hitPoint, hitNormal,
            _ownerTransform != null ? _ownerTransform.gameObject : null, other);

        damageable.ApplyDamage(damageInfo);
        SpawnPassiveEffects(in damageInfo);
    }

    private float ResolveDamageAmount()
    {
        float damage = _status?.AttackPower ?? 0f;

        if (_passiveBuffSet != null)
        {
            damage *= _passiveBuffSet.EvaluateDamageMultiplier();
            damage += _passiveBuffSet.EvaluateFlatDamageBonus();
        }

        return Mathf.Max(0f, damage);
    }

    private void SpawnPassiveEffects(in DamageInfo damageInfo)
    {
        if (_passiveBuffSet?.Buffs == null)
        {
            return;
        }

        foreach (var entry in _passiveBuffSet.Buffs)
        {
            if (entry == null || entry.OnHitEffectPrefab == null)
            {
                continue;
            }

            Quaternion rotation = damageInfo.HitNormal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(damageInfo.HitNormal)
                : Quaternion.identity;

            UnityEngine.Object.Instantiate(entry.OnHitEffectPrefab, damageInfo.HitPoint, rotation);
        }
    }
}
