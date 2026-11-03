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
    private bool _sweptHits;
    private Vector3 _previousBoxCenter;
    private Quaternion _previousBoxRotation;
    private bool _hasBoxSample;

    /// <summary>RushAttackだけが有効にする連続判定。通常攻撃とPlayerには既定で影響しない。</summary>
    public void SetSweptHits(bool active)
    {
        _sweptHits = active;
        ResetSweepSample();
    }
    public void ResetSweepSample()
    {
        _hasBoxSample = false;
        if (_sweptHits && _ownerCollider is BoxCollider box && box.enabled)
        {
            _previousBoxCenter = box.transform.TransformPoint(box.center);
            _previousBoxRotation = box.transform.rotation;
            _hasBoxSample = true;
        }
    }
    public void FlushSweptHits() => SweepBox();

    private void SweepBox()
    {
        if (!_sweptHits || !(_ownerCollider is BoxCollider box) || !box.enabled)
        { _hasBoxSample = false; return; }
        Vector3 center = box.transform.TransformPoint(box.center);
        Quaternion rotation = box.transform.rotation;
        Vector3 scale = box.transform.lossyScale;
        Vector3 half = Vector3.Scale(box.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        if (!_hasBoxSample) { _previousBoxCenter = center; _previousBoxRotation = rotation; }
        int steps = Mathf.Max(1, Mathf.CeilToInt(Quaternion.Angle(_previousBoxRotation, rotation) / 5f));
        Vector3 previous = _previousBoxCenter;
        for (int i = 1; i <= steps; i++)
        {
            Vector3 point = Vector3.Lerp(_previousBoxCenter, center, (float)i / steps);
            Quaternion q = Quaternion.Slerp(_previousBoxRotation, rotation, (float)i / steps);
            Vector3 delta = point - previous;
            if (delta.sqrMagnitude > .0000001f)
                foreach (var hit in Physics.BoxCastAll(previous, half, delta.normalized, q, delta.magnitude, ~0, QueryTriggerInteraction.Collide))
                    ReportSweptHit(hit.collider);
            foreach (var other in Physics.OverlapBox(point, half, q, ~0, QueryTriggerInteraction.Collide)) ReportSweptHit(other);
            previous = point;
        }
        _previousBoxCenter = center; _previousBoxRotation = rotation; _hasBoxSample = true;
    }
    private void ReportSweptHit(Collider other)
    {
        if (!_sweptHits || !enabled || _ownerCollider == null || !_ownerCollider.enabled || other == null ||
            Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer) || Physics.GetIgnoreCollision(_ownerCollider, other)) return;
        _onHit?.Invoke(other);
    }

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

    /// <summary>有効な刀Colliderの位置差を記録して接触時の斬撃方向に使用する。無効時は前回サンプルを破棄する。</summary>
    private void LateUpdate()
    {
        SweepBox();
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
    /// <summary>所有Colliderを取得し、命中通知用Triggerとして一度だけ初期化する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        AssignColliderAndForceTrigger();
    }

    /// <summary>Editorでコンポーネントを追加した際に所有ColliderをTriggerへ設定する。</summary>
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

    /// <summary>有効な判定区間だけ接触Colliderを登録済み購読者へ通知する。</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        _onHit?.Invoke(other);
    }
}
