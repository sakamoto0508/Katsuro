using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器コライダーのヒットボックス管理とコールバック登録を担うラッパー。
/// </summary>
public sealed class PlayerWeapon
{
    /// <summary>刀の判定Colliderと自己衝突を無視するCollider一覧を保持する。</summary>
    public PlayerWeapon(Collider[] weaponColliders, Collider[] ignoreColliders = null)
    {
        _weaponColliders = weaponColliders;
        _ignoreColliders = ignoreColliders;
    }

    private readonly Collider[] _weaponColliders;
    private readonly Dictionary<Collider, WeaponHitboxRelay> _relayCache = new();
    private readonly Collider[] _ignoreColliders;

    private readonly Dictionary<Collider, SwordTrail> _trails = new();
    private readonly Dictionary<BoxCollider, Vector3> _authoredSizes = new();
    private bool _initialized;
    /// <summary>刀判定を初期化し、Trigger Relay・軌跡・判定サイズ補正と自己衝突除外を準備する。</summary>
    public void Init() => Init(Vector3.zero);
    /// <summary>刀判定を初期化し、Trigger Relay・軌跡・判定サイズ補正と自己衝突除外を準備する。</summary>
    public void Init(Vector3 padding)
    {
        if (_initialized) return;
        _initialized = true;
        if (_weaponColliders == null) return;
        foreach (var collider in _weaponColliders)
        {
            if (collider == null) continue;
            if (collider is BoxCollider box) _authoredSizes[box] = box.size;
            var effect = collider.GetComponent<SwordTrail>();
            if (effect != null) { effect.Init(); _trails[collider] = effect; }
            var relay = collider.GetComponent<WeaponHitboxRelay>();
            if (relay != null) { relay.Init(); _relayCache[collider] = relay; }
            else Debug.LogError("武器ColliderにWeaponHitboxRelayを配置してください。", collider);
            collider.enabled = false;
        }
        SetHitboxPadding(padding);
        SetupIgnoreCollisions();
    }

    /// <summary>刀のローカル軸で全体のサイズを加算する。中心と攻撃受付時間は変えない。</summary>
    public void SetHitboxPadding(Vector3 padding)
    {
        padding = Vector3.Max(Vector3.zero, padding);
        foreach (var entry in _authoredSizes)
            if (entry.Key != null) entry.Key.size = entry.Value + padding;
    }

    /// <summary>ヒットボックスを有効化する。</summary>
    public void EnableHitbox() => SetHitboxActive(true);

    /// <summary>ヒットボックスを無効化する。</summary>
    public void DisableHitbox() => SetHitboxActive(false);

    /// <summary>保持している刀の軌跡を攻撃種別に応じた表示へ切り替える。</summary>
    public void SetTrailStyle(SwordTrail.AttackStyle style)
    {
        foreach (var trail in _trails.Values) trail.SetStyle(style);
    }

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

    /// <summary>初期化時に保存した刀判定Relayを列挙する。</summary>
    /// <returns>登録済みの命中通知Relay。</returns>
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

    /// <summary>接触点と同じ最寄りの刀から、血飛沫用の移動方向を取得する。</summary>
    /// <returns>直近の刀の振りから求めた斬撃方向。取得できなければ対象との方向。</returns>
    public Vector3 GetSlashDirection(Collider target)
    {
        float nearest = float.PositiveInfinity;
        Vector3 direction = Vector3.zero;
        if (_weaponColliders == null) return direction;
        foreach (var blade in _weaponColliders)
        {
            if (blade == null || !blade.enabled) continue;
            float distance = (target.ClosestPoint(blade.bounds.center) - blade.bounds.center).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance;
            direction = _relayCache.TryGetValue(blade, out var relay) ? relay.SweepDirection : Vector3.zero;
        }
        return direction;
    }

    /// <summary>刀Colliderと軌跡の受付をまとめて切り替え、判定区間と表示区間を対応させる。</summary>
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
    }

    /// <summary>指定された自己Colliderとの衝突を刀Colliderごとに無視する。</summary>
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
