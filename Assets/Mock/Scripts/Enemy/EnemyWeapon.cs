using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 非 MonoBehaviour の EnemyWeapon ラッパー（PlayerWeapon に合わせた API）
/// </summary>
public sealed class EnemyWeapon
{
    /// <summary>刀判定Colliderと攻撃設定未指定時の基礎威力を保持する。</summary>
    public EnemyWeapon(Collider[] weaponColliders, float fallbackPower)
    {
        _weaponColliders = weaponColliders;
        _fallbackPower = fallbackPower;
    }

    private readonly Collider[] _weaponColliders;
    // ステータス参照を使わず、フォールバックの攻撃力を保持する（型依存を避けるため）
    private readonly float _fallbackPower;
    private readonly Dictionary<Collider, WeaponHitboxRelay> _relayCache = new Dictionary<Collider, WeaponHitboxRelay>();
    private float _currentAttackDamage = 0f;

    private readonly Dictionary<Collider, SwordTrail> _trails = new();
    private bool _initialized;
    /// <summary>武器ColliderのTrigger Relayと刀の軌跡を一度だけ準備する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (_weaponColliders == null) return;
        foreach (var collider in _weaponColliders)
        {
            if (collider == null) continue;
            var effect = collider.GetComponent<SwordTrail>();
            if (effect == null)
                foreach (var candidate in _weaponColliders)
                    if (candidate != null && (effect = candidate.GetComponent<SwordTrail>()) != null) break;
            if (effect != null) { effect.Init(); _trails[collider] = effect; }
            var relay = collider.GetComponent<WeaponHitboxRelay>();
            if (relay != null) { relay.Init(); _relayCache[collider] = relay; }
            else Debug.LogError("武器ColliderにWeaponHitboxRelayを配置してください。", collider);
            collider.enabled = false;
        }
    }

    /// <summary>
    /// 一時的に設定される攻撃ダメージ。0 以下ならステータス由来のダメージを返す。
    /// </summary>
    public float CurrentAttackDamage
    {
        get => _currentAttackDamage;
        set => _currentAttackDamage = value;
    }

    /// <summary>ヒットボックスを有効化する。</summary>
    public void EnableHitbox() => SetHitboxActive(true);

    /// <summary>ヒットボックスを無効化する。</summary>
    public void DisableHitbox() => SetHitboxActive(false);

    /// <summary>武器がヒットした際の通知先を登録する。</summary>
    public void RegisterHitObserver(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        foreach (var relay in EnumerateRelays())
        {
            // reflection で Subscribe を呼ぶ（WeaponHitboxRelay 型に直接依存しない）
            relay.Subscribe(handler);
        }
    }

    /// <summary>ヒット通知の購読を解除する。</summary>
    public void UnregisterHitObserver(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        foreach (var relay in EnumerateRelays())
        {
            relay.Unsubscribe(handler);
        }
    }

    /// <summary>武器に保存した命中通知Relayを列挙する。</summary>
    /// <returns>登録済みの武器Relay。</returns>
    private IEnumerable<WeaponHitboxRelay> EnumerateRelays() => _relayCache.Values;

    /// <summary>対象に最も近い刀の判定部分から、接触面の位置を求める。</summary>
    /// <returns>刀の判定形状と対象の近接点から求めた接触位置。</returns>
    public Vector3 GetContactPoint(Collider target)
    {
        Vector3 point = target.bounds.center;
        float nearest = float.PositiveInfinity;
        if (_weaponColliders == null) return point;
        foreach (var blade in _weaponColliders)
        {
            if (blade == null || !blade.enabled) continue;
            Vector3 candidate = target.ClosestPoint(blade.bounds.center);
            float distance = (candidate - blade.bounds.center).sqrMagnitude;
            if (distance < nearest) { nearest = distance; point = candidate; }
        }
        return point;
    }

    /// <summary>武器Colliderと軌跡の有効状態を切り替え、攻撃判定区間と表示を同期する。</summary>
    private void SetHitboxActive(bool isActive)
    {
        if (_weaponColliders == null)
        {
            return;
        }

        foreach (var weaponCollider in _weaponColliders)
        {
            if (weaponCollider == null)
            {
                continue;
            }

            weaponCollider.enabled = isActive;
            if (_trails.TryGetValue(weaponCollider, out var trail)) trail.SetActive(isActive);
        }

        if (!isActive)
        {
            // 無効化時に一時ダメージはリセット
            _currentAttackDamage = 0f;
        }
    }

    /// <summary>この武器のダメージ量（ステータス参照）。</summary>
    /// <returns>攻撃中の指定威力。指定が正でなければ基礎威力。</returns>
    public float Damage()
    {
        if (_currentAttackDamage > 0f) return _currentAttackDamage;
        return _fallbackPower;
    }
}
