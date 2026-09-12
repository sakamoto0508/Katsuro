using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の攻撃を制御するクラス。EnemyWeapon のヒットリレーを購読し、ヒット時にダメージを適用します。
/// </summary>
public class EnemyAttacker : IDisposable
{
    public EnemyAttacker(EnemyAnimationController aniController, EnemyAttackData[] attackData
        , EnemyWeapon[] weapons, EnemyStuts status, Transform owner)
    {
        _animController = aniController;
        _attackData = attackData;
        _weapons = weapons;
        _status = status;
        _ownerTransform = owner;

        // 各武器のリレーを購読してヒット通知を受ける
        if (_weapons != null)
        {
            foreach (var w in _weapons)
            {
                if (w == null) continue;
                Action<Collider> handler = (other) => HandleWeaponHit(other, w);
                _handlerMap[w] = handler;
                w.RegisterHitObserver(handler);
            }
        }
    }

    private EnemyAttackData[] _attackData;
    private EnemyAnimationController _animController;
    private EnemyWeapon[] _weapons;
    private readonly HashSet<IDamageable> _hitTargets = new();
    private bool _isHitboxActive;
    private HitStopManager _hitStop;
    private bool _initialized;
    public void Init(HitStopManager hitStop)
    {
        if (_initialized) return;
        _initialized = true;
        _hitStop = hitStop;
    }
    private readonly Transform _ownerTransform;
    private readonly EnemyStuts _status;
    private readonly Dictionary<EnemyWeapon, Action<Collider>> _handlerMap = new();

    /// <summary>攻撃を実行する。攻撃データに基づき Animator トリガーを発火し、武器にダメージ値を設定します。</summary>
    public void PerformAttack(EnemyActionType attackType)
    {
        var data = FindData(attackType);
        if (data == null)
        {
            Debug.LogWarning($"EnemyAttacker: no attack data found for action={attackType}");
            return;
        }


        if (_animController != null)
        {
            // Animator トリガー名をそのまま使ってトリガーを発火する
            if (!string.IsNullOrEmpty(data.AnimatorTrigger))
            {
                _animController.PlayTrigger(data.AnimatorTrigger);
            }
            else
            {
                Debug.LogWarning($"EnemyAttacker: attack data for {attackType} has no AnimatorTrigger assigned.");
            }
        }

        // 武器へダメージを設定（複数武器がある場合は hitboxIndex を使う）
        if (_weapons != null && data.HitboxIndex >= 0 && data.HitboxIndex < _weapons.Length)
        {
            _weapons[data.HitboxIndex].CurrentAttackDamage = data.Damage;
        }
        else
        {
            CombatLog.Trace($"EnemyAttacker: perform {attackType} damage={data.Damage} (no weapon assigned or invalid hitboxIndex={data.HitboxIndex})");
        }
    }

    private EnemyAttackData FindData(EnemyActionType action)
    {
        if (_attackData == null) return null;
        EnemyAttackData selected = null;
        int count = 0;
        foreach (var d in _attackData)
        {
            if (d == null || d.ActionType != action) continue;
            if (UnityEngine.Random.Range(0, ++count) == 0) selected = d;
        }
        // 同じ行動種別の攻撃データが複数ある場合は、ランダムに1つ選ぶ。
        return selected;
    }

    /// <summary>攻撃フレームに合わせてヒットボックスを有効化し、ヒット済み管理を初期化。</summary>
    public void EnableWeaponHitbox()
    {
        _hitTargets.Clear();
        _isHitboxActive = true;
        if (_weapons == null) return;
        foreach (var w in _weapons)
        {
            w?.EnableHitbox();
        }
    }

    /// <summary>ヒットボックスを無効化し、新規ヒットを発生させない。</summary>
    public void DisableWeaponHitbox()
    {
        _isHitboxActive = false;
        if (_weapons == null) return;
        foreach (var w in _weapons)
        {
            w?.DisableHitbox();
        }
    }

    /// <summary>武器コライダーにヒットした相手へ一度だけダメージを適用する（Weaponごとのハンドラで呼ばれる）。</summary>
    private void HandleWeaponHit(Collider other, EnemyWeapon sourceWeapon)
    {
        if (!_isHitboxActive || other == null) return;

        // 自分自身(敵)へのヒットは無視
        if (_ownerTransform != null && other.transform.IsChildOf(_ownerTransform)) return;


        var damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null) return;
        if (!_hitTargets.Add(damageable)) return;

        Vector3 origin = _ownerTransform != null ? _ownerTransform.position : other.bounds.center;
        Vector3 hitPoint = other.ClosestPoint(origin);
        Vector3 hitNormal = (hitPoint - origin).sqrMagnitude > 0.0001f ? (hitPoint - origin).normalized : Vector3.forward;

        float damage = sourceWeapon != null ? sourceWeapon.Damage() : (_status != null ? _status.EnemyPower : 0f);

        var owner = _ownerTransform != null ? _ownerTransform.GetComponent<EnemyController>() : null;
        damage *= RunSession.EnemyDamage(owner != null ? owner.HpRatio : 1f);
        DamageInfo damageInfo = new DamageInfo(damage, hitPoint, hitNormal, _ownerTransform != null ? _ownerTransform.gameObject : null, other);
        // Debug: 出力（誰がどれだけのダメージを誰に与えたか）
        CombatLog.Trace($"EnemyAttacker: Hit target={other.gameObject.name} damage={damage} instigator={_ownerTransform?.gameObject.name} hitPoint={hitPoint}");
        bool avoided = damageable is PlayerController player && player.IsInvulnerable;
        damageable.ApplyDamage(damageInfo);
        var go = other != null ? other.gameObject : null;
        if (!avoided && _hitStop != null && go != null)
        {
            _hitStop.PlayHitStop(_hitStop.HitStopTime, go);
            CombatLog.Trace($"EnemyAttacker: Played hit stop for {go.name} with duration={_hitStop.HitStopTime}");
        }
    }

    public void Dispose()
    {
        DisableWeaponHitbox();
        // 登録したハンドラを解除
        if (_weapons != null)
        {
            foreach (var w in _weapons)
            {
                if (w == null) continue;
                if (_handlerMap.TryGetValue(w, out var h))
                {
                    w.UnregisterHitObserver(h);
                }
            }
        }
        _handlerMap.Clear();
        _hitTargets.Clear();
    }
}
