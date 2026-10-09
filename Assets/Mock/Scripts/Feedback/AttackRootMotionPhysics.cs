using UnityEngine;
/// <summary>攻撃Root Motionの水平移動をRigidbodyの衝突形状で制限する共通処理。</summary>
public static class AttackRootMotionPhysics
{
    /// <summary>RigidbodyのSweepで水平移動先の衝突を確認し、接触面より手前まで移動量を制限する。</summary>
    /// <returns>衝突を考慮した水平移動量。</returns>
    public static Vector3 LimitDisplacement(Rigidbody body, Vector3 delta)
    {
        delta.y = 0f;
        if (body == null || !body.detectCollisions || !float.IsFinite(delta.x) || !float.IsFinite(delta.z)) return Vector3.zero;
        float distance = delta.magnitude;
        if (distance < .00001f) return Vector3.zero;
        Vector3 direction = delta / distance;
        float allowed = distance;
        foreach (var hit in body.SweepTestAll(direction, distance + .01f, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(body.transform)) continue;
            if (Physics.GetIgnoreLayerCollision(body.gameObject.layer, hit.collider.gameObject.layer)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - .01f));
        }
        return direction * allowed;
    }
}
