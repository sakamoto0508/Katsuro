using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器コライダーのヒットボックス管理とコールバック登録を担うラッパー。
/// </summary>
public sealed class PlayerWeapon
{
    public PlayerWeapon(Collider[] weaponColliders, Collider[] ignoreColliders = null)
    {
        _weaponColliders = weaponColliders;
        _ignoreColliders = ignoreColliders;
        InitSwordTrails();
        SetHitboxActive(false);
        SetupIgnoreCollisions();
    }

    private readonly Collider[] _weaponColliders;
    private readonly Dictionary<Collider, WeaponHitboxRelay> _relayCache = new();
    private readonly Collider[] _ignoreColliders;

    private void InitSwordTrails()
    {
        if (_weaponColliders == null) return;
        foreach (var collider in _weaponColliders)
        {
            if (collider == null) continue;
            var effect = collider.GetComponent<SwordTrail>();
            if (effect != null) effect.Init();
        }
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

    private IEnumerable<WeaponHitboxRelay> EnumerateRelays()
    {
        if (_weaponColliders == null)
        {
            yield break;
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
                relay = weaponCollider.GetComponent<WeaponHitboxRelay>();
                if (relay == null)
                {
                    relay = weaponCollider.gameObject.AddComponent<WeaponHitboxRelay>();
                }

                relay.Init();
                _relayCache[weaponCollider] = relay;
            }

            yield return relay;
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
            SwordTrail.SetActive(weaponCollider, isActive);
        }
    }

    private void SetupIgnoreCollisions()
    {
        if (_weaponColliders == null || _ignoreColliders == null) return;

        foreach (var weaponCollider in _weaponColliders)
        {
            if (weaponCollider == null) continue;
            foreach (var ownerCollider in _ignoreColliders)
            {
                if (ownerCollider == null) continue;
                if (ownerCollider == weaponCollider) continue;
                Physics.IgnoreCollision(weaponCollider, ownerCollider, true);
            }
        }
    }
}
