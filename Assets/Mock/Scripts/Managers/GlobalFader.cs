using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>Owns the persistent overlay and the complete fade/load/fade transition.</summary>
public class GlobalFader : MonoBehaviour
{
    public static GlobalFader Instance { get; private set; }
    public bool IsTransitioning { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float duration = 1f;

    public static GlobalFader EnsureInstance()
    {
        if (Instance == null)
            new GameObject("GlobalFader").AddComponent<GlobalFader>().Init();
        return Instance;
    }

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

    private void EnsureOverlay()
    {
        // A scene-owned image would be destroyed halfway through the transition.
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
            EnsureOverlay();
            fadeImage.gameObject.SetActive(true);
            await FadeAlpha(1f, token);
            // Stay opaque while sceneLoaded and Start initialize the new scene.
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
