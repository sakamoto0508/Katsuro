using UnityEngine;

/// <summary>距離帯ごとの行動候補と重み・連続選択の補正を定義するAI判断用設定。</summary>
[CreateAssetMenu(fileName = "EnemyDecisionConfig", menuName = "ScriptableObjects/Enemy/DecisionConfig")]
public class EnemyDecisionConfig : ScriptableObject
{
    /// <summary>遠距離行動候補へ切り替えるPlayerとの距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("遠距離行動候補へ切り替えるPlayerとの距離（Unity単位）。")]
    [Header("Distance thresholds")]
    public float FarDistance = 12f;
    /// <summary>近距離行動候補へ切り替えるPlayerとの距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("近距離行動候補へ切り替えるPlayerとの距離（Unity単位）。")]
    public float NearDistance = 3f;
    /// <summary>Wait行動で様子を見る時間（秒）。</summary>
    [UnityEngine.Tooltip("Wait行動で様子を見る時間（秒）。")]
    public float ObserveSeconds = 1.5f;
    /// <summary>行動中に距離条件などを再検討する間隔（秒）。短いほど頻繁に判断する。</summary>
    [UnityEngine.Tooltip("行動中に距離条件などを再検討する間隔（秒）。短いほど頻繁に判断する。")]
    public float ReconsiderInterval = 0.5f;

    [Header("Weights")]
    // 互換性を維持するために残している旧形式の重み設定。
    /// <summary>旧形式のWarp攻撃の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式のWarp攻撃の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightWarpAttack = 10f;
    /// <summary>旧形式の接近行動の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式の接近行動の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightApproach = 30f;
    /// <summary>旧形式の突進行動の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式の突進行動の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightRush = 30f;
    /// <summary>旧形式の待機行動の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式の待機行動の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightObserve = 20f;
    /// <summary>旧形式の斬撃行動の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式の斬撃行動の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightSlash = 50f;
    /// <summary>旧形式の後退行動の抽選重み。現行は距離帯別Candidates一覧を使う。</summary>
    [UnityEngine.Tooltip("旧形式の後退行動の抽選重み。現行は距離帯別Candidates一覧を使う。")]
    public float WeightBackstep = 20f;
    
    /// <summary>直前に実行した行動の重みを何倍にするか（0 = 完全に除外、1 = 変化なし）</summary>
    [Header("Behavior")]
    [Range(0f, 1f), Tooltip("直前に実行した行動の重みを何倍にするか（0 = 完全に除外、1 = 変化なし）")]
    public float RepeatPenalty = 0.25f;

    /// <summary>Enemyの行動候補と抽選時の基準重みを対応付ける。</summary>
    [System.Serializable]
    public struct ActionWeight
    {
        /// <summary>この抽選候補が実行するEnemy行動の種類。</summary>
        [UnityEngine.Tooltip("この抽選候補が実行するEnemy行動の種類。")]
        public EnemyActionType Action;
        /// <summary>この候補の抽選重み。大きいほど選ばれやすく、直前行動にはRepeatPenaltyを掛ける。</summary>
        [UnityEngine.Tooltip("この候補の抽選重み。大きいほど選ばれやすく、直前行動にはRepeatPenaltyを掛ける。")]
        public float Weight;
    }

    /// <summary>候補: 遠距離用の行動リストと重み</summary>
    [Header("Decision Candidates (Inspector editable lists)")]
    [Tooltip("候補: 遠距離用の行動リストと重み")]
    public ActionWeight[] FarCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.WarpAttack, Weight = 10f },
        new ActionWeight{ Action = EnemyActionType.Approach, Weight = 30f }
    };

    /// <summary>候補: 中距離用の行動リストと重み</summary>
    [Tooltip("候補: 中距離用の行動リストと重み")]
    public ActionWeight[] MidCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.Thrust, Weight = 30f },
        new ActionWeight{ Action = EnemyActionType.Approach, Weight = 30f },
        new ActionWeight{ Action = EnemyActionType.Wait, Weight = 20f }
    };

    /// <summary>候補: 近距離用の行動リストと重み</summary>
    [Tooltip("候補: 近距離用の行動リストと重み")]
    public ActionWeight[] NearCandidates = new ActionWeight[]
    {
        new ActionWeight{ Action = EnemyActionType.Slash, Weight = 50f },
        new ActionWeight{ Action = EnemyActionType.Wait, Weight = 20f },
        new ActionWeight{ Action = EnemyActionType.StepBack, Weight = 20f }
    };
}
