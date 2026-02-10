using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
/// <summary>
/// ステータス効果の管理コンポーネント。
/// 敵オブジェクトにアタッチされ、適用された効果の残り時間やスタック管理、
/// アニメータ／NavMeshAgent への速度乗算反映、VFX の生成・破棄を行います。
/// </summary>
public class StatusEffectManager : MonoBehaviour, IStatusEffectReceiver
{
    // 現在適用中の効果インスタンス一覧
    private readonly List<StatusEffectInstance> _effects = new();
    // 敵の Animator / NavMeshAgent（存在する場合）
    private Animator _animator;
    private UnityEngine.AI.NavMeshAgent _agent;
    // 効果適用時に乗算するための基準値（Awake 時にキャッシュ）
    private float _baseAgentSpeed = 0f;
    private float _baseAnimatorSpeed = 1f;

    private void Awake()
    {
        // コンポーネント参照を取得し、現在の速度値を基準値としてキャッシュする。
        // これによりエフェクトのオン/オフで正しく乗算できる。
        _animator = GetComponent<Animator>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        // 乗算の基準となる速度を保存
        if (_agent != null) _baseAgentSpeed = _agent.speed;
        if (_animator != null) _baseAnimatorSpeed = _animator.speed;
    }

    private void Update()
    {
        // 各効果の残り時間を減算し、期限切れは解除する。
        if (_effects.Count == 0) return;
        float dt = Time.deltaTime;
        bool changed = false;
        for (int i = _effects.Count - 1; i >= 0; --i)
        {
            var inst = _effects[i];
            inst.Remaining -= dt;
            if (inst.Remaining <= 0f)
            {
                // 期限切れ: 表示 VFX を破棄してリストから削除
                if (inst.Vfx != null) Destroy(inst.Vfx);
                _effects.RemoveAt(i);
                changed = true;
            }
        }
        // 変化があれば修飾値を再計算して反映
        if (changed) RecalculateModifiers();
    }

    public void ApplyStatusEffect(StatusEffectInstance instance)
    {
        // 受け取った効果インスタンスをリストに追加、または既存と合成する
        if (instance == null || instance.Def == null) return;
        var existing = _effects.Find(e => e.Def.Id == instance.Def.Id);
        if (existing != null)
        {
            // 同一効果が既にある場合は定義に従って Refresh / Replace / Stack を行う
            switch (instance.Def.Stacking)
            {
                case StatusEffectDef.StackPolicy.Refresh:
                    existing.Remaining = instance.Def.Duration;
                    break;
                case StatusEffectDef.StackPolicy.Replace:
                    existing.Remaining = instance.Def.Duration;
                    existing.Stacks = 1;
                    break;
                case StatusEffectDef.StackPolicy.Stack:
                    existing.Stacks = Mathf.Min(existing.Stacks + 1, instance.Def.MaxStacks);
                    existing.Remaining = instance.Def.Duration;
                    break;
            }
        }
        else
        {
            // 新規効果として追加。VFX が設定されていれば敵の子として生成する
            _effects.Add(instance);
            if (instance.Def.VfxPrefab != null)
            {
                instance.Vfx = Instantiate(instance.Def.VfxPrefab, transform);
            }
        }
        // 効果が変化したので最終的な修飾値を反映する
        RecalculateModifiers();
    }

    public void RemoveStatusEffect(string id)
    {
        for (int i = _effects.Count - 1; i >= 0; --i)
        {
            if (_effects[i].Def.Id == id)
            {
                if (_effects[i].Vfx != null) Destroy(_effects[i].Vfx);
                _effects.RemoveAt(i);
            }
        }
        RecalculateModifiers();
    }

    public bool HasStatusEffect(string id)
    {
        return _effects.Exists(e => e.Def.Id == id);
    }

    private void RecalculateModifiers()
    {
        // 全エフェクトを乗算で合成して最終倍率を算出する
        float speedMul = 1f;
        float animMul = 1f;
        foreach (var e in _effects)
        {
            // スタック数分だけ乗算（例: 0.8^stacks）で効果を重ねる
            float s = Mathf.Pow(e.Def.SpeedMultiplier, e.Stacks);
            float a = Mathf.Pow(e.Def.AnimationSpeedMultiplier, e.Stacks);
            speedMul *= s;
            animMul *= a;
        }

        // NavMeshAgent の速度と Animator.speed に対して基準値に倍率を掛けて適用する
        if (_agent != null)
        {
            _agent.speed = _baseAgentSpeed * speedMul;
        }

        if (_animator != null)
        {
            _animator.speed = _baseAnimatorSpeed * animMul;
        }
    }
}
