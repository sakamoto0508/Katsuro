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

    private void Awake()
    {
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
