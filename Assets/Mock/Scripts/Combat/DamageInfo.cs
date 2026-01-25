using UnityEngine;

/// <summary>
/// 攻撃ヒット時にターゲットへ伝えるダメージ情報。
/// </summary>
public readonly struct DamageInfo
{
    public DamageInfo(float damageAmount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator, Collider targetCollider)
    {
        DamageAmount = damageAmount;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        Instigator = instigator;
        TargetCollider = targetCollider;
    }

    /// <summary>最終的に与えるダメージ量。</summary>
    public float DamageAmount { get; }

    /// <summary>命中したワールド座標。</summary>
    public Vector3 HitPoint { get; }

    /// <summary>命中面の法線ベクトル。ヒット演出の向き決定などに使用。</summary>
    public Vector3 HitNormal { get; }

    /// <summary>攻撃を発生させたオブジェクト（プレイヤー等）。</summary>
    public GameObject Instigator { get; }

    /// <summary>実際にヒットしたコライダー参照。</summary>
    public Collider TargetCollider { get; }
}

/// <summary>
/// ダメージを受け取れるターゲットが実装するインターフェース。
/// </summary>
public interface IDamageable
{
    void ApplyDamage(DamageInfo damageInfo);
}
