using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>共有FaderへScene遷移を委譲し、必要なら指定時間の待機後に遷移する。</summary>
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

    /// <summary>指定した実時間の待機後に共有Faderへ遷移を要求する。所有者の破棄で待機を中断する。</summary>
    public async UniTaskVoid LoadSceneAsync(string sceneName, int waitTime)
    {
        await UniTask.Delay(Mathf.Max(0, waitTime), ignoreTimeScale: true,
            cancellationToken: this.GetCancellationTokenOnDestroy());
        await _fader.FadeToScene(sceneName);
    }

    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>共有インスタンスとFaderを一度だけ接続する。</summary>
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

    /// <summary>自身が共有インスタンスの場合に参照を解除する。</summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
