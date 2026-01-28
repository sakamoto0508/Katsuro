using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スキルゲージ消費設定
/// </summary>
[Serializable]
public class SkillGaugeCostConfig
{
    /// <summary>ダッシュ時のゲージ消費（1秒あたり）。移動中に毎秒消費する基本値。</summary>
    public float DashPerSecond => _dashPerSecond;

    /// <summary>幽霊化／回避開始時に消費するゲージ量（ワンタイムコスト）。</summary>
    public float GhostActivationCost => _ghostActivationCost;

    /// <summary>幽霊化中に継続して消費されるゲージ（1秒あたり）。</summary>
    public float GhostPerSecondCost => _ghostPerSecondCost;

    /// <summary>自傷（Self Sacrifice）時に毎秒消費するゲージ量。</summary>
    public float SelfSacrificeGaugePerSecond => _selfSacrificeGaugePerSecond;

    /// <summary>自傷を行うときに許容される最小HP割合（この値以下のときは自傷を許可しないなどの判定に使用）。</summary>
    public float SelfSacrificeMinHpRatio => _selfSacrificeMinHpRatio;

    /// <summary>回復（Heal）時に、HPの1%あたり何ポイントのゲージを消費するか。</summary>
    public float HealGaugePerPercent => _healGaugePerPercent;

    /// <summary>バフモード（Buff Mode）時のゲージ消費（1秒あたり）。</summary>
    public float BuffGaugePerSecond => _buffGaugePerSecond;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float _dashPerSecond = 25f;

    [Header("Ghost / Evasion")]
    [SerializeField, Min(0f)] private float _ghostActivationCost = 20f;
    [SerializeField, Min(0f)] private float _ghostPerSecondCost = 5f;

    [Header("Self Sacrifice")]
    [SerializeField, Min(0f)] private float _selfSacrificeGaugePerSecond = 10f;
    [SerializeField, Range(0f, 1f)] private float _selfSacrificeMinHpRatio = 0.1f;

    [Header("Heal")]
    [SerializeField, Min(0f)] private float _healGaugePerPercent = 2f;

    [Header("Buff Mode")]
    [SerializeField, Min(0f)] private float _buffGaugePerSecond = 8f;
}

[CreateAssetMenu(fileName = "PlayerStatus", menuName = "ScriptableObjects/Player/PlayerStatus", order = 1)]
public sealed class PlayerStatus : ScriptableObject
{
    /// <summary>プレイヤーの残機数（ライフ）。</summary>
    public int Life => _life;

    /// <summary>最大HP（ヒットポイント）。ゲーム内での上限値。</summary>
    public int MaxHealth => _maxHealth;

    /// <summary>基礎攻撃力。ダメージ計算の基本値として使用。</summary>
    public float AttackPower => _attackPower;

    /// <summary>武器なし通常時の歩行速度。</summary>
    public float NoWeaponMoveSpeed => _noWeaponMoveSpeed;

    /// <summary>武器なし通常時のダッシュ（スプリント）速度。</summary>
    public float NoWeaponSprintSpeed => _noWeaponSprintSpeed;

    /// <summary>武器装備解除時の歩行速度（アンロック時）。</summary>
    public float UnLockWalkSpeed => _unLockWalkSpeed;

    /// <summary>武器装備解除時のスプリント速度（アンロック時）。</summary>
    public float UnLockSprintSpeed => _unLockSprintSpeed;

    /// <summary>ロックオン時の歩行速度。</summary>
    public float LockOnWalkSpeed => _lockOnWalkSpeed;

    /// <summary>ロックオン時のスプリント速度。</summary>
    public float LockOnSprintSpeed => _lockOnSprintSpeed;

    /// <summary>回転のスムーズネス（回転補間の係数）。</summary>
    public float RotationSmoothness => _rotationSmoothness;

    /// <summary>加速度（移動入力に対する加速の強さ）。</summary>
    public float Acceleration => _acceleration;

    /// <summary>減速率 / ブレーキ力。</summary>
    public float BreakForce => _breakForce;

    /// <summary>スキルゲージの最大値（上限）。PlayerState 側の設定と整合させること。</summary>
    public float MaxSkillGauge => _maxSkillGauge;

    /// <summary>スキル封鎖閾値（正規化 0〜1）。この割合以下でスキルが使用禁止になるなどの判定に使用（例: 0.25 = 25%）。</summary>
    public float SkillGaugeLockoutThresholdNormalized => _skillGaugeLockoutThresholdNormalized;

    /// <summary>スキルゲージのパッシブ回復量（1秒あたりの回復量）。</summary>
    public float SkillGaugePassiveRecoveryPerSecond => _skillGaugePassiveRecoveryPerSecond;

    /// <summary>攻撃時に付与されるスキルゲージの増加量（ヒット時など）。</summary>
    public float SkillGaugeOnAttackGain => _skillGaugeOnAttackGain;

    /// <summary>回避成功時に付与されるスキルゲージの増加量。</summary>
    public float SkillGaugeOnAvoidGain => _skillGaugeOnAvoidGain;

    /// <summary>ジャスト回避成功時に付与される追加ボーナスのスキルゲージ量。</summary>
    public float SkillGaugeOnJustAvoidBonus => _skillGaugeOnJustAvoidBonus;

    /// <summary>スキルゲージ消費に関する細かい設定（ダッシュ・幽霊化・自傷など）。</summary>
    public SkillGaugeCostConfig SkillGaugeCost => _skillGaugeCost;

    /// <summary>
    /// 低HP時のバフテーブル（ScriptableObject）。null なら未設定。
    /// </summary>
    public LowHpBuffTable LowHpBuffTable => _lowHpBuffTable;

    [Header("Basic Status")]
    [SerializeField] private int _life = 3;
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _attackPower = 10f;

    [Header("Movement")]
    [SerializeField] private float _noWeaponMoveSpeed = 5f;
    [SerializeField] private float _noWeaponSprintSpeed = 8f;
    [SerializeField] private float _unLockWalkSpeed = 5f;
    [SerializeField] private float _unLockSprintSpeed = 6f;
    [SerializeField] private float _lockOnWalkSpeed = 3f;
    [SerializeField] private float _lockOnSprintSpeed = 5f;
    [SerializeField] private float _rotationSmoothness = 0.25f;
    [SerializeField] private float _acceleration = 5f;
    [SerializeField] private float _breakForce = 0.9f;

    [Header("Skill Gauge")]
    [SerializeField, Min(1f)] private float _maxSkillGauge = 100f;
    [SerializeField, Range(0f, 1f)] private float _skillGaugeLockoutThresholdNormalized = 0.25f;
    [SerializeField, Min(0f)] private float _skillGaugePassiveRecoveryPerSecond = 10f;
    [SerializeField, Min(0f)] private float _skillGaugeOnAttackGain = 5f;
    [SerializeField, Min(0f)] private float _skillGaugeOnAvoidGain = 5f;
    [SerializeField, Min(0f)] private float _skillGaugeOnJustAvoidBonus = 10f;
    [SerializeField] private SkillGaugeCostConfig _skillGaugeCost;

    [Header("Low HP Buff")]
    [SerializeField] private LowHpBuffTable _lowHpBuffTable;   
}
