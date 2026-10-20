using UnityEngine;

/// <summary>
/// 攻撃ヒット時にターゲットへ伝えるダメージ情報。
/// </summary>
public readonly struct DamageInfo
{
    /// <summary>命中時の値と攻撃者を保持し、通常攻撃とジャスト回避追撃の識別を伝える。</summary>
    public DamageInfo(float damageAmount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator, Collider targetCollider, bool isHeavy = false, Vector3 slashDirection = default, bool isJustAvoidCounter = false, JustAvoidCounterStrike counterStrike = JustAvoidCounterStrike.None, ComboSoundLayer[] hitSounds = null)
    {
        HitSounds = hitSounds;
        DamageAmount = damageAmount;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        Instigator = instigator;
        TargetCollider = targetCollider;
        IsHeavy = isHeavy;
        SlashDirection = slashDirection;
        IsJustAvoidCounter = isJustAvoidCounter;
        CounterStrike = isJustAvoidCounter ? counterStrike : JustAvoidCounterStrike.None;
    }

    /// <summary>演出の強度区分。ダメージ計算には使わない。</summary>
    public bool IsHeavy { get; }

    /// <summary>Just Avoid専用の追撃Animationから成立した命中。通常Heavyとは独立。</summary>
    public bool IsJustAvoidCounter { get; }
    public JustAvoidCounterStrike CounterStrike { get; }
    public bool IsCounterFirst => IsJustAvoidCounter && CounterStrike == JustAvoidCounterStrike.First;

    /// <summary>攻撃側が指定した命中音。敵コンボの段別SEで使用し、未指定なら被弾側の通常命中音を鳴らす。</summary>
    public ComboSoundLayer[] HitSounds { get; }

    /// <summary>最終的に与えるダメージ量。</summary>
    public float DamageAmount { get; }

    /// <summary>命中したワールド座標。</summary>
    public Vector3 HitPoint { get; }

    /// <summary>命中面の法線ベクトル。ヒット演出の向き決定などに使用。</summary>
    public Vector3 HitNormal { get; }

    /// <summary>血飛沫用の刀の移動方向。取得できない攻撃はHitNormalへフォールバックする。</summary>
    public Vector3 SlashDirection { get; }

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
