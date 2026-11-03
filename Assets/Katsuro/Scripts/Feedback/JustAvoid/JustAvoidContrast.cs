using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>ジャスト回避成功時だけ専用Volumeで世界をモノクロ化し、BGM Duckを同時に開始する。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(CameraManager))]
public sealed class JustAvoidContrast : MonoBehaviour
{
    /// <summary>Just Avoid成功専用のHDRP Volume。通常Profileを書き換えずWeightでモノクロを制御する。</summary>
    [UnityEngine.Tooltip("Just Avoid成功専用のHDRP Volume。通常Profileを書き換えずWeightでモノクロを制御する。")]
    [SerializeField] private Volume _volume;
    /// <summary>Just Avoid成功から完全モノクロへ移行する時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Just Avoid成功から完全モノクロへ移行する時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _enter = .02f;
    /// <summary>完全モノクロを保持する時間（実時間の秒）。移行時間とは別に加算する。</summary>
    [UnityEngine.Tooltip("完全モノクロを保持する時間（実時間の秒）。移行時間とは別に加算する。")]
    [SerializeField, Min(0)] private float _hold = .08f;
    /// <summary>完全モノクロから元の色へ戻す時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("完全モノクロから元の色へ戻す時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _restore = .20f;
    private JustAvoidEnvelope _envelope;
    private AudioManager _audio;
    private bool _blocked;
    /// <summary>勝利・敗北が優先中でなければ、現在値から回避用モノクロとBGM Duckを同時に開始する。</summary>
    public void Play(AudioManager audio)
    {
        if (!isActiveAndEnabled || _blocked || (FinalBlowManager.Instance != null && FinalBlowManager.Instance.IsPlaying)) return;
        _audio = audio;
        _envelope.Evaluate(Time.unscaledTime, _enter, _hold, _restore);
        _envelope.Begin(Time.unscaledTime);
        _audio?.PlayJustAvoidDuck();
        Update();
    }
    /// <summary>実時間の包絡線を専用Volume重みへ反映し、指定時間後に通常表示へ戻す。</summary>
    private void Update()
    {
        if (_volume != null) _volume.weight = _envelope.Evaluate(Time.unscaledTime, _enter, _hold, _restore);
    }
    /// <summary>専用Volumeと回避用BGM Duckを即解除する。BGMの再生状態は変更しない。</summary>
    public void Cancel()
    {
        _envelope.Reset();
        if (_volume != null) _volume.weight = 0;
        _audio?.CancelJustAvoidDuck();
    }
    /// <summary>アクティブScene変更時に回避用の色と音を通常へ戻す。</summary>
    private void SceneChanged(Scene previous, Scene next) => Cancel();
    /// <summary>SceneのUnload時に取り残された回避演出を解除する。</summary>
    private void SceneUnloaded(Scene scene) => Cancel();
    /// <summary>戦闘・Pause以外の進行状態では演出を解除し、新しい回避演出の開始を拒否する。</summary>
    private void GameStateChanged(GameManager.GameState state)
    {
        _blocked = state != GameManager.GameState.InGame && state != GameManager.GameState.Pause;
        if (_blocked) Cancel();
    }
    /// <summary>Scene変更とゲーム進行の通知を購読して中断時の復帰を保証する。</summary>
    private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; SceneManager.sceneUnloaded += SceneUnloaded; GameManager.OnGameStateChanged += GameStateChanged; }
    /// <summary>Scene・進行通知を解除し、回避演出の色と音を戻す。</summary>
    private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; SceneManager.sceneUnloaded -= SceneUnloaded; GameManager.OnGameStateChanged -= GameStateChanged; Cancel(); }
    /// <summary>破棄時に回避用Volumeと音量倍率を通常へ戻す。</summary>
    private void OnDestroy() => Cancel();
}
