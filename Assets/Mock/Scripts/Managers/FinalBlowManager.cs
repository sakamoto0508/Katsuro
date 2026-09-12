using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;

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
    [SerializeField] private float _whiteFlashDuration = 0.18f;
    [SerializeField] private float _phase2Duration = 1.6f;
    [SerializeField] private TextMeshProUGUI _finalBlowText;
    [SerializeField] private float _finalBlowTextFadeIn = 0.5f;
    [SerializeField] private Ease _ease = Ease.InQuint;
    private bool _isPlaying;

    private GameManager _game;
    private AudioManager _audio;
    private HitStopManager _hitStop;
    private LoadSceneManager _loader;
    private GlobalFader _fader;
    private bool _initialized;
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
            _finalBlowText.gameObject.SetActive(false);
        }
    }

    public void StartFinalBlow()
    {
        if (_isPlaying || _enemyController == null || _player == null) return;
        _isPlaying = true;
        _game?.WinGame();
        DoFinalBlow().Forget();
    }

    private async UniTaskVoid DoFinalBlow()
    {
        var token = this.GetCancellationTokenOnDestroy();
        // フェーズ1: ヒットストップ（敵の Animator を一時停止）とプレイヤーの短時間スロー
        // 注意: 呼び出し元が player を null で渡しているとスローが適用されないため、

        _hitStop?.PlayHitStop(_phase1HitStop, _enemyController.gameObject);
        // BGM を停止し、敵の死亡SEを再生する
        if (_audio != null)
        {
            _audio.StopAllBGMs();
            if (_audioConfig != null) _audio.PlaySE(_audioConfig.EnemyDeadSound);
        }

        // プレイヤーは完全停止ではなくスローにする（例: 0.3 の速度）
        _hitStop?.PlayHitStopSlow(0.2f, 0.3f, _player.gameObject);

        if (_finalBlowText != null)
        {
            _finalBlowText.gameObject.SetActive(true);
            // 初期 alpha をゼロにする
            var col = _finalBlowText.color;
            col.a = 0f;
            _finalBlowText.color = col;

            // 既存 Tween を止め、unscaled でフェードイン
            _finalBlowText.DOKill();
            _finalBlowText.DOFade(1f, _finalBlowTextFadeIn)
                .SetEase(_ease)
                .SetUpdate(true);
        }
        // UniTask のバージョンに合わせてミリ秒で待機（実時間）
        await UniTask.Delay((int)(Mathf.Max(0f, _whiteFlashDuration) * 1000), ignoreTimeScale: true, cancellationToken: token);
        // player のスローは上のコルーチンが終了すると自動で元に戻るため、ここで再設定はしない
        _player.AnimController.PlayTrigger(_player.AnimController.AnimName.SwordSheathing);
        await UniTask.Delay((int)(Mathf.Max(0f, _phase2Duration) * 1000), ignoreTimeScale: true, cancellationToken: token);
        var config = _loader != null ? _loader.SceneNameConfig : null;
        await _fader.FadeToScene(config != null ? config.TitleScene : "TitleScene");
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (_finalBlowText != null) _finalBlowText.DOKill();
    }
}
