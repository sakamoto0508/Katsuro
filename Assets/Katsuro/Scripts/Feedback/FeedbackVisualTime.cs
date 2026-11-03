using UnityEngine;

/// <summary>命中表示用の実時間を数え、ゲーム全体のPause中だけ進行を止める。AnimatorのHitStopでは止めない。</summary>
internal static class FeedbackVisualTime
{
    private static int _frame = -1;
    private static float _elapsed;
    private static bool _gamePaused;

    /// <summary>時間停止またはゲーム進行のPause通知を受けている間、演出の更新を止める。</summary>
    public static bool IsPaused => Time.timeScale <= 0f || _gamePaused;

    /// <summary>現在フレームまでのPauseを除いた実時間。スロー中も実時間で短い斬撃表示を減衰させる。</summary>
    public static float Now
    {
        get
        {
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                if (!IsPaused) _elapsed += Time.unscaledDeltaTime;
            }
            return _elapsed;
        }
    }

    /// <summary>Domain Reloadを無効にしたPlay開始でも、前回の演出時計を持ち越さない。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _frame = -1; _elapsed = 0f; _gamePaused = false;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    /// <summary>TimeScaleを変更しない中断画面でも、命中演出だけは停止する。</summary>
    private static void OnGameStateChanged(GameManager.GameState state) { _gamePaused = state == GameManager.GameState.Pause; }
}
