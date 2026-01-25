using System;
using UnityEngine;

/// <summary>
/// 武器コライダーの OnTriggerEnter を外部へ多播する補助コンポーネント。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WeaponHitboxRelay : MonoBehaviour
{
    private event Action<Collider> _onHit;
    private Collider _ownerCollider;

    public void Subscribe(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        _onHit -= handler;
        _onHit += handler;
    }

    public void Unsubscribe(Action<Collider> handler)
    {
        if (handler == null)
        {
            return;
        }

        _onHit -= handler;
    }

    private void Awake()
    {
        _ownerCollider = GetComponent<Collider>();
        if (_ownerCollider != null)
        {
            _ownerCollider.isTrigger = true;
        }
    }

    private void Reset()
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
