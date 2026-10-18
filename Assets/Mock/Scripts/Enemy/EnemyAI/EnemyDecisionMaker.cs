using UnityEngine;

/// <summary>抽選結果。移動・待機にはAttackを持たせない。</summary>
public readonly struct EnemyDecision
{
    public EnemyActionType Action { get; }
    public EnemyAttackData Attack { get; }
    public EnemyDecision(EnemyActionType action, EnemyAttackData attack = null)
    {
        Action = action;
        Attack = IsAttackAction(action) ? attack : null;
    }
    public static bool IsAttackAction(EnemyActionType action) =>
        action == EnemyActionType.Slash || action == EnemyActionType.Slash2 ||
        action == EnemyActionType.HeavySlash || action == EnemyActionType.HeavySlash2 ||
        action == EnemyActionType.Thrust || action == EnemyActionType.WarpAttack;
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

    public EnemyDecision DecideAction(float distance, EnemyDecision last, EnemyDecisionConfig config)
    {
        var wait = new EnemyDecision(EnemyActionType.Wait);
        if (config == null) return wait;
        var candidates = distance > config.FarDistance ? config.FarCandidates
            : distance > config.NearDistance ? config.MidCandidates : config.NearCandidates;
        if (candidates == null) return wait;
        float penalty = float.IsNaN(config.RepeatPenalty) ? 1f : Mathf.Clamp01(config.RepeatPenalty);
        double total = 0;
        foreach (var candidate in candidates) total += Weight(candidate, last, penalty, config.RepeatPenaltyPerAttack);
        if (total <= 0) return wait;
        double remaining = _rand.NextDouble() * total;
        var fallback = wait;
        foreach (var candidate in candidates)
        {
            float weight = Weight(candidate, last, penalty, config.RepeatPenaltyPerAttack);
            if (weight <= 0) continue;
            fallback = new EnemyDecision(candidate.Action, candidate.Attack);
            remaining -= weight;
            if (remaining < 0) return fallback;
        }
        return fallback;
    }

    private static float Weight(EnemyDecisionConfig.ActionWeight candidate, EnemyDecision last, float penalty, bool perAttack)
    {
        float weight = candidate.Weight;
        if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f) return 0f;
        if ((int)candidate.Action < (int)EnemyActionType.Approach || (int)candidate.Action > (int)EnemyActionType.Wait) return 0f;
        bool repeat = perAttack && EnemyDecision.IsAttackAction(candidate.Action) && candidate.Attack != null
            ? candidate.Attack == last.Attack : candidate.Action == last.Action;
        return repeat ? weight * penalty : weight;
    }
}
