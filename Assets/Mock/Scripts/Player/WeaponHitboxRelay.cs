using System;
using UnityEngine;

/// <summary>
/// 武器コライダーの OnTriggerEnter を外部へ多播する補助コンポーネント。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WeaponHitboxRelay : MonoBehaviour
{
    /// <summary>衝突を購読者へ通知するイベント。</summary>
    private event Action<Collider> _onHit;

    /// <summary>自分自身に付与されているコライダー参照。</summary>
    private Collider _ownerCollider;
    private Vector3 _previousSweepCenter, _lastSweep;
    private bool _hasSweepSample;

    /// <summary>命中フレームの刀の進行方向。判定やCollider設定には使用しない。</summary>
    public Vector3 SweepDirection
    {
        get
        {
            if (_ownerCollider == null || !_hasSweepSample) return Vector3.zero;
            Vector3 delta = _ownerCollider.bounds.center - _previousSweepCenter;
            return delta.sqrMagnitude > .000001f ? delta.normalized : _lastSweep;
        }
    }

    private void LateUpdate()
    {
        if (_ownerCollider == null || !_ownerCollider.enabled)
        {
            _hasSweepSample = false;
            _lastSweep = Vector3.zero;
            return;
        }
        Vector3 center = _ownerCollider.bounds.center;
        if (_hasSweepSample)
        {
            Vector3 delta = center - _previousSweepCenter;
            // 静止時に前の斬撃方向を持ち越さない。
            _lastSweep = delta.sqrMagnitude > .000001f ? delta.normalized : Vector3.zero;
        }
        _previousSweepCenter = center;
        _hasSweepSample = true;
    }

    /// <summary>ヒットイベントの購読者を登録する。</summary>
    public void Subscribe(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        _onHit -= handler;
        _onHit += handler;
    }

    /// <summary>ヒットイベントの購読者登録を解除する。</summary>
    public void Unsubscribe(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        _onHit -= handler;
    }

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        AssignColliderAndForceTrigger();
    }

    private void Reset()
    {
        AssignColliderAndForceTrigger();
    }

    /// <summary>所有コライダーを取得して isTrigger を強制的に true にする。</summary>
    private void AssignColliderAndForceTrigger()
    {
        _ownerCollider = GetComponent<Collider>();
        if (_ownerCollider != null)
        {
            _ownerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        _onHit?.Invoke(other);
    }
}
