using System.Collections.Generic;
using UnityEngine;

/// <summary>登録済み対象のAnimationSpeedControllerへ実時間期限の停止・減速を要求する。TimeScaleは変更しない。</summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }
    /// <summary>通常Light命中のHitStop時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("通常Light命中のHitStop時間（実時間の秒）。")]
    [SerializeField] private float _hitStopTime = .05f;
    /// <summary>最終Hit用に公開するHitStop時間設定（実時間の秒）。FinalBlowManagerには専用時間設定もある。</summary>
    [UnityEngine.Tooltip("最終Hit用に公開するHitStop時間設定（実時間の秒）。FinalBlowManagerには専用時間設定もある。")]
    [SerializeField] private float _lastHitStopTime = .2f;
    /// <summary>通常Heavy命中のHitStop時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("通常Heavy命中のHitStop時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _heavyHitStop = .075f;
    /// <summary>通常Light命中のHitStop時間（実時間の秒）。</summary>
    public float LightHitStop => _hitStopTime;
    /// <summary>通常Heavy命中のHitStop時間（実時間の秒）。</summary>
    public float HeavyHitStop => _heavyHitStop;
    /// <summary>通常Light命中のHitStop時間（実時間の秒）。</summary>
    public float HitStopTime => _hitStopTime;
    /// <summary>最終Hit用に公開するHitStop時間設定（実時間の秒）。FinalBlowManagerには専用時間設定もある。</summary>
    public float LastHitStopTime => _lastHitStopTime;
    private readonly Dictionary<GameObject, AnimationSpeedController[]> _targets = new();
    /// <summary>対象階層の速度制御を初期化し、子オブジェクトからも同じ対象へ停止を要求できるよう登録する。</summary>
    public void RegisterTarget(GameObject owner)
    {
        if (owner == null) return;
        var speeds = owner.GetComponentsInChildren<AnimationSpeedController>(true);
        foreach (var speed in speeds) speed.Init();
        foreach (var child in owner.GetComponentsInChildren<Transform>(true))
            _targets[child.gameObject] = speeds;
        var stale = new List<GameObject>();
        foreach (var entry in _targets) if (entry.Key == null) stale.Add(entry.Key);
        foreach (var key in stale) _targets.Remove(key);
    }
    private readonly List<AnimationSpeedController> affected = new();
    private bool _initialized;
    /// <summary>永続する共有HitStopManagerを確立し、重複を除去する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    /// <summary>登録済みの対象へ指定実時間の完全停止を要求する。TimeScaleは変更しない。</summary>
    /// <param name="durationRealtime">完全停止する実時間秒数。</param>
    /// <param name="targets">あらかじめRegisterTargetで登録した対象またはその子。</param>
    public void PlayHitStop(float durationRealtime = .06f, params GameObject[] targets)
        => PlayHitStopSlow(durationRealtime, 0f, targets);
    /// <summary>登録済み対象のAnimatorへ一時速度を設定し、終了時の解除対象として保持する。</summary>
    /// <param name="durationRealtime">一時倍率の実時間の有効秒数。</param>
    /// <param name="slowSpeed">一時的なAnimator速度倍率。ゼロなら完全停止。</param>
    /// <param name="targets">登録済みの停止・減速対象。</param>
    public void PlayHitStopSlow(float durationRealtime, float slowSpeed, params GameObject[] targets)
    {
        if (!isActiveAndEnabled || targets == null) return;
        affected.RemoveAll(item => item == null);
        foreach (var target in targets)
        {
            if (target == null) continue;
            if (!_targets.TryGetValue(target, out var speeds)) continue;
            foreach (var speed in speeds)
            {
                if (speed == null) continue;
                speed.SetTemporary(slowSpeed, durationRealtime);
                if (!affected.Contains(speed)) affected.Add(speed);
            }
        }

    }
    /// <summary>自身が影響を与えた速度制御の一時倍率を全解除する。</summary>
    private void OnDisable()
    {
        foreach (var speed in affected) if (speed != null) speed.ClearTemporary();
        affected.Clear();
    }
    /// <summary>自身が共有インスタンスの場合に参照を解除する。</summary>
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
