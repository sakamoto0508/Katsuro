using System;
using UnityEngine;

/// <summary>設定から直接、重み付きで行動を選ぶ。判断のたびに候補領域を確保しない。</summary>
public sealed class EnemyDecisionMaker
{
    private readonly System.Random _rand = new();

    /// <summary>距離帯の候補から無効な重みを除外し、連続選択の補正を含めて行動を抽選する。</summary>
    /// <returns>選ばれた行動。有効候補がない場合はWait。</returns>
    public EnemyActionType Decide(float distance, EnemyActionType lastAction, EnemyDecisionConfig config)
    {
        if (config == null) return EnemyActionType.Wait;
        var candidates = distance > config.FarDistance ? config.FarCandidates
            : distance > config.NearDistance ? config.MidCandidates : config.NearCandidates;
        if (candidates == null) return EnemyActionType.Wait;

        float repeatPenalty = Mathf.Clamp01(config.RepeatPenalty);
        double total = 0;
        foreach (var candidate in candidates)
            total += Weight(candidate, lastAction, repeatPenalty);
        if (total <= 0) return EnemyActionType.Wait;

        double remaining = _rand.NextDouble() * total;
        var fallback = EnemyActionType.Wait;
        foreach (var candidate in candidates)
        {
            float weight = Weight(candidate, lastAction, repeatPenalty);
            if (weight <= 0) continue;
            fallback = candidate.Action;
            remaining -= weight;
            if (remaining < 0) return candidate.Action;
        }
        return fallback;
    }

    /// <summary>候補の重みを検証し、直前と同じ行動には繰り返し補正を適用する。</summary>
    /// <returns>抽選に使用する有効重み。無効値ならゼロ。</returns>
    private static float Weight(EnemyDecisionConfig.ActionWeight candidate, EnemyActionType lastAction, float repeatPenalty)
    {
        float weight = candidate.Weight;
        if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f) return 0f;
        return candidate.Action == lastAction ? weight * repeatPenalty : weight;
    }
}
