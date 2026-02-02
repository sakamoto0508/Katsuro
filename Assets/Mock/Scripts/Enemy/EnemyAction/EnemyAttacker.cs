using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の攻撃を制御するクラス。EnemyWeapon のヒットリレーを購読し、ヒット時にダメージを適用します。
/// </summary>
public class EnemyAttacker : IDisposable
{
    public EnemyAttacker(Animator animator, EnemyAttackData[] attackData, EnemyWeapon[] weapons, EnemyStuts status, Transform owner)
    {
        _animator = animator;
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

    [Header("Attack Data")]
    [SerializeField] private EnemyAttackData[] _attackData;

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyWeapon[] _weapons;

    private readonly Transform _ownerTransform;
    private readonly EnemyStuts _status;

    // ヒット管理
    private bool _isHitboxActive;
    private HashSet<int> _hitTargets = new();

    // 登録したハンドラを保持して解除できるようにする
    private readonly Dictionary<EnemyWeapon, Action<Collider>> _handlerMap = new();

    /// <summary>攻撃を実行する。攻撃データに基づき Animator トリガーを発火し、武器にダメージ値を設定します。</summary>
    public void PerformAttack(EnemyActionType attackType)
    {
        var data = FindData(attackType);
        if (data == null)
        {
            Debug.LogWarning($"EnemyAttacker: no attack data for {attackType}");
            return;
        }

        // Animator トリガー
        if (_animator != null && !string.IsNullOrEmpty(data.animatorTrigger))
        {
            _animator.SetTrigger(data.animatorTrigger);
        }

        // 武器へダメージを設定（複数武器がある場合は hitboxIndex を使う）
        if (_weapons != null && data.hitboxIndex >= 0 && data.hitboxIndex < _weapons.Length)
        {
            _weapons[data.hitboxIndex].CurrentAttackDamage = data.damage;
        }
    }

    private EnemyAttackData FindData(EnemyActionType action)
    {
        if (_attackData == null) return null;
        foreach (var d in _attackData)
        {
            if (d != null && d.actionType == action) return d;
        }
        return null;
    }

    /// <summary>攻撃フレームに合わせてヒットボックスを有効化し、ヒット済み管理を初期化。</summary>
    public void EnableWeaponHitbox()
    {
        _hitTargets.Clear();
        _isHitboxActive = true;
        if (_weapons == null) return;
        foreach (var w in _weapons)
        {
            w.EnableHitbox();
        }
    }

    /// <summary>ヒットボックスを無効化し、新規ヒットを発生させない。</summary>
    public void DisableWeaponHitbox()
    {
        _isHitboxActive = false;
        if (_weapons == null) return;
        foreach (var w in _weapons)
        {
            w.DisableHitbox();
        }
    }

    /// <summary>武器コライダーにヒットした相手へ一度だけダメージを適用する（Weaponごとのハンドラで呼ばれる）。</summary>
    private void HandleWeaponHit(Collider other, EnemyWeapon sourceWeapon)
    {
        if (!_isHitboxActive || other == null) return;

        // 自分自身(敵)へのヒットは無視
        if (_ownerTransform != null && other.transform.IsChildOf(_ownerTransform)) return;

        int instanceId = other.GetInstanceID();
        if (!_hitTargets.Add(instanceId)) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        Vector3 origin = _ownerTransform != null ? _ownerTransform.position : other.bounds.center;
        Vector3 hitPoint = other.ClosestPoint(origin);
        Vector3 hitNormal = (hitPoint - origin).sqrMagnitude > 0.0001f ? (hitPoint - origin).normalized : Vector3.forward;

        float damage = sourceWeapon != null ? sourceWeapon.Damage() : (_status != null ? _status.EnemyPower : 0f);

        DamageInfo damageInfo = new DamageInfo(damage, hitPoint, hitNormal, _ownerTransform != null ? _ownerTransform.gameObject : null, other);
        damageable.ApplyDamage(damageInfo);
    }

    public void Dispose()
    {
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
