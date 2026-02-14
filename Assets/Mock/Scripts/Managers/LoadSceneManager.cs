using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance { get; private set; }

    public SceneNameConfig SceneNameConfig => _sceneNameConfig;

    [SerializeField] private SceneNameConfig _sceneNameConfig;

    /// <summary>
    /// 同期的にシーンを読み込みます。sceneName が空ならデフォルトシーンを使用します。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public async UniTaskVoid LoadSceneAsync(string sceneName, int waitTime)
    {
        await UniTask.Delay(waitTime);
        SceneManager.LoadScene(sceneName);
    }

    private void Awake()
    {
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
