using DG.Tweening;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>タイトル文字を明滅させる。シーン遷移はキーボード・パッド入力と共通の処理を使う。</summary>
public class TitleText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private string _gameSceneName = "GameScene";
    private Sequence _pulse;
    private bool _isTransitioning;
    private TitleManager _title;
    private GlobalFader _fader;
    private bool _initialized;
    public void Init(TitleManager title, GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _title = title;
        _fader = fader;
        if (isActiveAndEnabled) StartPulse();
    }

    private void OnEnable() { if (_initialized) StartPulse(); }
    private void StartPulse()
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
        var titleManager = _title;
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
        try { await _fader.FadeToScene(_gameSceneName); }
        finally { _isTransitioning = false; }
    }
}
