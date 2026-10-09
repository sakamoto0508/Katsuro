using TMPro;
using UnityEngine;

/// <summary>決着のUIだけをunscaled timeで描画する。HUDのHP更新・戦闘状態には触れない。</summary>
public sealed class FinalBlowPresentation : MonoBehaviour
{
    /// <summary>最終Hitで白Flashを表示するCanvasGroup。Alphaで強さを制御する。</summary>
    [UnityEngine.Tooltip("最終Hitで白Flashを表示するCanvasGroup。Alphaで強さを制御する。")]
    [SerializeField] private CanvasGroup _whiteFlash;
    /// <summary>勝利中に画面上へ出す黒帯のRectTransform。</summary>
    [UnityEngine.Tooltip("勝利中に画面上へ出す黒帯のRectTransform。")]
    [SerializeField] private RectTransform _letterboxTop;
    /// <summary>勝利中に画面下へ出す黒帯のRectTransform。</summary>
    [UnityEngine.Tooltip("勝利中に画面下へ出す黒帯のRectTransform。")]
    [SerializeField] private RectTransform _letterboxBottom;
    /// <summary>勝利中にFade OutするHUDのCanvasGroup一覧。</summary>
    [UnityEngine.Tooltip("勝利中にFade OutするHUDのCanvasGroup一覧。")]
    [SerializeField] private CanvasGroup[] _hudGroups;
    /// <summary>通常HUD更新と勝利演出の表示制御を調停するRunHUD一覧。</summary>
    [UnityEngine.Tooltip("通常HUD更新と勝利演出の表示制御を調停するRunHUD一覧。")]
    [SerializeField] private RunHUD[] _hudVisibilityOwners;
    /// <summary>勝利中に新しいダメージ数字を抑制する表示コンポーネント。</summary>
    [UnityEngine.Tooltip("勝利中に新しいダメージ数字を抑制する表示コンポーネント。")]
    [SerializeField] private DamageNumbers _damageNumbers;
    /// <summary>最終Hit白Flashの最大Alpha。0で透明、1で不透明。</summary>
    [UnityEngine.Tooltip("最終Hit白Flashの最大Alpha。0で透明、1で不透明。")]
    [SerializeField, Range(0f, 1f)] private float _flashStrength = .8f;
    /// <summary>勝利開始から白Flashを出すまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("勝利開始から白Flashを出すまでの時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _flashDelay = .02f;
    /// <summary>勝利開始からHUDを消し始めるまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("勝利開始からHUDを消し始めるまでの時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _hudFadeDelay = .12f;
    /// <summary>HUDのFade Outにかける時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("HUDのFade Outにかける時間（実時間の秒）。")]
    [SerializeField, Range(.15f, .3f)] private float _hudFadeDuration = .23f;
    /// <summary>勝利開始から上下黒帯を出し始めるまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("勝利開始から上下黒帯を出し始めるまでの時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _letterboxDelay = .15f;
    /// <summary>上下黒帯が目標高さになるまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("上下黒帯が目標高さになるまでの時間（実時間の秒）。")]
    [SerializeField, Range(.18f, .3f)] private float _letterboxDuration = .25f;
    /// <summary>各黒帯の目標高さ。親画面の高さに対する比率。</summary>
    [UnityEngine.Tooltip("各黒帯の目標高さ。親画面の高さに対する比率。")]
    [SerializeField, Range(.07f, .1f)] private float _letterboxHeight = .08f;
    /// <summary>討伐文字が現れる瞬間のScale倍率。元のScaleを基準にする。</summary>
    [UnityEngine.Tooltip("討伐文字が現れる瞬間のScale倍率。元のScaleを基準にする。")]
    [SerializeField, Range(.8f, 1f)] private float _textStartScale = .9f;
    /// <summary>討伐文字が素早く拡大したときの最大Scale倍率。その後元のScaleへ戻す。</summary>
    [UnityEngine.Tooltip("討伐文字が素早く拡大したときの最大Scale倍率。その後元のScaleへ戻す。")]
    [SerializeField, Range(1f, 1.08f)] private float _textOvershootScale = 1.025f;
    /// <summary>討伐表示時間のうち最初の拡大・Fadeに使う割合。残りの時間でScaleを落ち着かせる。</summary>
    [UnityEngine.Tooltip("討伐表示時間のうち最初の拡大・Fadeに使う割合。残りの時間でScaleを落ち着かせる。")]
    [SerializeField, Range(.3f, .8f)] private float _textRiseFraction = .55f;
    private TMP_Text _text;
    private Color _savedTextColor;
    private Vector3 _savedTextScale;
    private bool _savedTextActive;
    private float[] _savedAlpha;
    private bool[] _savedInteractable, _savedRaycasts;
    private float _started, _flashDuration, _textDelay, _textDuration;
    private bool _playing;

    /// <summary>元の文字・HUD状態を保存し、Flash・黒帯・討伐表示の実時間タイムラインを開始する。</summary>
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

    /// <summary>勝利UIが進行中の間だけ、開始からの実経過時間で表示を更新する。</summary>
    private void Update() { if (_playing) RenderAt(Time.unscaledTime - _started); }

    // Edit Modeでタイムラインを検証できるよう、時計と描画を分ける。
    /// <summary>指定経過秒でFlash、HUD非表示、黒帯と討伐文字の素早い出現・Scaleの落ち着きを反映する。</summary>
    /// <param name="elapsed">勝利UI開始からの実経過秒数。</param>
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

    /// <summary>画面高の比率を使って上下黒帯のAnchorを移動し、解像度に依存せず出現させる。</summary>
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

    /// <summary>Flashと黒帯を解除し、保存したHUD・文字の色・Scale・有効状態を復元する。</summary>
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
    /// <summary>勝利UIを解除して停止前のHUDと文字状態を復元する。</summary>
    private void OnDisable() => ResetPresentation();
    /// <summary>勝利UIが保持した一時表示を解除して元の状態へ戻す。</summary>
    private void OnDestroy() => ResetPresentation();
}
