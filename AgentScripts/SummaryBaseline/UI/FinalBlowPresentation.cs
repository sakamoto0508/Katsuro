using TMPro;
using UnityEngine;

/// <summary>決着のUIだけをunscaled timeで描画する。HUDのHP更新・戦闘状態には触れない。</summary>
public sealed class FinalBlowPresentation : MonoBehaviour
{
    [SerializeField] private CanvasGroup _whiteFlash;
    [SerializeField] private RectTransform _letterboxTop, _letterboxBottom;
    [SerializeField] private CanvasGroup[] _hudGroups;
    [SerializeField] private RunHUD[] _hudVisibilityOwners;
    [SerializeField] private DamageNumbers _damageNumbers;
    [SerializeField, Range(0f, 1f)] private float _flashStrength = .8f;
    [SerializeField, Min(0f)] private float _flashDelay = .02f;
    [SerializeField, Min(0f)] private float _hudFadeDelay = .12f;
    [SerializeField, Range(.15f, .3f)] private float _hudFadeDuration = .23f;
    [SerializeField, Min(0f)] private float _letterboxDelay = .15f;
    [SerializeField, Range(.18f, .3f)] private float _letterboxDuration = .25f;
    [SerializeField, Range(.07f, .1f)] private float _letterboxHeight = .08f;
    [SerializeField, Range(.8f, 1f)] private float _textStartScale = .9f;
    [SerializeField, Range(1f, 1.08f)] private float _textOvershootScale = 1.025f;
    [SerializeField, Range(.3f, .8f)] private float _textRiseFraction = .55f;
    private TMP_Text _text;
    private Color _savedTextColor;
    private Vector3 _savedTextScale;
    private bool _savedTextActive;
    private float[] _savedAlpha;
    private bool[] _savedInteractable, _savedRaycasts;
    private float _started, _flashDuration, _textDelay, _textDuration;
    private bool _playing;

    public void Begin(TMP_Text text, float flashDuration, float textDelay, float textDuration)
    {
        if (_playing) ResetPresentation();
        _text = text;
        if (_text != null)
        {
            _savedTextColor = _text.color; _savedTextScale = _text.transform.localScale;
            _savedTextActive = _text.gameObject.activeSelf;
            _text.alpha = 0; _text.gameObject.SetActive(false);
        }
        int count = _hudGroups != null ? _hudGroups.Length : 0;
        _savedAlpha = new float[count]; _savedInteractable = new bool[count]; _savedRaycasts = new bool[count];
        for (int i = 0; i < count; i++)
        {
            var group = _hudGroups[i]; if (group == null) continue;
            _savedAlpha[i] = group.alpha; _savedInteractable[i] = group.interactable; _savedRaycasts[i] = group.blocksRaycasts;
            group.interactable = group.blocksRaycasts = false;
        }
        if (_hudVisibilityOwners != null)
            foreach (var owner in _hudVisibilityOwners) if (owner != null) owner.SetPresentationVisibility(true);
        if (_damageNumbers != null) _damageNumbers.SetPresentationSuppressed(true);
        _flashDuration = Mathf.Max(.01f, flashDuration);
        _textDelay = Mathf.Max(0, textDelay); _textDuration = Mathf.Max(.01f, textDuration);
        _started = Time.unscaledTime; _playing = true;
        RenderAt(0);
    }

    private void Update() { if (_playing) RenderAt(Time.unscaledTime - _started); }

    // Edit Modeでタイムラインを検証できるよう、時計と描画を分ける。
    public void RenderAt(float elapsed)
    {
        if (!_playing) return;
        float flashAge = (elapsed - _flashDelay) / _flashDuration;
        if (_whiteFlash != null) _whiteFlash.alpha = flashAge >= 0 && flashAge < 1 ? _flashStrength * Mathf.Pow(1 - flashAge, 2) : 0;
        float hud = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - _hudFadeDelay) / _hudFadeDuration));
        for (int i = 0; i < _savedAlpha.Length; i++) if (_hudGroups[i] != null) _hudGroups[i].alpha = _savedAlpha[i] * (1 - hud);
        float bars = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - _letterboxDelay) / _letterboxDuration));
        SetLetterbox(bars);
        if (_text != null)
        {
            bool visible = elapsed >= _textDelay;
            _text.gameObject.SetActive(visible);
            float age = Mathf.Clamp01((elapsed - _textDelay) / _textDuration);
            float rise = Mathf.Clamp01(age / _textRiseFraction);
            float fade = 1 - Mathf.Pow(1 - rise, 3);
            _text.alpha = fade * _savedTextColor.a;
            float scale = age < _textRiseFraction ? Mathf.Lerp(_textStartScale, _textOvershootScale, fade)
                : Mathf.Lerp(_textOvershootScale, 1, Mathf.SmoothStep(0, 1, (age - _textRiseFraction) / (1 - _textRiseFraction)));
            _text.transform.localScale = _savedTextScale * scale;
        }
    }

    private void SetLetterbox(float progress)
    {
        // Anchorで画面高の割合を指定。画面外の同じ高さからSlideさせる。
        if (_letterboxTop != null)
        {
            _letterboxTop.anchorMin = new Vector2(0, 1 - _letterboxHeight * progress);
            _letterboxTop.anchorMax = new Vector2(1, 1 + _letterboxHeight * (1 - progress));
            _letterboxTop.offsetMin = _letterboxTop.offsetMax = Vector2.zero;
        }
        if (_letterboxBottom != null)
        {
            _letterboxBottom.anchorMin = new Vector2(0, -_letterboxHeight * (1 - progress));
            _letterboxBottom.anchorMax = new Vector2(1, _letterboxHeight * progress);
            _letterboxBottom.offsetMin = _letterboxBottom.offsetMax = Vector2.zero;
        }
    }

    public void ResetPresentation()
    {
        if (_whiteFlash != null) _whiteFlash.alpha = 0;
        SetLetterbox(0);
        if (!_playing) return;
        _playing = false;
        for (int i = 0; i < _savedAlpha.Length; i++)
        {
            var group = _hudGroups[i]; if (group == null) continue;
            group.alpha = _savedAlpha[i]; group.interactable = _savedInteractable[i]; group.blocksRaycasts = _savedRaycasts[i];
        }
        if (_hudVisibilityOwners != null)
            foreach (var owner in _hudVisibilityOwners) if (owner != null) owner.SetPresentationVisibility(false);
        if (_damageNumbers != null) _damageNumbers.SetPresentationSuppressed(false);
        if (_text != null)
        {
            _text.color = _savedTextColor; _text.transform.localScale = _savedTextScale;
            _text.gameObject.SetActive(_savedTextActive);
        }
    }
    private void OnDisable() => ResetPresentation();
    private void OnDestroy() => ResetPresentation();
}
