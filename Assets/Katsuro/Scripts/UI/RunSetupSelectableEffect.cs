using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 準備画面のカード・ボタン・入力欄に、操作フォーカス・マウスHover・装備中の見た目を分けて表示する。
/// フォーカスの移動では装備を変更せず、Toggleの決定（クリック・Submit）だけが装備選択になる。
/// </summary>
[RequireComponent(typeof(Selectable))]
public sealed class RunSetupSelectableEffect : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISubmitHandler
{
    /// <summary>キーボード・ゲームパッドで操作中の項目に表示する四隅の枠。</summary>
    [UnityEngine.Tooltip("キーボード・ゲームパッドで操作中の項目に表示する四隅の枠。")]
    [SerializeField] private CanvasGroup _focusFrame;
    /// <summary>マウスHoverまたは操作中に明るく重ねる層。</summary>
    [UnityEngine.Tooltip("マウスHoverまたは操作中に明るく重ねる層。")]
    [SerializeField] private CanvasGroup _hoverLayer;
    /// <summary>Toggleがオン（装備中）の間だけ常時表示する層。ボタンでは未設定でよい。</summary>
    [UnityEngine.Tooltip("Toggleがオン（装備中）の間だけ常時表示する層。ボタンでは未設定でよい。")]
    [SerializeField] private CanvasGroup _selectedLayer;
    /// <summary>決定時に一瞬だけ光らせる層。未設定なら拡縮だけで反応する。</summary>
    [UnityEngine.Tooltip("決定時に一瞬だけ光らせる層。未設定なら拡縮だけで反応する。")]
    [SerializeField] private CanvasGroup _pressFlash;
    /// <summary>Hover・フォーカス・決定で拡縮させる表示部分。未設定ならこのオブジェクト自身。</summary>
    [UnityEngine.Tooltip("Hover・フォーカス・決定で拡縮させる表示部分。未設定ならこのオブジェクト自身。")]
    [SerializeField] private RectTransform _scaleTarget;
    /// <summary>操作フォーカス中の拡大率。</summary>
    [UnityEngine.Tooltip("操作フォーカス中の拡大率。")]
    [SerializeField] private float _focusScale = 1.03f;
    /// <summary>マウスHover中の拡大率。</summary>
    [UnityEngine.Tooltip("マウスHover中の拡大率。")]
    [SerializeField] private float _hoverScale = 1.015f;
    /// <summary>フォーカス枠が現れるときの開始倍率。外側から項目へ寄る動きにする。</summary>
    [UnityEngine.Tooltip("フォーカス枠が現れるときの開始倍率。外側から項目へ寄る動きにする。")]
    [SerializeField] private float _focusFrameStartScale = 1.08f;
    /// <summary>装備中の層が現れるときの開始倍率。</summary>
    [UnityEngine.Tooltip("装備中の層が現れるときの開始倍率。")]
    [SerializeField] private float _selectedStartScale = 1.06f;
    /// <summary>決定時に押し込む拡縮量。</summary>
    [UnityEngine.Tooltip("決定時に押し込む拡縮量。")]
    [SerializeField] private float _pressPunch = -0.05f;
    /// <summary>Hover・フォーカス表示の切り替え時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Hover・フォーカス表示の切り替え時間（実時間の秒）。")]
    [SerializeField] private float _stateDuration = 0.12f;
    /// <summary>装備中の層が現れる時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("装備中の層が現れる時間（実時間の秒）。")]
    [SerializeField] private float _selectDuration = 0.22f;
    /// <summary>決定時の押し込みと光の時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("決定時の押し込みと光の時間（実時間の秒）。")]
    [SerializeField] private float _pressDuration = 0.25f;

    private Selectable _selectable;
    private Toggle _toggle;
    private Tween _scaleTween, _focusTween, _focusScaleTween, _hoverTween, _selectedTween, _selectedScaleTween, _flashTween;
    private bool _focused, _hovered, _initialized;

    /// <summary>参照を補完し、Toggleなら装備状態の変化を購読する。</summary>
    private void Awake() => Init();

    /// <summary>対象Selectableを取得し、各層を非表示から始める。</summary>
    private void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _selectable = GetComponent<Selectable>();
        _toggle = _selectable as Toggle;
        if (_scaleTarget == null) _scaleTarget = (RectTransform)transform;
        SetAlpha(_focusFrame, 0f);
        SetAlpha(_hoverLayer, 0f);
        SetAlpha(_pressFlash, 0f);
        if (_toggle != null) _toggle.onValueChanged.AddListener(value => ShowSelected(value, true));
        ShowSelected(_toggle != null && _toggle.isOn, false);
    }

    /// <summary>
    /// 装備中の層を表示または非表示にする。通知なしでToggleを切り替えた後は animate=false で同期する。
    /// </summary>
    public void ShowSelected(bool value, bool animate)
    {
        if (_selectedLayer == null) return;
        _selectedTween?.Kill();
        _selectedScaleTween?.Kill();
        var layer = _selectedLayer.transform;
        if (!animate || !isActiveAndEnabled)
        {
            _selectedLayer.alpha = value ? 1f : 0f;
            layer.localScale = Vector3.one;
            return;
        }
        _selectedTween = _selectedLayer.DOFade(value ? 1f : 0f, _selectDuration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        if (!value) return;
        // 枠が外側からカードへ収まる動きで、どれを装備したかを短く強調する。
        layer.localScale = Vector3.one * _selectedStartScale;
        _selectedScaleTween = layer.DOScale(1f, _selectDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
    }

    /// <summary>操作フォーカスを受けたことを表示する。装備は変更しない。</summary>
    public void OnSelect(BaseEventData eventData)
    {
        _focused = true;
        if (_focusFrame != null) _focusFrame.transform.localScale = Vector3.one * _focusFrameStartScale;
        Apply();
    }

    /// <summary>操作フォーカスが外れたことを表示する。</summary>
    public void OnDeselect(BaseEventData eventData)
    {
        _focused = false;
        Apply();
    }

    /// <summary>操作可能な項目にマウスが乗ったことを表示する。</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        Apply();
    }

    /// <summary>マウスが離れたことを表示する。</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        Apply();
    }

    /// <summary>クリックによる決定を短く押し込んで返す。</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) PlayPress();
    }

    /// <summary>キーボード・ゲームパッドの決定を短く押し込んで返す。</summary>
    public void OnSubmit(BaseEventData eventData) => PlayPress();

    /// <summary>現在のHover・フォーカス状態に合わせて層の不透明度と拡大率を補間する。</summary>
    private void Apply()
    {
        bool interactable = _selectable != null && _selectable.IsInteractable();
        bool focused = _focused && interactable;
        bool hovered = _hovered && interactable;
        _focusTween?.Kill();
        _focusScaleTween?.Kill();
        _hoverTween?.Kill();
        _scaleTween?.Kill();
        if (_focusFrame != null)
        {
            _focusTween = _focusFrame.DOFade(focused ? 1f : 0f, _stateDuration).SetUpdate(true).SetLink(gameObject);
            _focusScaleTween = _focusFrame.transform.DOScale(1f, _stateDuration * 1.5f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        if (_hoverLayer != null)
            _hoverTween = _hoverLayer.DOFade(focused || hovered ? 1f : 0f, _stateDuration).SetUpdate(true).SetLink(gameObject);
        float scale = focused ? _focusScale : hovered ? _hoverScale : 1f;
        _scaleTween = _scaleTarget.DOScale(scale, _stateDuration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
    }

    /// <summary>決定時の押し込みと光を再生する。連打時は前の演出を打ち切って重ねない。</summary>
    private void PlayPress()
    {
        // ボタンは決定処理で操作不可になった直後にも呼ばれるため、Toggleだけ操作可能かを確認する。
        if (_toggle != null && !_toggle.IsInteractable()) return;
        _scaleTween?.Kill();
        float scale = _focused ? _focusScale : _hovered ? _hoverScale : 1f;
        _scaleTarget.localScale = Vector3.one * scale;
        _scaleTween = _scaleTarget.DOPunchScale(Vector3.one * _pressPunch, _pressDuration, 6, 0.6f).SetUpdate(true).SetLink(gameObject);
        if (_pressFlash == null) return;
        _flashTween?.Kill();
        _pressFlash.alpha = 1f;
        _flashTween = _pressFlash.DOFade(0f, _pressDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
    }

    /// <summary>非表示になる時に演出を止め、次回表示で途中状態が残らないようにする。</summary>
    private void OnDisable()
    {
        KillTweens();
        _focused = false;
        _hovered = false;
        SetAlpha(_focusFrame, 0f);
        SetAlpha(_hoverLayer, 0f);
        SetAlpha(_pressFlash, 0f);
        if (_scaleTarget != null) _scaleTarget.localScale = Vector3.one;
        if (_selectedLayer != null) _selectedLayer.transform.localScale = Vector3.one;
    }

    /// <summary>このコンポーネントが所有するTweenをすべて停止する。</summary>
    private void KillTweens()
    {
        _scaleTween?.Kill();
        _focusTween?.Kill();
        _focusScaleTween?.Kill();
        _hoverTween?.Kill();
        _selectedTween?.Kill();
        _selectedScaleTween?.Kill();
        _flashTween?.Kill();
    }

    /// <summary>未設定を許容してCanvasGroupの不透明度を即時設定する。</summary>
    private static void SetAlpha(CanvasGroup group, float alpha) { if (group != null) group.alpha = alpha; }
}
