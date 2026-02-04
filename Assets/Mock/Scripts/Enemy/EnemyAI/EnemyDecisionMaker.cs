using System;
using System.Collections.Generic;

/// <summary>
/// 敵の行動決定ロジックを保持するクラス（MonoBehaviour ではないため単体でテスト可能）。
/// 距離や前回の行動、設定値に基づいて次に実行すべき EnemyActionType を返します。
/// </summary>
public sealed class EnemyDecisionMaker
{
    public EnemyDecisionMaker() { }
    private readonly System.Random _rand = new();

    /// <summary>
    /// 次に実行すべき行動を決定します。
    /// </summary>
    /// <param name="distance">敵とターゲット間の距離（ワールド単位）</param>
    /// <param name="lastAction">直前に実行した行動</param>
    /// <param name="config">行動決定に使用する重みや閾値を格納した設定オブジェクト</param>
    /// <returns>選択された <see cref="EnemyActionType"/></returns>
    public EnemyActionType Decide(float distance, EnemyActionType lastAction, EnemyDecisionConfig config)
    {
        if (config == null) return EnemyActionType.Wait;

        // 小さな改善: 直前と同じ行動が連続して選ばれにくくするため、重みにペナルティを与える
        const float repeatPenalty = 0.25f; // 直前と同じ行動は 25% に減らす

        if (distance > config.FarDistance)
        {
            var candidates = new (EnemyActionType action, float weight)[]
            {
                (EnemyActionType.WarpAttack, config.WeightWarpAttack),
                (EnemyActionType.Approach, config.WeightApproach)
            };

            // 直前と同じ行動は候補から除外して連続選択を防ぐ（重みを 0 にする）
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].action == lastAction) candidates[i] = (candidates[i].action, 0f);
            }
            return Sample(candidates);
        }
        else if (distance > config.NearDistance)
        {
            var candidates = new (EnemyActionType action, float weight)[]
            {
                (EnemyActionType.Thrust, config.WeightRush),
                (EnemyActionType.Approach, config.WeightApproach),
                (EnemyActionType.Wait, config.WeightObserve)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].action == lastAction) candidates[i] = (candidates[i].action, 0f);
            }
            return Sample(candidates);
        }
        else
        {
            var candidates = new List<(EnemyActionType action, float weight)>()
            {
                (EnemyActionType.Slash, config.WeightSlash),
                (EnemyActionType.Wait, config.WeightObserve),
                (EnemyActionType.StepBack, config.WeightBackstep)
            };

            if (lastAction == EnemyActionType.Wait)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].action == EnemyActionType.Wait)
                        candidates[i] = (candidates[i].action, 0f);
                }
            }

            return Sample(candidates.ToArray());
        }
    }

    /// <summary>
    /// 候補の中から確率的に一つをサンプリングして返します。
    /// 重みは候補タプルの weight によって指定され、負の値は無視されます。
    /// </summary>
    /// <param name="candidates">(action, weight) の配列</param>
    /// <returns>選択された <see cref="EnemyActionType"/></returns>
    private EnemyActionType Sample((EnemyActionType action, float weight)[] candidates)
    {
        float total = 0f;
        foreach (var c in candidates) total += Math.Max(0f, c.weight);
        if (total <= 0f) return EnemyActionType.Wait;
        double r = _rand.NextDouble() * total; // Random value based on total weight
        double acc = 0;
        foreach (var c in candidates)
        {
            acc += Math.Max(0f, c.weight);
            if (r <= acc) return c.action;
        }
        return candidates[candidates.Length - 1].action; // Return last candidate if no action selected
    }
}
