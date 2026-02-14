using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance { get; private set; }

    [SerializeField] private SceneNameConfig _sceneNameConfig;

    /// <summary>
    /// 同期的にシーンを読み込みます。sceneName が空ならデフォルトシーンを使用します。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 非同期でシーンを読み込みます（簡易版）。内部で Coroutine を使ってロードを開始します。
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadAsyncRoutine(sceneName));
    }

    private IEnumerator LoadAsyncRoutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}'");
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }
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
