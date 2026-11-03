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
    /// <summary>自傷（Self Sacrifice）時の HP% 減少量（秒あたり、1 = 1%/秒）。</summary>
    public float SelfSacrificeDamagePercentPerSecond => _selfSacrificeDamagePercentPerSecond;

    /// <summary>回復（Heal）時に、HPの1%あたり何ポイントのゲージを消費するか。</summary>
    public float HealGaugePerPercent => _healGaugePerPercent;

    /// <summary>バフモード（Buff Mode）時のゲージ消費（1秒あたり）。</summary>
    public float BuffGaugePerSecond => _buffGaugePerSecond;

    /// <summary>疾走中の毎秒ゲージ消費量（ゲージ単位/秒）。</summary>
    [UnityEngine.Tooltip("疾走中の毎秒ゲージ消費量（ゲージ単位/秒）。")]
    [Header("Dash")]
    [SerializeField, Min(0f)] private float _dashPerSecond = 25f;

    /// <summary>幽体化を開始した瞬間に消費するゲージ量（ゲージ単位）。</summary>
    [UnityEngine.Tooltip("幽体化を開始した瞬間に消費するゲージ量（ゲージ単位）。")]
    [Header("Ghost / Evasion")]
    [SerializeField, Min(0f)] private float _ghostActivationCost = 20f;
    /// <summary>幽体化を継続する毎秒ゲージ消費量（ゲージ単位/秒）。</summary>
    [UnityEngine.Tooltip("幽体化を継続する毎秒ゲージ消費量（ゲージ単位/秒）。")]
    [SerializeField, Min(0f)] private float _ghostPerSecondCost = 5f;

    /// <summary>自傷中の毎秒ゲージ消費量（ゲージ単位/秒）。HP消費とは別に支払う。</summary>
    [UnityEngine.Tooltip("自傷中の毎秒ゲージ消費量（ゲージ単位/秒）。HP消費とは別に支払う。")]
    [Header("Self Sacrifice")]
    [SerializeField, Min(0f)] private float _selfSacrificeGaugePerSecond = 10f;
    /// <summary>自傷を継続できるHP比率の下限。0〜1で最大HPに対する比率。</summary>
    [UnityEngine.Tooltip("自傷を継続できるHP比率の下限。0〜1で最大HPに対する比率。")]
    [SerializeField, Range(0f, 1f)] private float _selfSacrificeMinHpRatio = 0.1f;
    /// <summary>自傷で毎秒失う最大HPの割合（%/秒）。1で最大HPの1%を毎秒失う。</summary>
    [UnityEngine.Tooltip("自傷で毎秒失う最大HPの割合（%/秒）。1で最大HPの1%を毎秒失う。")]
    [SerializeField, Min(0f)] private float _selfSacrificeDamagePercentPerSecond = 1f;

    /// <summary>最大HPを1%回復するために消費するゲージ量（ゲージ単位/1%）。</summary>
    [UnityEngine.Tooltip("最大HPを1%回復するために消費するゲージ量（ゲージ単位/1%）。")]
    [Header("Heal")]
    [SerializeField, Min(0f)] private float _healGaugePerPercent = 2f;

    /// <summary>バフ用の毎秒ゲージ消費設定。使用する行動側から参照するための値。</summary>
    [UnityEngine.Tooltip("バフ用の毎秒ゲージ消費設定。使用する行動側から参照するための値。")]
    [Header("Buff Mode")]
    [SerializeField, Min(0f)] private float _buffGaugePerSecond = 8f;
}

/// <summary>PlayerのHP・移動・攻撃などの基礎値と能力コストをInspectorで設定する。</summary>
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

    /// <summary>
    /// ジャスト回避スタックの効果設定（ScriptableObject）。null ならスタック効果は無効。
    /// </summary>
    public JustAvoidBuffConfig JustAvoidBuffConfig => _justAvoidBuffConfig;

    /// <summary>Playerの初期残機設定。Runの進行ではRunSessionの残機を使用する。</summary>
    [UnityEngine.Tooltip("Playerの初期残機設定。Runの進行ではRunSessionの残機を使用する。")]
    [Header("Basic Status")]
    [SerializeField] private int _life = 3;
    /// <summary>Playerの最大HP。体力初期化とHP比率計算の基準。</summary>
    [UnityEngine.Tooltip("Playerの最大HP。体力初期化とHP比率計算の基準。")]
    [SerializeField] private int _maxHealth = 100;
    /// <summary>Playerの基準攻撃力。攻撃Clipや装備・バフのダメージ計算で参照する。</summary>
    [UnityEngine.Tooltip("Playerの基準攻撃力。攻撃Clipや装備・バフのダメージ計算で参照する。")]
    [SerializeField] private float _attackPower = 10f;

    /// <summary>納刀中の通常移動速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("納刀中の通常移動速度（Unity単位/秒）。")]
    [Header("Movement")]
    [SerializeField] private float _noWeaponMoveSpeed = 5f;
    /// <summary>納刀中の疾走速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("納刀中の疾走速度（Unity単位/秒）。")]
    [SerializeField] private float _noWeaponSprintSpeed = 8f;
    /// <summary>抜刀・非Lock-On時の歩行速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("抜刀・非Lock-On時の歩行速度（Unity単位/秒）。")]
    [SerializeField] private float _unLockWalkSpeed = 5f;
    /// <summary>抜刀・非Lock-On時の疾走速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("抜刀・非Lock-On時の疾走速度（Unity単位/秒）。")]
    [SerializeField] private float _unLockSprintSpeed = 6f;
    /// <summary>Lock-On中の歩行速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("Lock-On中の歩行速度（Unity単位/秒）。")]
    [SerializeField] private float _lockOnWalkSpeed = 3f;
    /// <summary>Lock-On中の疾走速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("Lock-On中の疾走速度（Unity単位/秒）。")]
    [SerializeField] private float _lockOnSprintSpeed = 5f;
    /// <summary>Playerが移動方向へ向く回転補間係数。大きいほど素早く向きを合わせる。</summary>
    [UnityEngine.Tooltip("Playerが移動方向へ向く回転補間係数。大きいほど素早く向きを合わせる。")]
    [SerializeField] private float _rotationSmoothness = 0.25f;
    /// <summary>通常移動で目標速度へ近づける加速係数。</summary>
    [UnityEngine.Tooltip("通常移動で目標速度へ近づける加速係数。")]
    [SerializeField] private float _acceleration = 5f;
    /// <summary>移動入力がなくなったときに速度を落とす減速係数。</summary>
    [UnityEngine.Tooltip("移動入力がなくなったときに速度を落とす減速係数。")]
    [SerializeField] private float _breakForce = 0.9f;

    /// <summary>Playerスキルゲージの最大値。初期化時に使用する。</summary>
    [UnityEngine.Tooltip("Playerスキルゲージの最大値。初期化時に使用する。")]
    [Header("Skill Gauge")]
    [SerializeField, Min(1f)] private float _maxSkillGauge = 100f;
    /// <summary>ゲージ消費行動の受付制限に使う正規化閾値。0〜1で最大ゲージに対する比率。</summary>
    [UnityEngine.Tooltip("ゲージ消費行動の受付制限に使う正規化閾値。0〜1で最大ゲージに対する比率。")]
    [SerializeField, Range(0f, 1f)] private float _skillGaugeLockoutThresholdNormalized = 0.25f;
    /// <summary>スキルゲージの基準自然回復量（ゲージ単位/秒）。</summary>
    [UnityEngine.Tooltip("スキルゲージの基準自然回復量（ゲージ単位/秒）。")]
    [SerializeField, Min(0f)] private float _skillGaugePassiveRecoveryPerSecond = 10f;
    /// <summary>攻撃命中時の基本ゲージ回復量（ゲージ単位）。</summary>
    [UnityEngine.Tooltip("攻撃命中時の基本ゲージ回復量（ゲージ単位）。")]
    [SerializeField, Min(0f)] private float _skillGaugeOnAttackGain = 5f;
    /// <summary>回避時の基本ゲージ回復量（ゲージ単位）。</summary>
    [UnityEngine.Tooltip("回避時の基本ゲージ回復量（ゲージ単位）。")]
    [SerializeField, Min(0f)] private float _skillGaugeOnAvoidGain = 5f;
    /// <summary>Just Avoid成功時に追加するゲージ回復量（ゲージ単位）。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時に追加するゲージ回復量（ゲージ単位）。")]
    [SerializeField, Min(0f)] private float _skillGaugeOnJustAvoidBonus = 10f;
    /// <summary>疾走・幽体化・自傷・回復などのゲージ消費設定。</summary>
    [UnityEngine.Tooltip("疾走・幽体化・自傷・回復などのゲージ消費設定。")]
    [SerializeField] private SkillGaugeCostConfig _skillGaugeCost;

    /// <summary>現在HP比率に応じた攻撃倍率・ゲージ回復補正の参照表。</summary>
    [UnityEngine.Tooltip("現在HP比率に応じた攻撃倍率・ゲージ回復補正の参照表。")]
    [Header("Low HP Buff")]
    [SerializeField] private LowHpBuffTable _lowHpBuffTable;

    /// <summary>Just Avoid成功の累積攻撃ボーナスと最大スタックの設定。</summary>
    [UnityEngine.Tooltip("Just Avoid成功の累積攻撃ボーナスと最大スタックの設定。")]
    [Header("Just-Avoid / Buffs")]
    [SerializeField] private JustAvoidBuffConfig _justAvoidBuffConfig;
}
