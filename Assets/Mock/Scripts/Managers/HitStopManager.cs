using System.Collections.Generic;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }
    [SerializeField] private float _hitStopTime = .05f;
    [SerializeField] private float _lastHitStopTime = .2f;
    public float HitStopTime => _hitStopTime;
    public float LastHitStopTime => _lastHitStopTime;
    private readonly Dictionary<GameObject, AnimationSpeedController[]> _targets = new();
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
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void PlayHitStop(float durationRealtime = .06f, params GameObject[] targets)
        => PlayHitStopSlow(durationRealtime, 0f, targets);
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
    private void OnDisable()
    {
        foreach (var speed in affected) if (speed != null) speed.ClearTemporary();
        affected.Clear();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
