using UnityEngine;
using UnityEngine.Serialization;

/// <summary>1本のコンボClipと完了後の復帰設定。斬撃区間は敵専用ClipのAnimation Eventが所有する。</summary>
[CreateAssetMenu(menuName = "Enemy/Attack Pattern", fileName = "EnemyAttackPattern")]
public sealed class EnemyAttackPattern : ScriptableObject
{
    public string PatternName = "Combo";
    public EnemyAttackData Attack;
    [Tooltip("完全なAnimator Stateパス。遅れた別StateのEventを除外する。")]
    public string AnimatorState;
    [FormerlySerializedAs("Windup"), InspectorName("Stance Duration / Windup Duration"), Min(0f), Tooltip("構え時間（秒）。RushではStanceDurationとして再利用。HitStop中は停止する。TuneDashRootMotion使用時は0.01秒以上。")]
    public float WindupDuration = .35f;
    [Min(0f), Tooltip("予兆として遅く再生するClip冒頭の終端（Clip秒）。必ず初撃のHitbox ONより前にする。")]
    public float WindupEndTime = .15f;
    [Min(0f)] public float RecoveryDuration = .45f;
    [Min(.1f), Tooltip("速度1での異常復帰期限。再生速度に応じて自動補正する。")]
    public float Timeout = 6f;
    [Range(.5f, 1.5f), Tooltip("コンボ本体の速度。Rush調整使用時は刺突後の保持・復帰だけに適用。Animator全体のspeedは変更しない。")]
    public float PlaybackSpeed = 1f;
    [Min(0f), Tooltip("このコンボだけの水平Root Motion倍率。Rush調整使用時はDashDistanceと実測値から自動算出する。")]
    public float RootMotionScale = .18f;
    [Header("RushAttack / Root Motion tuning")]
    [Tooltip("RushAttackだけで使用。WindupDurationがStanceDurationに相当。距離と速度はRoot Motionから調整する。")]
    public bool TuneDashRootMotion;
    [Min(.1f)] public float DashDistance = 3f;
    [Min(.1f), Tooltip("平均前進速度。瞬間速度は元Clipの加速カーブに従う。")]
    public float DashSpeed = 12f;
    [Min(0f), Tooltip("刺突区間の終端（元Clip秒）。開始はWindupEndTime。")]
    public float DashEndTime = .55f;
    [Min(.001f), Tooltip("既存Enemy Avatarで測定した刺突区間の水平前進量。Clip変更時は再測定。")]
    public float MeasuredDashDistance = 3.145f;
    [Header("Rush pose / weapon grip")]
    [Tooltip("自然な手首で制作したRush Clip用のGrip。終了・中断時に元の装備Transformへ戻す。")]
    public bool OverrideWeaponGrip;
    public Vector3 WeaponGripLocalPosition;
    public Vector3 WeaponGripLocalEuler;
    public bool UsesDashTuning => TuneDashRootMotion && !IsWarp && Attack != null && Attack.ActionType == EnemyActionType.RushAttack;
    public float DashDuration => DashDistance / DashSpeed;
    public float DashPlaybackSpeed => (DashEndTime - WindupEndTime) / DashDuration;
    public float DashRootMotionScale => DashDistance / MeasuredDashDistance;
    public float BodyTimeout => UsesDashTuning ? Timeout + DashDuration : Timeout / PlaybackSpeed;
    [Header("Selection / travel")]
    [Min(0f)] public float MinAttackDistance;
    [Min(0f)] public float MaxAttackDistance = 1000f;
    [Min(0f)] public float Cooldown;
    [Min(0f), Tooltip("水平Root Motionの累計移動距離の上限。0は制限なし。")]
    public float MaximumTravelDistance;
    [Header("WarpAttack only")]
    [Min(.1f)] public float TeleportDistance = 2.5f;
    [Range(-90f, 90f)] public float TeleportAngleOffset;
    [Min(0f)] public float DisappearDuration = .18f;
    [Min(.1f), Tooltip("再出現後の攻撃予兆。WarpではWindupDurationは消失前、こちらは出現後の構え時間。")]
    public float ReappearWindupDuration = .5f;
    public AudioClip DisappearSound;
    public AudioClip ReappearSound;
    [Tooltip("旧ワープ演出。下のArt VFX（消失/出現）が未設定のときだけ使う。")]
    public GameObject WarpEffectPrefab;
    [Tooltip("消失時の黒霧（足元基準）。")]
    public KatsuroTimedVFX WarpVanishVFX;
    [Tooltip("出現時の青白い収束光・閃光・衝撃波（収束点基準）。")]
    public KatsuroTimedVFX WarpAppearVFX;
    [Tooltip("出現後の斬撃で、最初の攻撃判定が開いた瞬間に出す一閃。")]
    public KatsuroTimedVFX WarpSlashVFX;

    [Min(0f), Tooltip("黒霧を出してから本体を隠すまでの秒数。移動・判定には使用しない。")]
    public float WarpVanishLead = .07f;
    [Min(0f), Tooltip("実体出現前に収束光を始める秒数。") ]
    public float WarpAppearLead = .06f;
    [Min(0f), Tooltip("収束点・一閃の高さ（足元からm）。")]
    public float WarpEffectHeight = 1.1f;
    [Min(0f), Tooltip("一閃を敵の前方へ出す距離（m）。")]
    public float WarpSlashForward = .8f;
    [Range(-180f, 180f), Tooltip("一閃の向き。敵の右方向を、敵の前方軸まわりに回した角度。負で右下がり。")]
    public float WarpSlashRoll = -35f;
    [Range(.3f, 2f), Tooltip("一閃の大きさ（基準長2m）。")]
    public float WarpSlashScale = .9f;
    public bool UsesArtWarpVFX => WarpVanishVFX != null || WarpAppearVFX != null;
    public bool IsWarp => Attack != null && Attack.ActionType == EnemyActionType.WarpAttack;
    public float ActiveWindupDuration => IsWarp ? ReappearWindupDuration : WindupDuration;
    public float WindupSpeed => ActiveWindupDuration > 0f && WindupEndTime > 0f
        ? WindupEndTime / ActiveWindupDuration : PlaybackSpeed;
    public int StateHash => Animator.StringToHash(AnimatorState ?? "");
    public bool IsValid => Attack != null && !string.IsNullOrWhiteSpace(Attack.AnimatorTrigger) &&
        !string.IsNullOrWhiteSpace(AnimatorState) && Valid(WindupDuration) && Valid(WindupEndTime) && Valid(RecoveryDuration) &&
        Valid(Timeout) && Timeout > 0f && Valid(RootMotionScale) && Valid(Cooldown) &&
        Valid(MinAttackDistance) && Valid(MaxAttackDistance) && MaxAttackDistance >= MinAttackDistance && Valid(MaximumTravelDistance) &&
        (!UsesDashTuning || !OverrideWeaponGrip || (Finite(WeaponGripLocalPosition) && Finite(WeaponGripLocalEuler))) &&
        (!UsesDashTuning || (WindupDuration >= .01f && WindupEndTime > 0f && Valid(DashEndTime) && DashEndTime > WindupEndTime &&
            Valid(DashDistance) && DashDistance >= .1f && Valid(DashSpeed) && DashSpeed >= .1f &&
            Valid(MeasuredDashDistance) && MeasuredDashDistance >= .001f && float.IsFinite(DashPlaybackSpeed))) &&
        (!IsWarp || (Valid(TeleportDistance) && TeleportDistance > 0f && float.IsFinite(TeleportAngleOffset) &&
            Mathf.Abs(TeleportAngleOffset) <= 90f && Valid(DisappearDuration) && Valid(ReappearWindupDuration) && ReappearWindupDuration >= .1f)) &&
        float.IsFinite(PlaybackSpeed) && PlaybackSpeed >= .5f && PlaybackSpeed <= 1.5f;
    private static bool Valid(float value) => float.IsFinite(value) && value >= 0f;
    private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
}
