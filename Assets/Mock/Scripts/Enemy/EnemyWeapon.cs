using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 非 MonoBehaviour の EnemyWeapon ラッパー（PlayerWeapon に合わせた API）
/// </summary>
public sealed class EnemyWeapon
{
    public EnemyWeapon(Collider[] weaponColliders, float fallbackPower)
    {
        _weaponColliders = weaponColliders;
        _fallbackPower = fallbackPower;
        SetHitboxActive(false);
    }

    private readonly Collider[] _weaponColliders;
    // ステータス参照を使わず、フォールバックの攻撃力を保持する（型依存を避けるため）
    private readonly float _fallbackPower;
    private readonly Dictionary<Collider, Component> _relayCache = new Dictionary<Collider, Component>();
    private float _currentAttackDamage = 0f;

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
            var method = relay.GetType().GetMethod("Subscribe");
            method?.Invoke(relay, new object[] { handler });
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
            var method = relay.GetType().GetMethod("Unsubscribe");
            method?.Invoke(relay, new object[] { handler });
        }
    }

    private IEnumerable<Component> EnumerateRelays()
    {
        if (_weaponColliders == null)
        {
            yield break;
        }

        // WeaponHitboxRelay 型は Player 側に定義されているため、リフレクションで取得する
        Type relayType = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            relayType = asm.GetType("WeaponHitboxRelay");
            if (relayType != null) break;
        }

        foreach (var weaponCollider in _weaponColliders)
        {
            if (weaponCollider == null)
            {
                continue;
            }

            weaponCollider.isTrigger = true;

            if (!_relayCache.TryGetValue(weaponCollider, out var relay) || relay == null)
            {
                if (relayType != null)
                {
                    relay = weaponCollider.GetComponent(relayType) as Component;
                    if (relay == null)
                    {
                        relay = weaponCollider.gameObject.AddComponent(relayType) as Component;
                    }
                }
                _relayCache[weaponCollider] = relay;
            }

            if (relay != null) yield return relay;
        }
    }

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
        }

        if (!isActive)
        {
            // 無効化時に一時ダメージはリセット
            _currentAttackDamage = 0f;
        }
    }

    /// <summary>この武器のダメージ量（ステータス参照）。</summary>
    public float Damage()
    {
        if (_currentAttackDamage > 0f) return _currentAttackDamage;
        return _fallbackPower;
    }
}
