using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;
using System;
using System.Threading;

/// <summary>
/// 最後の一撃（フィニッシュ）演出を制御するマネージャー。
/// </summary>
public class FinalBlowManager : MonoBehaviour
{
    public static FinalBlowManager Instance { get; private set; }

    [SerializeField] private AudioConfig _audioConfig;
    [SerializeField] private PlayerController _player;
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private float _phase1HitStop = 0.2f;
    [SerializeField, Range(.08f, .12f)] private float _whiteFlashDuration = .10f;
    [Tooltip("納刀開始からScene Fade開始まで。納刀Clip 2.07秒を見せる。")]
    [SerializeField, Min(0f)] private float _phase2Duration = 2.3f;
    [SerializeField] private TextMeshProUGUI _finalBlowText;
    [SerializeField] private float _finalBlowTextFadeIn = .22f;
    [SerializeField] private FinalBlowPresentation _presentation;
    [SerializeField] private CameraManager _cameraFeedback;
    [SerializeField, Min(0f)] private float _cameraPushDelay = .18f;
    [SerializeField, Min(0f)] private float _sheathingDelay = .8f;
    [SerializeField, Min(0f)] private float _textDelay = 2.12f;
    private CancellationTokenSource _sequenceCancellation;
    private bool _isPlaying;
    /// <summary>Final Blowが制御権を持っている間はtrue。回避など低優先演出の開始抑制に使用する。</summary>
    public bool IsPlaying => _isPlaying;

    private GameManager _game;
    private AudioManager _audio;
    private HitStopManager _hitStop;
    private LoadSceneManager _loader;
    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>勝利演出に使用するゲーム・Audio・HitStop・遷移・Faderを接続し、討伐文字を待機状態へ戻す。</summary>
    public void Init(GameManager game, AudioManager audio, HitStopManager hitStop, LoadSceneManager loader, GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game; _audio = audio; _hitStop = hitStop; _loader = loader; _fader = fader;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (_finalBlowText != null)
        {
            _finalBlowText.DOKill();
            _finalBlowText.gameObject.SetActive(false);
        }
    }

    /// <summary>重複開始を防いで回避の色と音を解除し、勝利用モノクロ・UI・決着シーケンスを開始する。</summary>
    public void StartFinalBlow()
    {
        if (_isPlaying || _sequenceCancellation != null || !isActiveAndEnabled || _enemyController == null || _player == null) return;
        _isPlaying = true;
        _cameraFeedback?.GetComponent<JustAvoidContrast>()?.Cancel();
        _audio?.CancelJustAvoidDuck();
        _cameraFeedback?.GetComponent<VictoryContrast>()?.Begin();
        _sequenceCancellation = new CancellationTokenSource();
        if (_presentation != null) _presentation.Begin(_finalBlowText, _whiteFlashDuration, _textDelay, _finalBlowTextFadeIn);
        _game?.WinGame();
        DoFinalBlow().Forget();
    }

    /// <summary>HitStopとBGM停止から専用カメラ、納刀、討伐、既存Scene Fadeへ実時間で進め、中断時も一時状態を片付ける。</summary>
    private async UniTask DoFinalBlow()
    {
        var source = _sequenceCancellation;
        var token = source.Token;
        float started = Time.unscaledTime;
        try
        {
            _hitStop?.PlayHitStop(_phase1HitStop, _enemyController.gameObject);
            _hitStop?.PlayHitStopSlow(_phase1HitStop, .3f, _player.gameObject);
            if (_audio != null)
            {
                _audio.StopAllBGMs();
                if (_audioConfig != null) _audio.PlaySE(_audioConfig.EnemyDeadSound);
            }
            // FOVの優先権は成立直後に取る。寄り始めだけ短く遅らせる。
            if (_cameraFeedback != null) _cameraFeedback.PlayFinalBlowFeedback(_cameraPushDelay);
            await WaitUntil(started, Mathf.Max(_cameraPushDelay, _sheathingDelay), token);
            if (_player != null && _player.AnimController != null)
                _player.AnimController.PlayTrigger(_player.AnimController.AnimName.SwordSheathing);
            await WaitUntil(started, Mathf.Max(_sheathingDelay + _phase2Duration, _textDelay + _finalBlowTextFadeIn), token);
            var config = _loader != null ? _loader.SceneNameConfig : null;
            if (_fader != null) await _fader.FadeToScene(config != null ? config.TitleScene : "TitleScene").AttachExternalCancellation(token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            Cleanup();
            if (_sequenceCancellation == source) _sequenceCancellation = null;
            source.Dispose();
        }
    }

    /// <summary>開始基準の実時刻までUpdateで待機し、TimeScaleに左右されずキャンセルを受け付ける。</summary>
    private static async UniTask WaitUntil(float started, float seconds, CancellationToken token)
    {
        while (Time.unscaledTime - started < seconds) await UniTask.Yield(PlayerLoopTiming.Update, token);
        token.ThrowIfCancellationRequested();
    }

    /// <summary>進行中の非同期演出をキャンセルし、勝利UI・Volume・カメラを復元する。</summary>
    public void CancelPresentation()
    {
        _sequenceCancellation?.Cancel();
        Cleanup();
    }
    /// <summary>演出の一時状態と速度補正を解除する。勝利後の入力停止はGameManagerに任せて保持する。</summary>
    private void Cleanup()
    {
        _cameraFeedback?.GetComponent<VictoryContrast>()?.Cancel();
        if (_presentation != null) _presentation.ResetPresentation();
        if (_cameraFeedback != null) _cameraFeedback.StopFinalBlowFeedback();
        if (_finalBlowText != null) _finalBlowText.DOKill();
        if (!_isPlaying) return;
        if (_player != null) foreach (var speed in _player.GetComponentsInChildren<AnimationSpeedController>(true)) if (speed != null) speed.ClearTemporary();
        if (_enemyController != null) foreach (var speed in _enemyController.GetComponentsInChildren<AnimationSpeedController>(true)) if (speed != null) speed.ClearTemporary();
        _isPlaying = false;
        // Victoryの入力停止はGameManagerが所有する。演出Cleanupで再有効化しない。
    }
    /// <summary>勝利シーケンスを中断して表示とカメラ・Volumeを片付ける。</summary>
    private void OnDisable() => CancelPresentation();
    /// <summary>勝利演出を中断し、自身が共有インスタンスなら参照を消去する。</summary>
    private void OnDestroy()
    {
        CancelPresentation();
        if (Instance == this) Instance = null;
    }
}
