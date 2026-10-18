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
    [FormerlySerializedAs("Windup"), Min(0f), Tooltip("コンボ冒頭の構えを再生する時間（秒）。0で通常速度へ戻す。HitStop中は停止する。")]
    public float WindupDuration = .35f;
    [Min(0f), Tooltip("予兆として遅く再生するClip冒頭の終端（Clip秒）。必ず初撃のHitbox ONより前にする。")]
    public float WindupEndTime = .15f;
    [Min(0f)] public float RecoveryDuration = .45f;
    [Min(.1f), Tooltip("速度1での異常復帰期限。再生速度に応じて自動補正する。")]
    public float Timeout = 6f;
    [Range(.5f, 1.5f), Tooltip("予兆区間の後に適用するコンボ本体の速度。1で元速度。Animator全体のspeedは変更しない。")]
    public float PlaybackSpeed = 1f;
    [Min(0f), Tooltip("このコンボだけの水平Root Motion倍率。通常単発の共通倍率は変更しない。")]
    public float RootMotionScale = .18f;
    public float WindupSpeed => WindupDuration > 0f && WindupEndTime > 0f
        ? WindupEndTime / WindupDuration : PlaybackSpeed;
    public int StateHash => Animator.StringToHash(AnimatorState ?? "");
    public bool IsValid => Attack != null && !string.IsNullOrWhiteSpace(Attack.AnimatorTrigger) &&
        !string.IsNullOrWhiteSpace(AnimatorState) && Valid(WindupDuration) && Valid(WindupEndTime) && Valid(RecoveryDuration) &&
        Valid(Timeout) && Timeout > 0f && Valid(RootMotionScale) &&
        float.IsFinite(PlaybackSpeed) && PlaybackSpeed >= .5f && PlaybackSpeed <= 1.5f;
    private static bool Valid(float value) => float.IsFinite(value) && value >= 0f;
}
