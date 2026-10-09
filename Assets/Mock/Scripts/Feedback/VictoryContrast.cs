using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>勝利専用Volumeの重みを実時間で制御し、決着から納刀・討伐表示への色の変化を担当する。</summary>
[DisallowMultipleComponent]
public sealed class VictoryContrast : MonoBehaviour
{
    /// <summary>勝利専用のHDRP Volume。通常Profileを書き換えずWeightでモノクロを制御する。</summary>
    [UnityEngine.Tooltip("勝利専用のHDRP Volume。通常Profileを書き換えずWeightでモノクロを制御する。")]
    [SerializeField] private Volume _volume;
    /// <summary>勝利から完全モノクロへ移行する時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("勝利から完全モノクロへ移行する時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _enter = .04f;
    /// <summary>完全モノクロを保持する時間（実時間の秒）。移行時間とは別に加算する。</summary>
    [UnityEngine.Tooltip("完全モノクロを保持する時間（実時間の秒）。移行時間とは別に加算する。")]
    [SerializeField, Min(0)] private float _hold = 2.08f;
    /// <summary>完全モノクロから元の色へ戻す時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("完全モノクロから元の色へ戻す時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _restore = .45f;
    private JustAvoidEnvelope _envelope;
    /// <summary>現在の重みから勝利用モノクロを開始する。時間倍率や共有Profileには書き込まない。</summary>
    public void Begin() { if (!isActiveAndEnabled) return; _envelope.Begin(Time.unscaledTime); RenderAt(Time.unscaledTime); }
    /// <summary>指定した実時刻に対応するVolumeの重みを反映する。</summary>
    /// <param name="now">Time.unscaledTimeと同じ基準の秒数。</param>
    public void RenderAt(float now) { if (_volume != null) _volume.weight = _envelope.Evaluate(now, _enter, _hold, _restore); }
    /// <summary>実時間で勝利専用モノクロの移行・保持・復帰を進める。</summary>
    private void Update() => RenderAt(Time.unscaledTime);
    /// <summary>勝利モノクロを即解除し、通常のVolume合成へ戻す。</summary>
    public void Cancel() { _envelope.Reset(); if (_volume != null) _volume.weight = 0; }
    /// <summary>Scene変更時に勝利用Volumeの重みをゼロへ戻す。</summary>
    private void SceneChanged(Scene previous, Scene next) => Cancel();
    /// <summary>Scene変更通知を購読してモノクロの持ち越しを防ぐ。</summary>
    private void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
    /// <summary>Scene購読を解除し、専用Volumeの重みを通常へ戻す。</summary>
    private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; Cancel(); }
    /// <summary>破棄時に専用Volumeのモノクロを解除する。</summary>
    private void OnDestroy() => Cancel();
}
