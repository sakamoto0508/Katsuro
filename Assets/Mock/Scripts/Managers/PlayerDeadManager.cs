using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
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
    [SerializeField] private Volume _volume;
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private float _intensity = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _smoothness = 0.5f;
    [SerializeField] private TextMeshProUGUI _deadText;
    [SerializeField] private float _deadTextFadeIn = 2f;
    [SerializeField] private Ease _ease = Ease.InQuint;

    private GameObject _overlay;
    private Vignette _vignette;
    // UI image refs for better control
    private UnityEngine.UI.Image _redImage;
    private UnityEngine.UI.Image _blackImage;
    private Material _blackMaterialInstance;

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

    private void Start()
    {
        if (_volume != null)
        {
            _volume.profile.TryGet(out _vignette);
            if (_vignette != null)
            {
                _vignette.intensity.value = 0f;
                _vignette.smoothness.value = 0f;
            }
        }
        if (_deadText != null)
        {
            _deadText.gameObject.SetActive(false);
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
        // フェーズ1: Vignette のフェードイン（オーバーレイと Post-process を同時にフェード）
        CreateVignetteOverlay();
        var overlayTask = _overlay != null ? FadeOverlayAlpha(_vignetteMaxAlpha, _vignetteFadeIn) : UniTask.CompletedTask;
        var vigTask = (_vignette != null) ? FadeVignette(_intensity, _smoothness, _vignetteFadeIn) : UniTask.CompletedTask;
        if (_deadText != null)
        {
            _deadText.gameObject.SetActive(true);
            // ensure starting alpha is zero so fade-in always plays
            var c = _deadText.color;
            c.a = 0f;
            _deadText.color = c;
            _deadText.DOKill();
            // use unscaled update so tween runs during hitstop/slow
            _deadText.DOFade(1f, _deadTextFadeIn).SetEase(_ease).SetUpdate(true);
        }
        await UniTask.WhenAll(overlayTask, vigTask);

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

        await UniTask.Delay(System.TimeSpan.FromSeconds(_slowDuration), ignoreTimeScale: true);
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
        // 赤のフルスクリーン背景
        var redGO = new GameObject("vignette_red");
        redGO.transform.SetParent(_overlay.transform, false);
        _redImage = redGO.AddComponent<Image>();
        _redImage.color = new Color(_vignetteColor.r, _vignetteColor.g, _vignetteColor.b, 0f);
        var redRect = _redImage.GetComponent<RectTransform>();
        redRect.anchorMin = Vector2.zero;
        redRect.anchorMax = Vector2.one;
        redRect.offsetMin = Vector2.zero;
        redRect.offsetMax = Vector2.zero;

        // その上に黒のビネット（中心を透明にする）を配置する
        var blackGO = new GameObject("vignette_black");
        blackGO.transform.SetParent(_overlay.transform, false);
        _blackImage = blackGO.AddComponent<Image>();
        // 使用するシェーダーは Assets/Mock/Shaders/UIUnlitVignette.shader
        var shader = Shader.Find("UI/UnlitVignette");
        if (shader != null)
        {
            _blackMaterialInstance = new Material(shader);
            // initialize black material parameters
            _blackMaterialInstance.SetColor("_Color", new Color(0f, 0f, 0f, 1f));
            _blackMaterialInstance.SetFloat("_InnerRadius", 0.3f);
            _blackMaterialInstance.SetFloat("_OuterRadius", 0.95f);
            _blackMaterialInstance.SetFloat("_Smoothness", 0.7f);
            _blackImage.material = _blackMaterialInstance;
        }
        var blackRect = _blackImage.GetComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        // ensure black is rendered on top
        _blackImage.transform.SetAsLastSibling();
        _blackImage.raycastTarget = false;
    }

    /// <summary>
    /// オーバーレイのアルファをフェードさせます（実時間、タイムスケールの影響を受けない）。
    /// </summary>
    private async UniTask FadeOverlayAlpha(float targetAlpha, float duration)
    {
        if (_overlay == null) return;
        // Fade both red background image and black vignette material alpha (if available)
        var red = _redImage;
        var blackMat = _blackMaterialInstance;
        if (red == null && blackMat == null) return;
        float t = 0f;
        Color redStart = red != null ? red.color : Color.clear;
        float blackStartAlpha = 1f;
        if (blackMat != null)
        {
            var c = blackMat.GetColor("_Color");
            blackStartAlpha = c.a;
        }
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(redStart.a, targetAlpha, k);
            if (red != null)
            {
                red.color = new Color(redStart.r, redStart.g, redStart.b, a);
            }
            if (blackMat != null)
            {
                var c = blackMat.GetColor("_Color");
                c.a = Mathf.Lerp(blackStartAlpha, targetAlpha, k);
                blackMat.SetColor("_Color", c);
            }
            await UniTask.Yield();
        }
        if (red != null) red.color = new Color(redStart.r, redStart.g, redStart.b, targetAlpha);
        if (blackMat != null)
        {
            var c = blackMat.GetColor("_Color");
            c.a = targetAlpha;
            blackMat.SetColor("_Color", c);
        }
    }

    private async UniTask FadeVignette(float intensity, float smoothness, float duration)
    {
        float start = _vignette.intensity.value;
        float start2 = _vignette.smoothness.value;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            _vignette.intensity.value = Mathf.Lerp(start, intensity, t);
            _vignette.smoothness.value = Mathf.Lerp(start2, smoothness, t);
            await UniTask.Yield();
        }

        _vignette.intensity.value = intensity;
        _vignette.smoothness.value = smoothness;
    }
}
