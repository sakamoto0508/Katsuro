using UnityEngine;

/// <summary>抽選結果。移動・待機にはAttackを持たせない。</summary>
public readonly struct EnemyDecision
{
    public EnemyActionType Action { get; }
    public EnemyAttackData Attack { get; }
    public EnemyAttackPattern Pattern { get; }
    public EnemyDecision(EnemyActionType action, EnemyAttackData attack = null, EnemyAttackPattern pattern = null)
    {
        Action = action;
        Pattern = IsAttackAction(action) ? pattern : null;
        Attack = IsAttackAction(action) && Pattern == null ? attack : null;
    }
    public static bool IsAttackAction(EnemyActionType action) =>
        action == EnemyActionType.Slash || action == EnemyActionType.Slash2 ||
        action == EnemyActionType.HeavySlash || action == EnemyActionType.HeavySlash2 ||
        action == EnemyActionType.Thrust || action == EnemyActionType.WarpAttack || action == EnemyActionType.RushAttack;
}

/// <summary>距離帯の候補から既存の重み付き抽選で行動と攻撃データを選ぶ。</summary>
public sealed class EnemyDecisionMaker
{
    private readonly System.Random _rand;
    public EnemyDecisionMaker() : this(new System.Random()) { }
    public EnemyDecisionMaker(System.Random random) { _rand = random ?? throw new System.ArgumentNullException(nameof(random)); }

    // 旧呼び出し元との互換用。新しいAIは攻撃参照も返すDecideActionを使用する。
    public EnemyActionType Decide(float distance, EnemyActionType lastAction, EnemyDecisionConfig config) =>
        DecideAction(distance, new EnemyDecision(lastAction), config).Action;

    public EnemyDecision DecideAction(float distance, EnemyDecision last, EnemyDecisionConfig config,
        System.Func<EnemyDecisionConfig.ActionWeight, bool> canUse = null)
    {
        var wait = new EnemyDecision(EnemyActionType.Wait);
        if (config == null || !float.IsFinite(distance) || distance < 0f) return wait;
        var candidates = distance > config.FarDistance ? config.FarCandidates
            : distance > config.NearDistance ? config.MidCandidates : config.NearCandidates;
        if (candidates == null) return wait;
        float penalty = float.IsNaN(config.RepeatPenalty) ? 1f : Mathf.Clamp01(config.RepeatPenalty);
        double total = 0;
        foreach (var candidate in candidates) total += Weight(candidate, last, penalty, config.RepeatPenaltyPerAttack, distance, config, canUse);
        if (total <= 0) return wait;
        double remaining = _rand.NextDouble() * total;
        var fallback = wait;
        foreach (var candidate in candidates)
        {
            float weight = Weight(candidate, last, penalty, config.RepeatPenaltyPerAttack, distance, config, canUse);
            if (weight <= 0) continue;
            fallback = new EnemyDecision(candidate.Action, candidate.Attack, candidate.Pattern);
            remaining -= weight;
            if (remaining < 0) return fallback;
        }
        return fallback;
    }

    private static float Weight(EnemyDecisionConfig.ActionWeight candidate, EnemyDecision last, float penalty, bool perAttack,
        float distance, EnemyDecisionConfig config, System.Func<EnemyDecisionConfig.ActionWeight, bool> canUse)
    {
        float weight = candidate.Weight;
        if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f) return 0f;
        if ((int)candidate.Action < 0 || (int)candidate.Action > (int)EnemyActionType.RushAttack) return 0f;
        if (candidate.Pattern != null && (!EnemyDecision.IsAttackAction(candidate.Action) || !candidate.Pattern.IsValid)) return 0f;
        if (candidate.Pattern != null && !candidate.Pattern.IsInSelectionRange(distance, config)) return 0f;
        if ((candidate.Action == EnemyActionType.RushAttack || candidate.Action == EnemyActionType.WarpAttack) &&
            (candidate.Pattern == null || candidate.Pattern.Attack.ActionType != candidate.Action)) return 0f;
        if (canUse != null && !canUse(candidate)) return 0f;
        bool repeat = candidate.Action == last.Action;
        if (perAttack && candidate.Pattern != null) repeat = candidate.Pattern == last.Pattern;
        else if (perAttack && EnemyDecision.IsAttackAction(candidate.Action) && candidate.Attack != null)
            repeat = last.Pattern == null && candidate.Attack == last.Attack;
        return repeat ? weight * penalty : weight;
    }
}
