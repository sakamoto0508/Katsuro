using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDecisionConfig", menuName = "ScriptableObjects/Enemy/DecisionConfig")]
public class EnemyDecisionConfig : ScriptableObject
{
    [Header("Distance thresholds")]
    public float FarDistance = 12f;
    public float NearDistance = 3f;
    public float ObserveSeconds = 1.5f;
    public float ReconsiderInterval = 0.5f;

    [Header("Weights")]
    // Legacy single-weight fields (kept for compatibility)
    public float WeightWarpAttack = 10f;
    public float WeightApproach = 30f;
    public float WeightRush = 30f;
    public float WeightObserve = 20f;
    public float WeightSlash = 50f;
    public float WeightBackstep = 20f;
    
    [Header("Behavior")]
    [Range(0f, 1f), Tooltip("直前に実行した行動の重みを何倍にするか（0 = 完全に除外、1 = 変化なし）")]
    public float RepeatPenalty = 0.25f;

    [System.Serializable]
    public struct ActionWeight
    {
        public EnemyActionType Action;
        public float Weight;
    }

    [Header("Decision Candidates (Inspector editable lists)")]
    [Tooltip("候補: 遠距離用の行動リストと重み")]
    public ActionWeight[] FarCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.WarpAttack, Weight = 10f },
        new ActionWeight{ Action = EnemyActionType.Approach, Weight = 30f }
    };

    [Tooltip("候補: 中距離用の行動リストと重み")]
    public ActionWeight[] MidCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.Thrust, Weight = 30f },
        new ActionWeight{ Action = EnemyActionType.Approach, Weight = 30f },
        new ActionWeight{ Action = EnemyActionType.Wait, Weight = 20f }
    };

    [Tooltip("候補: 近距離用の行動リストと重み")]
    public ActionWeight[] NearCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.Slash, Weight = 50f },
        new ActionWeight{ Action = EnemyActionType.Wait, Weight = 20f },
        new ActionWeight{ Action = EnemyActionType.StepBack, Weight = 20f }
    };
}
