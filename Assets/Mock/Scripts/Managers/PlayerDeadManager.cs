using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

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
    // 画面演出を制御するための画像参照。
    private UnityEngine.UI.Image _redImage;
    private UnityEngine.UI.Image _blackImage;
    private Material _blackMaterialInstance;
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
            InitPresentation();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitPresentation()
    {
        CreateVignetteOverlay();
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
        if (_isPlaying) return;
        if (playerObject == null)
        {
            Debug.LogWarning("StartDefeatSequence called with null playerObject");
            return;
        }
        _isPlaying = true;
        _game?.LoseGame();
        StartDefeatSequenceAsync(playerObject).Forget();
    }

    /// <summary>
    /// 敗北演出の非同期実行本体。
    /// フェーズごとに演出を順次実行します（ヴィネット、ローパス、スロー、無音、タイトル遷移）。
    /// </summary>
    private async UniTaskVoid StartDefeatSequenceAsync(GameObject playerObject)
    {
        var token = this.GetCancellationTokenOnDestroy();
        // フェーズ1: Vignette のフェードイン（オーバーレイと Post-process を同時にフェード）

        var overlayTask = _overlay != null ? FadeOverlayAlpha(_vignetteMaxAlpha, _vignetteFadeIn) : UniTask.CompletedTask;
        var vigTask = (_vignette != null) ? FadeVignette(_intensity, _smoothness, _vignetteFadeIn) : UniTask.CompletedTask;
        if (_deadText != null)
        {
            _deadText.gameObject.SetActive(true);
            // 必ずフェードインするよう、開始時の不透明度をゼロにする。
            var c = _deadText.color;
            c.a = 0f;
            _deadText.color = c;
            _deadText.DOKill();
            // 停止・スロー中も補間が進むよう、時間倍率の影響を受けない更新を使う。
            _deadText.DOFade(1f, _deadTextFadeIn).SetEase(_ease).SetUpdate(true);
        }
        await UniTask.WhenAll(overlayTask, vigTask);

        // Audio: ローパスを適用して音がこもる
        if (_audio != null)
        {
            _audio.ApplyLowPassToListener(_lowPassCutoff);
        }

        // スロー: シーン内の Animator を全体的に遅くする（プレイヤー優先）
        if (_hitStop != null)
        {
            _hitStop.PlayHitStopSlow(_slowDuration, _slowSpeed, playerObject,
                _enemyController != null ? _enemyController.gameObject : null);
        }

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

        await UniTask.Delay(System.TimeSpan.FromSeconds(Mathf.Max(0f, _slowDuration)), ignoreTimeScale: true, cancellationToken: token);
        _audio?.StopAllAudioImmediate();
        await UniTask.Delay(System.TimeSpan.FromSeconds(Mathf.Max(0f, _silenceDuration)), ignoreTimeScale: true, cancellationToken: token);

        // フェーズ3: タイトルへ遷移
        var config = _loader != null ? _loader.SceneNameConfig : null;
        await _fader.FadeToScene(config != null ? config.TitleScene : "TitleScene");
    }

    /// <summary>
    /// 画面全体に覆い被さるヴィネット用オーバーレイを生成します。
    /// </summary>
    private void CreateVignetteOverlay()
    {
        if (_overlay != null) return;
        _overlay = new GameObject("VignetteOverlay");
        _overlay.transform.SetParent(transform, false);
        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        // 赤のフルスクリーン背景
        var redGO = new GameObject("vignette_red");
        redGO.transform.SetParent(_overlay.transform, false);
        _redImage = redGO.AddComponent<Image>();
        _redImage.color = new Color(_vignetteColor.r, _vignetteColor.g, _vignetteColor.b, 0f);
        _redImage.raycastTarget = false;
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
            // 黒い画面効果のマテリアル設定を初期化する。
            _blackMaterialInstance.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
            _blackMaterialInstance.SetFloat("_InnerRadius", 0.3f);
            _blackMaterialInstance.SetFloat("_OuterRadius", 0.95f);
            _blackMaterialInstance.SetFloat("_Smoothness", 0.7f);
            _blackImage.material = _blackMaterialInstance;
        }
        else
        {
            // 専用シェーダーがビルドに含まれない場合は、通常の黒い画像を使用し、透明な状態から開始する。
            _blackImage.color = new Color(0f, 0f, 0f, 0f);
        }
        var blackRect = _blackImage.GetComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        // 黒い画像を最前面に描画する。
        _blackImage.transform.SetAsLastSibling();
        _blackImage.raycastTarget = false;
    }

    /// <summary>
    /// オーバーレイのアルファをフェードさせます（実時間、タイムスケールの影響を受けない）。
    /// </summary>
    private async UniTask FadeOverlayAlpha(float targetAlpha, float duration)
    {
        if (_overlay == null) return;
        // 赤い背景画像と、存在する場合は黒い周辺減光マテリアルの不透明度を変化させる。
        var red = _redImage;
        var blackMat = _blackMaterialInstance;
        var blackImg = _blackImage;
        if (red == null && blackMat == null && blackImg == null) return;
        float t = 0f;
        Color redStart = red != null ? red.color : Color.clear;
        float blackStartAlpha = 1f;
        if (blackMat != null)
        {
            var c = blackMat.GetColor("_Color");
            blackStartAlpha = c.a;
        }
        else if (blackImg != null)
        {
            blackStartAlpha = blackImg.color.a;
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
            else if (blackImg != null)
            {
                var bc = blackImg.color;
                bc.a = Mathf.Lerp(blackStartAlpha, targetAlpha, k);
                blackImg.color = bc;
            }
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }
        if (red != null) red.color = new Color(redStart.r, redStart.g, redStart.b, targetAlpha);
        if (blackMat != null)
        {
            var c = blackMat.GetColor("_Color");
            c.a = targetAlpha;
            blackMat.SetColor("_Color", c);
        }
        else if (blackImg != null)
        {
            var bc = blackImg.color;
            bc.a = targetAlpha;
            blackImg.color = bc;
        }
    }

    private async UniTask FadeVignette(float intensity, float smoothness, float duration)
    {
        float start = _vignette.intensity.value;
        float start2 = _vignette.smoothness.value;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            _vignette.intensity.value = Mathf.Lerp(start, intensity, t);
            _vignette.smoothness.value = Mathf.Lerp(start2, smoothness, t);
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        _vignette.intensity.value = intensity;
        _vignette.smoothness.value = smoothness;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (_deadText != null) _deadText.DOKill();
        if (_blackMaterialInstance != null) Destroy(_blackMaterialInstance);
        _audio?.RemoveLowPassFromListener();
    }
}
