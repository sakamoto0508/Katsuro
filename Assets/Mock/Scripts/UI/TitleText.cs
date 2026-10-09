using DG.Tweening;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>タイトル文字を明滅させる。シーン遷移はキーボード・パッド入力と共通の処理を使う。</summary>
public class TitleText : MonoBehaviour
{
    /// <summary>タイトル画面で点滅FadeするTMP文字参照。</summary>
    [UnityEngine.Tooltip("タイトル画面で点滅FadeするTMP文字参照。")]
    [SerializeField] private TextMeshProUGUI _text;
    /// <summary>タイトル文字が現れるまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("タイトル文字が現れるまでの時間（実時間の秒）。")]
    [SerializeField] private float _fadeInDuration = 1.0f;
    /// <summary>タイトル文字が消えるまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("タイトル文字が消えるまでの時間（実時間の秒）。")]
    [SerializeField] private float _fadeOutDuration = 1.0f;
    /// <summary>タイトル文字のFadeに使用するDOTween補間曲線。</summary>
    [UnityEngine.Tooltip("タイトル文字のFadeに使用するDOTween補間曲線。")]
    [SerializeField] private Ease _ease = Ease.InOutSine;
    /// <summary>TitleManager未設定時の代替開始処理でロードするScene名。通常はTitleManagerへ委譲する。</summary>
    [UnityEngine.Tooltip("TitleManager未設定時の代替開始処理でロードするScene名。通常はTitleManagerへ委譲する。")]
    [SerializeField] private string _gameSceneName = "GameScene";
    private Sequence _pulse;
    private bool _isTransitioning;
    private TitleManager _title;
    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>TitleとFaderを接続し、有効な文字表示なら実時間の点滅を開始する。</summary>
    public void Init(TitleManager title, GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _title = title;
        _fader = fader;
        if (isActiveAndEnabled) StartPulse();
    }

    /// <summary>初期化済みのTitle文字に点滅Tweenを再開する。</summary>
    private void OnEnable() { if (_initialized) StartPulse(); }
    /// <summary>文字のFadeと待機を繰り返すTweenを実時間更新で組み立てる。</summary>
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

    /// <summary>所有する点滅Tweenを停止して参照を解除する。</summary>
    private void OnDisable()
    {
        _pulse?.Kill();
        _pulse = null;
    }

    /// <summary>多重遷移を防ぎ、TitleManagerの設定画面または既存のゲーム開始処理を要求する。</summary>
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

    /// <summary>共有FaderでGameSceneへ遷移し、成功・中断にかかわらず遷移中フラグを解除する。</summary>
    private async UniTask StartGame()
    {
        try { await _fader.FadeToScene(_gameSceneName); }
        finally { _isTransitioning = false; }
    }
}
