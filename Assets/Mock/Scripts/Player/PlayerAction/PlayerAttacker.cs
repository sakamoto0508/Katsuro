using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アニメーション制御・ヒットボックス制御・ダメージ算出を一手に担う攻撃マネージャー。
/// </summary>
public sealed class PlayerAttacker : IDisposable
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName,
        PlayerWeapon playerWeapon, PlayerStatus status, PlayerPassiveBuffSet passiveBuffSet,
        Transform ownerTransform, PlayerResource playerResource = null)
    {
        _animController = animController;
        _animName = animName;
        _weapon = playerWeapon;
        _status = status;
        _ownerTransform = ownerTransform;
        _playerResource = playerResource;

        ApplyPassiveBuffSet(passiveBuffSet);
        _weapon?.RegisterHitObserver(HandleWeaponHit);
    }

    /// <summary>ステートコンテキストを後から注入する（ResolveDamageAmount で JustAvoidStacks を参照するため）。</summary>
    public void SetContext(PlayerStateContext context) => _context = context;

    /// <summary>抜刀済みで攻撃に移行できる状態か。</summary>
    public bool IsSwordReady => _isSwordReady;

    /// <summary>抜刀アニメ再生中かどうか。</summary>
    public bool IsDrawingSword => _isDrawingSword;

    private readonly PlayerAnimationController _animController;
    private readonly AnimationName _animName;
    private readonly PlayerWeapon _weapon;
    private readonly PlayerStatus _status;
    private readonly Transform _ownerTransform;
    private readonly PlayerResource _playerResource;
    private PlayerPassiveBuffSet _passiveBuffSet;
    private PlayerStateContext _context;
    private int _currentComboStep;
    private bool _currentIsLockOnVariant;
    private bool _currentIsStrongAttack;
    private bool _hasDrawnSword;
    private bool _isSwordReady;
    private bool _isDrawingSword;
    private bool _isHitboxActive;
    private readonly HashSet<int> _hitTargets = new();
    private float _currentClipDamage;

    /// <summary>抜刀アニメを再生し、完了イベントで攻撃準備完了に遷移する。</summary>
    public void DrawSword()
    {
        if (_isSwordReady || _isDrawingSword)
        {
            return;
        }

        _isDrawingSword = true;
        _hasDrawnSword = true;
        _context?.Mover.SetDrawingSword(true);
        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
        {
            _animController?.PlayBool(_animName.IsDrawingSword, true);
        }
    }

    /// <summary>アニメイベントから呼ばれ、抜刀状態のフラグを更新する。</summary>
    public void CompleteDrawSword()
    {
        if (!_isDrawingSword)
        {
            return;
        }

        // まずアニメータ側に "抜刀済み" フラグを立ててから描画フラグをクリアする。
        // これによりアニメータ内の遷移条件が先に満たされ、抜刀済みの Idle へ遷移しやすくなる。
        if (!string.IsNullOrEmpty(_animName?.IsSwordDrawn))
        {
            _animController?.PlayBool(_animName.IsSwordDrawn, true);
        }

        _isDrawingSword = false;
        _isSwordReady = true;
        _context?.Mover.SetDrawingSword(false);
        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
        {
            _animController?.PlayBool(_animName.IsDrawingSword, false);
        }
        Debug.Log($"PlayerAttacker: CompleteDrawSword called. IsSwordReady={_isSwordReady}");
    }

    public void PlayLightAttack() => PlayLightAttack(0, false);
    public void PlayLightAttack(int comboStep) => PlayLightAttack(comboStep, false);

    /// <summary>ロックオン有無に応じたライト攻撃アニメを再生する。</summary>
    public void PlayLightAttack(int comboStep, bool isLockOnVariant)
    {
        _currentIsStrongAttack = false;
        _currentComboStep = comboStep;
        _currentIsLockOnVariant = isLockOnVariant;
        ApplyLockOnFlag(isLockOnVariant);
        // set clip-specific flat damage from config
        _currentClipDamage = _context?.StateConfig?.GetLightAttackClipDamage(isLockOnVariant, comboStep) ?? 0f;
        PlayAttackTrigger(_animName?.LightAttack, comboStep);
    }

    public void PlayStrongAttack() => PlayStrongAttack(0);
    public void PlayStrongAttack(int comboStep)
    {
        _currentIsStrongAttack = true;
        _currentComboStep = comboStep;
        _currentIsLockOnVariant = false;
        _currentClipDamage = _context?.StateConfig?.GetStrongAttackClipDamage(comboStep) ?? 0f;
        PlayAttackTrigger(_animName?.StrongAttack, comboStep);
    }

    public void PlayStrongAttackInternal(int comboStep)
    {
        _currentIsStrongAttack = true;
        _currentComboStep = comboStep;
        _currentIsLockOnVariant = false;
        PlayAttackTrigger(_animName?.StrongAttack, comboStep);
    }

    /// <summary>攻撃終了時にヒットボックスを確実にオフにする。</summary>
    public void EndAttack()
    {
        DisableWeaponHitbox();
    }

    /// <summary>攻撃フレームに合わせてヒットボックスを有効化し、ヒット済み管理を初期化。</summary>
    public void EnableWeaponHitbox()
    {
        _hitTargets.Clear();
        _isHitboxActive = true;
        _weapon?.EnableHitbox();
    }

    /// <summary>ヒットボックスを無効化し、新規ヒットを発生させない。</summary>
    public void DisableWeaponHitbox()
    {
        _isHitboxActive = false;
        _weapon?.DisableHitbox();
    }

    /// <summary>装備セットを差し替え、以後のダメージ計算に反映する。</summary>
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

    /// <summary>コンボ段数を Animator パラメーターへ設定する。</summary>
    private void ApplyComboStep(int comboStep)
    {
        if (string.IsNullOrEmpty(_animName?.ComboStep))
        {
            return;
        }

        _animController?.SetInteger(_animName.ComboStep, comboStep);
    }

    /// <summary>ロックオン状態を Animator へ伝える。</summary>
    private void ApplyLockOnFlag(bool isLockOn)
    {
        if (string.IsNullOrEmpty(_animName?.IsLockOn))
        {
            return;
        }

        _animController?.PlayBool(_animName.IsLockOn, isLockOn);
    }

    /// <summary>武器コライダーにヒットした相手へ一度だけダメージを適用する。</summary>
    private void HandleWeaponHit(Collider other)
    {
        // 無効状態や null なら終了（デバッグログを出す）
        if (!_isHitboxActive || other == null)
        {
            return;
        }
        // 自分自身(プレイヤー)へのヒットは無視
        if (_ownerTransform != null && other.transform.IsChildOf(_ownerTransform))
        {
            return;
        }
        // 対象がダメージを受けられない（環境コライダー等はここで弾く）
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        // 既に当たったコライダーなら無視（ダメージ対象のみを記録）
        int instanceId = other.GetInstanceID();
        if (!_hitTargets.Add(instanceId))
        {
            return;
        }
        // 攻撃者起点と命中位置・法線を算出し、安定しない場合は forward をフォールバックとする
        Vector3 origin = _ownerTransform != null ? _ownerTransform.position : other.bounds.center;
        Vector3 hitPoint = other.ClosestPoint(origin);
        Vector3 hitNormal = (hitPoint - origin).normalized;

        if (hitNormal.sqrMagnitude < 0.0001f)
        {
            hitNormal = _ownerTransform != null ? _ownerTransform.forward : Vector3.forward;
        }
        // ダメージ情報を生成して IDamageable へ通知、続けてパッシブ固有エフェクトを再生。
        float damage = ResolveDamageAmount();
        DamageInfo damageInfo = new DamageInfo(damage, hitPoint, hitNormal,
            _ownerTransform != null ? _ownerTransform.gameObject : null, other);

        // Debug: ログ出力（ダメージが発生する場合）
        if (damage > 0f)
        {
            Debug.Log($"PlayerAttacker: Hit target={other.gameObject.name} damage={damage}");
        }

        damageable.ApplyDamage(damageInfo);
        if (HitStopManager.Instance != null && other != null)
        {
            HitStopManager.Instance.PlayHitStop(HitStopManager.Instance.HitStopTime, other.gameObject);
            Debug.Log($"PlayerAttacker: Played hit stop for target={other.gameObject.name}");
        }
        SpawnPassiveEffects(in damageInfo);
    }

    /// <summary>
    /// 基礎ダメージを解決する。低HPバフが設定されており PlayerResource が渡されていれば適用する。
    /// さらに PlayerStateContext の Just-Avoid スタックが設定されている場合、そのスタック分の攻撃ボーナスを適用します。
    /// </summary>
    private float ResolveDamageAmount()
    {
        // 基礎ダメージ
        float baseDamage = _status?.AttackPower ?? 0f;

        // パッシブ乗算・加算
        float passiveMult = _passiveBuffSet != null ? _passiveBuffSet.EvaluateDamageMultiplier() : 1f;
        float passiveFlat = _passiveBuffSet != null ? _passiveBuffSet.EvaluateFlatDamageBonus() : 0f;
        // include clip flat damage as additive component
        float clipFlat = _currentClipDamage;
        float additive = baseDamage + passiveFlat + clipFlat; // (base + equip(flat) + clip)
        float afterPassive = additive * passiveMult; // apply passive multiplier

        // 低HPバフ乗算
        float lowHpMult = 1f;
        float currentHpRatio = 1f;
        if (_playerResource != null && _playerResource.MaxHp > 0f)
        {
            currentHpRatio = Mathf.Clamp01(_playerResource.CurrentHp / _playerResource.MaxHp);
            if (_status?.LowHpBuffTable != null)
            {
                lowHpMult = _status.LowHpBuffTable.EvaluateDamageMultiplier(currentHpRatio);
            }
        }
        float afterLowHp = afterPassive * lowHpMult;

        // ジャスト回避スタック乗算
        float justMult = 1f;
        if (_context != null && _status?.JustAvoidBuffConfig != null)
        {
            int stacks = Mathf.Clamp(_context.JustAvoidStacks, 0, _status.JustAvoidBuffConfig.MaxStacks);
            if (stacks > 0)
            {
                float perStack = _status.JustAvoidBuffConfig.DamageMultiplierPerStack;
                justMult = 1f + perStack * stacks;
            }
        }

        float final = Mathf.Max(0f, afterLowHp * justMult);
        return final;
    }

    /// <summary>ヒット時エフェクトを必要に応じて発生させる。</summary>
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
            // エフェクトの向きは法線方向に合わせるが、法線が不安定な場合は回転なしで生成する。
            Quaternion rotation = damageInfo.HitNormal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(damageInfo.HitNormal)
                : Quaternion.identity;

            UnityEngine.Object.Instantiate(entry.OnHitEffectPrefab, damageInfo.HitPoint, rotation);
        }
    }
}
