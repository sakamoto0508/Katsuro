using System.Collections.Generic;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }
    [SerializeField] private float _hitStopTime = .05f;
    [SerializeField] private float _lastHitStopTime = .2f;
    public float HitStopTime => _hitStopTime;
    public float LastHitStopTime => _lastHitStopTime;
    private readonly List<Animator> animators = new();
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
            target.GetComponentsInChildren(true, animators);
            foreach (var animator in animators)
            {
                var speed = AnimationSpeedController.For(animator);
                speed.Init();
                speed.SetTemporary(slowSpeed, durationRealtime);
                if (!affected.Contains(speed)) affected.Add(speed);
            }
        }
        animators.Clear();
    }
    private void OnDisable()
    {
        foreach (var speed in affected) if (speed != null) speed.ClearTemporary();
        affected.Clear();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
