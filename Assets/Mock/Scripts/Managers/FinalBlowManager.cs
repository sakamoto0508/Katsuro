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
        if (_finalBlowText != null)
        {
            _finalBlowText.gameObject.SetActive(false);
        }
    }

    public void StartFinalBlow()
    {
        if (_enemyController == null) return;
        DoFinalBlow().Forget();
    }

    private async UniTaskVoid DoFinalBlow()
    {
        // フェーズ1: ヒットストップ（敵の Animator を一時停止）とプレイヤーの短時間スロー
        // 注意: 呼び出し元が player を null で渡しているとスローが適用されないため、

        HitStopManager.Instance.PlayHitStop(_phase1HitStop, _player.gameObject);
        HitStopManager.Instance.PlayHitStop(_phase1HitStop, _enemyController.gameObject);
        // BGM を停止し、敵の死亡SEを再生する
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlaySE(_audioConfig.EnemyDeadSound);
        }

        // プレイヤーは完全停止ではなくスローにする（例: 0.3 の速度）
        HitStopManager.Instance.PlayHitStopSlow(0.2f, 0.3f, _player.gameObject);

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
        await UniTask.Delay((int)(_whiteFlashDuration * 1000));
        // player のスローは上のコルーチンが終了すると自動で元に戻るため、ここで再設定はしない
        _player.AnimController.PlayTrigger(_player.AnimController.AnimName.SwordSheathing);
        await UniTask.Delay((int)(_phase2Duration * 1000));
        LoadSceneManager.Instance.LoadScene(LoadSceneManager.Instance.SceneNameConfig.TitleScene);
    }
}
