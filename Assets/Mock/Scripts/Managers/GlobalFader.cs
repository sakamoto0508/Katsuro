using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class GlobalFader : MonoBehaviour
{
    public static GlobalFader Instance;

    /// <summary>
    /// Ensure a GlobalFader exists in the scene (creates one if missing) and return the instance.
    /// </summary>
    public static GlobalFader EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GlobalFader");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GlobalFader>();
        return Instance;
    }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float duration = 1f;

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
        // Ensure overlay exists on this persistent object so fadeImage won't be null after scene load
        EnsureOverlay();
        // Keep overlay state clean when scenes load
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a new scene is loaded, ensure overlay is hidden and alpha reset so title isn't blocked
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
            var canvas = fadeImage.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.overrideSorting = false;
        }
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c; // 初期透明
        }

    }

    private void EnsureOverlay()
    {
        if (fadeImage != null) return;

        // create Canvas under this object
        var canvasGO = new GameObject("GlobalFader_Canvas");
        canvasGO.transform.SetParent(this.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        var img = imageGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        var rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = img;
    }

    public async UniTask FadeToScene(string sceneName)
    {
        // ensure overlay/images are present and on top
        EnsureOverlay();
        if (fadeImage == null)
        {
            var opF = SceneManager.LoadSceneAsync(sceneName);
            var tcsF = new UniTaskCompletionSource<bool>();
            opF.completed += _ => tcsF.TrySetResult(true);
            await tcsF.Task;
            return;
        }

        // bring canvas to top and prepare image
        var canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }
        fadeImage.gameObject.SetActive(true);
        // ensure starting alpha is 0 so fade-out anim is visible
        var colStart = fadeImage.color;
        colStart.a = 0f;
        fadeImage.color = colStart;

        // Fade out (unscaled) using DOTween and await completion via UniTaskCompletionSource
        // Kill any existing tweens on this target and ensure image is active and visible start at 0
        DOTween.Kill(fadeImage);
        fadeImage.gameObject.SetActive(true);
        var startCol = fadeImage.color;
        startCol.a = 0f;
        fadeImage.color = startCol;

        var tcsOut = new UniTaskCompletionSource<bool>();
        var tweenOut = fadeImage.DOFade(1f, duration).SetEase(Ease.InOutSine).SetUpdate(true);
        tweenOut.OnComplete(() => tcsOut.TrySetResult(true));
        await tcsOut.Task;

        // Load scene and await completion
        var op = SceneManager.LoadSceneAsync(sceneName);
        var tcs = new UniTaskCompletionSource<bool>();
        op.completed += _ => tcs.TrySetResult(true);
        await tcs.Task;

        // Fade in
        DOTween.Kill(fadeImage);
        var tcsIn = new UniTaskCompletionSource<bool>();
        var tweenIn = fadeImage.DOFade(0f, duration).SetEase(Ease.InOutSine).SetUpdate(true);
        tweenIn.OnComplete(() => tcsIn.TrySetResult(true));
        await tcsIn.Task;

        // cleanup: hide overlay and restore canvas sorting
        fadeImage.gameObject.SetActive(false);
        var canvasAfter = fadeImage.GetComponentInParent<Canvas>();
        if (canvasAfter != null) canvasAfter.overrideSorting = false;
    }
}
