using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

/// <summary>
/// プレイヤー敗北時の演出を管理します。
/// フェーズ1: 赤いヴィネット、ローパス（音がこもる）、スロー
/// フェーズ2: 崩れ（膝をつくアニメは外部で実行）、無音 0.5 秒
/// フェーズ3: タイトルへ遷移
/// </summary>
public class PlayerDeadManager : MonoBehaviour
{
    public static PlayerDeadManager Instance { get; private set; }

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private EnemyController _enemyController;
    // ヴィネットのフェードイン時間（秒）
    [SerializeField] private float _vignetteFadeIn = 0.25f;
    // ヴィネットの色（赤みを帯びた色を想定）
    [SerializeField] private Color _vignetteColor = new Color(0.4f, 0f, 0f, 0.0f);
    // ヴィネットの最大アルファ
    [SerializeField] private float _vignetteMaxAlpha = 0.6f;
    // 全体スローの継続時間（秒）
    [SerializeField] private float _slowDuration = 1.2f;
    // 全体スロー時に適用する再生速度（0..1、1が通常速度）
    [SerializeField] private float _slowSpeed = 0.4f;
    // 崩れ後の無音継続時間（秒）
    [SerializeField] private float _silenceDuration = 0.5f;
    // ローパスのカットオフ周波数（Hz）: 値を下げるほど音がこもる
    [SerializeField] private float _lowPassCutoff = 800f;

    private GameObject _overlay;

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
    }

    /// <summary>
    /// 敗北演出を開始します。
    /// <param name="playerObject">プレイヤーの GameObject。Animator を含むオブジェクトを指定してください。</param>
    /// </summary>
    public void StartDefeatSequence(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogWarning("StartDefeatSequence called with null playerObject");
            return;
        }
        StartDefeatSequenceAsync(playerObject).Forget();
    }

    /// <summary>
    /// 敗北演出の非同期実行本体。
    /// フェーズごとに演出を順次実行します（ヴィネット、ローパス、スロー、無音、タイトル遷移）。
    /// </summary>
    private async UniTaskVoid StartDefeatSequenceAsync(GameObject playerObject)
    {
        // フェーズ1: Vignette のフェードイン
        CreateVignetteOverlay();
        await FadeOverlayAlpha(_vignetteMaxAlpha, _vignetteFadeIn);

        // Audio: ローパスを適用して音がこもる
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyLowPassToListener(_lowPassCutoff);
        }

        // スロー: シーン内の Animator を全体的に遅くする（プレイヤー優先）
        HitStopManager.Instance.PlayHitStopSlow(0.2f, 0.3f, _playerController.AnimController.gameObject);
        HitStopManager.Instance.PlayHitStopSlow(0.2f, 0.3f, _enemyController.gameObject);

        // プレイヤーが既に破棄されていないか確認してからトリガーを送る
        if (_playerController != null && _playerController.AnimController != null)
        {
            var playerGO = _playerController.AnimController.gameObject;
            if (playerGO != null && !playerGO.Equals(null))
            {
                try
                {
                    _playerController.AnimController.PlayTrigger(_playerController.AnimController.AnimName.PlayerDead);
                }
                catch (UnityEngine.MissingReferenceException)
                {
                    // プレイヤーオブジェクトがシーン遷移等で破棄されていた場合は安全に無視する
                }
            }
        }

        // フェーズ2: 崩れ（膝をつく）。無音を作る。
        await UniTask.Delay(System.TimeSpan.FromSeconds(_slowDuration), ignoreTimeScale: true);
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllAudioImmediate();
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(_silenceDuration), ignoreTimeScale: true);

        // フェーズ3: タイトルへ遷移
        LoadSceneManager.Instance.LoadScene(LoadSceneManager.Instance.SceneNameConfig.TitleScene);
    }

    /// <summary>
    /// 画面全体に覆い被さるヴィネット用オーバーレイを生成します。
    /// </summary>
    private void CreateVignetteOverlay()
    {
        if (_overlay != null) return;
        _overlay = new GameObject("VignetteOverlay");
        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var imgGO = new GameObject("vignette");
        imgGO.transform.SetParent(_overlay.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(_vignetteColor.r, _vignetteColor.g, _vignetteColor.b, 0f);
        var rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// オーバーレイのアルファをフェードさせます（実時間、タイムスケールの影響を受けない）。
    /// </summary>
    private async UniTask FadeOverlayAlpha(float targetAlpha, float duration)
    {
        if (_overlay == null) return;
        var img = _overlay.GetComponentInChildren<Image>();
        if (img == null) return;
        float t = 0f;
        Color start = img.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            img.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, targetAlpha, k));
            await UniTask.Yield();
        }
        img.color = new Color(start.r, start.g, start.b, targetAlpha);
    }
}
