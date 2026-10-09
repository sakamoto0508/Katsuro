using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>勝利専用Volumeの重みを実時間で制御し、決着から納刀・討伐表示への色の変化を担当する。</summary>
[DisallowMultipleComponent]
public sealed class VictoryContrast : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField, Min(0)] private float _enter = .04f;
    [SerializeField, Min(0)] private float _hold = 2.08f;
    [SerializeField, Min(0)] private float _restore = .45f;
    private JustAvoidEnvelope _envelope;
    /// <summary>現在の重みから勝利用モノクロを開始する。時間倍率や共有Profileには書き込まない。</summary>
    public void Begin() { if (!isActiveAndEnabled) return; _envelope.Begin(Time.unscaledTime); RenderAt(Time.unscaledTime); }
    /// <summary>指定した実時刻に対応するVolumeの重みを反映する。</summary>
    /// <param name="now">Time.unscaledTimeと同じ基準の秒数。</param>
    public void RenderAt(float now) { if (_volume != null) _volume.weight = _envelope.Evaluate(now, _enter, _hold, _restore); }
    private void Update() => RenderAt(Time.unscaledTime);
    /// <summary>勝利モノクロを即解除し、通常のVolume合成へ戻す。</summary>
    public void Cancel() { _envelope.Reset(); if (_volume != null) _volume.weight = 0; }
    private void SceneChanged(Scene previous, Scene next) => Cancel();
    private void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
    private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; Cancel(); }
    private void OnDestroy() => Cancel();
}
