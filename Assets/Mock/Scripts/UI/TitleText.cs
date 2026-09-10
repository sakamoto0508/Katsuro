using DG.Tweening;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>Title text pulse, with the same scene transition used by keyboard/gamepad input.</summary>
public class TitleText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private string _gameSceneName = "GameScene";
    private Sequence _pulse;
    private bool _isTransitioning;

    private void OnEnable()
    {
        if (_text == null) return;
        _text.alpha = 0f;
        _pulse = DOTween.Sequence().SetUpdate(true);
        _pulse.Append(_text.DOFade(1f, _fadeInDuration).SetEase(_ease));
        _pulse.AppendInterval(0.5f);
        _pulse.Append(_text.DOFade(0f, _fadeOutDuration).SetEase(_ease));
        _pulse.SetLoops(-1);
    }

    private void OnDisable()
    {
        _pulse?.Kill();
        _pulse = null;
    }

    public void OnStartButton()
    {
        if (_isTransitioning) return;
        var titleManager = FindFirstObjectByType<TitleManager>();
        if (titleManager != null)
        {
            titleManager.OnPressStart();
            return;
        }
        _isTransitioning = true;
        StartGame().Forget();
    }

    private async UniTask StartGame()
    {
        try { await GlobalFader.EnsureInstance().FadeToScene(_gameSceneName); }
        finally { _isTransitioning = false; }
    }
}
