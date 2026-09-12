using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance { get; private set; }

    public SceneNameConfig SceneNameConfig => _sceneNameConfig;

    [SerializeField] private SceneNameConfig _sceneNameConfig;

    /// <summary>
    /// フェードを伴うシーン遷移を開始します。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        _fader.FadeToScene(sceneName).Forget();
    }

    public async UniTaskVoid LoadSceneAsync(string sceneName, int waitTime)
    {
        await UniTask.Delay(Mathf.Max(0, waitTime), ignoreTimeScale: true,
            cancellationToken: this.GetCancellationTokenOnDestroy());
        await _fader.FadeToScene(sceneName);
    }

    private GlobalFader _fader;
    private bool _initialized;
    public void Init(GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _fader = fader;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
