using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シンプルなヒットストップ実装（Animator を一時停止する方式）。
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }
    public float HitStopTime => _hitStopTime;
    public float LastHitStopTime => _lastHitStopTime;

    [SerializeField] private float _hitStopTime = 0.05f;
    [SerializeField] private float _lastHitStopTime = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Track applied temporary speed changes per Animator so multiple overlapping calls restore correctly.
    private class AnimatorEntry { public float OriginalSpeed; public int RefCount; }
    private readonly Dictionary<Animator, AnimatorEntry> _animatorEntries = new();

    /// <summary>
    /// 指定した対象の Animator を一時的に停止します（duration 秒、実時間）。
    /// 複数対象を渡せます。
    /// 内部では PlayHitStopSlow に speed=0 を渡すラッパとして動作し、重複呼び出しに対応します。
    /// </summary>
    public void PlayHitStop(float durationRealtime = 0.06f, params GameObject[] targets)
    {
        PlayHitStopSlow(durationRealtime, 0f, targets);
    }

    /// <summary>
    /// 一時的に対象の Animator の再生速度を指定値に変更し、duration 秒後に復帰させます。
    /// 複数回呼ばれた場合は参照カウント方式で最後の解除時に元の速度に戻します。
    /// </summary>
    public void PlayHitStopSlow(float durationRealtime, float slowSpeed, params GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return;
        foreach (var go in targets)
        {
            if (go == null) continue;
            var anims = go.GetComponentsInChildren<Animator>(true);
            foreach (var a in anims)
            {
                if (a == null) continue;
                ApplyTempSpeed(a, slowSpeed, durationRealtime);
            }
        }
    }

    private void ApplyTempSpeed(Animator animator, float speed, float duration)
    {
        if (animator == null) return;

        if (!_animatorEntries.TryGetValue(animator, out var entry))
        {
            entry = new AnimatorEntry { OriginalSpeed = animator.speed, RefCount = 0 };
            _animatorEntries[animator] = entry;
        }

        entry.RefCount++;
        animator.speed = speed;
        StartCoroutine(ReleaseAfterDelay(animator, duration));
    }

    private IEnumerator ReleaseAfterDelay(Animator animator, float duration)
    {
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return null;

        if (animator == null) yield break;
        if (!_animatorEntries.TryGetValue(animator, out var entry)) yield break;

        entry.RefCount--;
        if (entry.RefCount <= 0)
        {
            try
            {
                animator.speed = entry.OriginalSpeed;
            }
            catch { }
            _animatorEntries.Remove(animator);
        }
    }
}
