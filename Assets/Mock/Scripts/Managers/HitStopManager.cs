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

    [SerializeField] private float _hitStopTime = 0.05f;

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

    /// <summary>
    /// 指定した対象の Animator を一時的に停止します（duration 秒、実時間）。
    /// 複数対象を渡せます。
    /// </summary>
    public void PlayHitStop(float durationRealtime = 0.06f, params GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return;
        StartCoroutine(DoHitStop(durationRealtime, targets));
    }

    private IEnumerator DoHitStop(float durationRealtime, GameObject[] targets)
    {
        var list = new List<(Animator animator, float prevSpeed)>();
        foreach (var go in targets)
        {
            if (go == null) continue;
            var anims = go.GetComponentsInChildren<Animator>(true);
            foreach (var a in anims)
            {
                if (a == null) continue;
                list.Add((a, a.speed));
                a.speed = 0f;
            }
        }

        yield return new WaitForSecondsRealtime(durationRealtime);

        foreach (var entry in list)
        {
            if (entry.animator != null)
            {
                entry.animator.speed = entry.prevSpeed;
            }
        }
    }
}
