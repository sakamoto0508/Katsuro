using DG.Tweening;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
// Simple title controller that fades out via GlobalFader then loads game scene
public class TitleText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private string _gameSceneName = "GameScene";

    private void Start()
    {
        _text.alpha = 0f;
        Sequence seq = DOTween.Sequence();

        seq.Append(_text.DOFade(1f, _fadeInDuration).SetEase(_ease));
        seq.AppendInterval(0.5f);
        seq.Append(_text.DOFade(0f, _fadeOutDuration).SetEase(_ease));

        seq.SetLoops(-1);
    }

    public void OnStartButton()
    {
        StartGame().Forget();
    }

    private async UniTaskVoid StartGame()
    {
        if (GlobalFader.Instance == null)
        {
            Debug.LogWarning("GlobalFader not found. Loading scene directly.");
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_gameSceneName);
            var tcs = new UniTaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;
            return;
        }

        await GlobalFader.Instance.FadeToScene(_gameSceneName);
    }
}
