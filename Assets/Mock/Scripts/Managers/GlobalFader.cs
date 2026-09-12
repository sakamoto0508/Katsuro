using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>シーンをまたいで残る画面被覆と、フェード・読み込み・フェードの遷移全体を管理する。</summary>
public class GlobalFader : MonoBehaviour
{
    public static GlobalFader Instance { get; private set; }
    public bool IsTransitioning { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float duration = 1f;

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (Instance != null && Instance != this)
        {
            if (fadeImage != null && fadeImage != Instance.fadeImage)
                fadeImage.gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        SetAlpha(0f);
        fadeImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// フェード用のオーバーレイを確保する。シーン遷移中に破棄されないよう、永続オブジェクトの子に配置する。
    /// </summary>
    private void EnsureOverlay()
    {
        // シーンに属する画像は遷移途中で破棄されるため、永続オブジェクトの子に配置する。
        if (fadeImage == null || !fadeImage.transform.IsChildOf(transform))
        {
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            var canvasObject = new GameObject("GlobalFader_Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            var imageObject = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            fadeImage = imageObject.GetComponent<Image>();
            fadeImage.color = Color.clear;
        }
        var overlayCanvas = fadeImage.GetComponentInParent<Canvas>();
        if (overlayCanvas != null)
        {
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = short.MaxValue;
        }
        var rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fadeImage.raycastTarget = true;
    }

    public async UniTask FadeToScene(string sceneName)
    {
        if (IsTransitioning) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            throw new ArgumentException($"Scene is not available in Build Settings: {sceneName}", nameof(sceneName));
        IsTransitioning = true;
        var token = this.GetCancellationTokenOnDestroy();
        try
        {

            fadeImage.gameObject.SetActive(true);
            await FadeAlpha(1f, token);
            // シーン読み込み完了時と開始時の初期化が済むまで、画面を不透明に保つ。
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null) throw new InvalidOperationException($"Could not load scene: {sceneName}");
            await operation.ToUniTask(cancellationToken: token);
            await UniTask.NextFrame(cancellationToken: token);
            await FadeAlpha(0f, token);
        }
        finally
        {
            if (fadeImage != null)
            {
                SetAlpha(0f);
                fadeImage.gameObject.SetActive(false);
            }
            IsTransitioning = false;
        }
    }

    private async UniTask FadeAlpha(float target, CancellationToken token)
    {
        float start = fadeImage.color.a;
        float seconds = Mathf.Max(0f, duration);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / seconds);
            float eased = (1f - Mathf.Cos(progress * Mathf.PI)) * 0.5f;
            SetAlpha(Mathf.Lerp(start, target, eased));
        }
        SetAlpha(target);
    }

    private void SetAlpha(float alpha)
    {
        fadeImage.color = new Color(0f, 0f, 0f, alpha);
    }
}
