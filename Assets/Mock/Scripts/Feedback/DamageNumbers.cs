using TMPro;
using UnityEngine;

/// <summary>シーンに配置したキャンバスと文字テンプレートを使い、戦闘前に一度だけ表示用プールを準備する。</summary>
public sealed class DamageNumbers : MonoBehaviour
{
    /// <summary>ダメージ数字のUIを生成・配置する親RectTransform。</summary>
    [UnityEngine.Tooltip("ダメージ数字のUIを生成・配置する親RectTransform。")]
    [Header("uGUI references")]
    [SerializeField] private RectTransform _container;
    /// <summary>通常命中のダメージ数字に使うTMPテンプレート。</summary>
    [UnityEngine.Tooltip("通常命中のダメージ数字に使うTMPテンプレート。")]
    [SerializeField] private TMP_Text _normalTemplate;
    /// <summary>強い命中のダメージ数字に使う必須TMPテンプレート。通常用と両方割り当てる。</summary>
    [UnityEngine.Tooltip("強い命中のダメージ数字に使う必須TMPテンプレート。通常用と両方割り当てる。")]
    [SerializeField] private TMP_Text _criticalTemplate;
    /// <summary>命中のワールド位置を画面座標へ変換するCamera。</summary>
    [UnityEngine.Tooltip("命中のワールド位置を画面座標へ変換するCamera。")]
    [SerializeField] private Camera _camera;
    /// <summary>ダメージ数字を表示して消すまでの時間（秒）。</summary>
    [UnityEngine.Tooltip("ダメージ数字を表示して消すまでの時間（秒）。")]
    [Header("Motion")]
    [SerializeField, Min(.05f)] private float _lifetime = .85f;
    /// <summary>命中位置へ加えるワールド座標の表示ずらし（Unity単位）。</summary>
    [UnityEngine.Tooltip("命中位置へ加えるワールド座標の表示ずらし（Unity単位）。")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0, 1.85f, 0);
    /// <summary>画面へ投影した後に加えるUI座標の表示ずらし。</summary>
    [UnityEngine.Tooltip("画面へ投影した後に加えるUI座標の表示ずらし。")]
    [SerializeField] private Vector2 _screenOffset;
    /// <summary>数字の表示位置に加えるランダムなずらし幅（UIローカル単位）。両成分を0にすると無効。</summary>
    [Tooltip("数字の表示位置に加えるランダムなずらし幅（UIローカル単位）。両成分を0にすると無効。")]
    [SerializeField] private Vector2 _randomOffsetRange = new Vector2(24f, 12f);
    /// <summary>数字の投影元ワールド位置が上昇する速度（Unity単位/秒）。その位置を毎フレーム画面へ投影する。</summary>
    [UnityEngine.Tooltip("数字の投影元ワールド位置が上昇する速度（Unity単位/秒）。その位置を毎フレーム画面へ投影する。")]
    [SerializeField] private float _riseSpeed = .8f;
    /// <summary>初期化時に用意するダメージ数字のプール数。</summary>
    [UnityEngine.Tooltip("初期化時に用意するダメージ数字のプール数。")]
    [SerializeField, Range(1, 128)] private int _poolSize = 32;
    private TMP_Text[] normal, critical;
    private TMP_Text[] active;
    private Vector3[] positions;
    private Vector2[] randomOffsets;
    private float[] started, opacity;
    private int next;
    private Canvas canvas;
    private bool[] criticalHit;
    private bool _initialized;
    private bool _presentationSuppressed;
    /// <summary>勝利などの演出中に新しいダメージ数値の表示を抑制する。</summary>
    public void SetPresentationSuppressed(bool suppressed) => _presentationSuppressed = suppressed;
    /// <summary>使用カメラを接続し、配置済みuGUIテンプレートから表示プールを一度だけ準備する。</summary>
    public void Init(Camera camera)
    {
        if (_initialized) return;
        if (camera != null) _camera = camera;
        _initialized = Initialize();
    }
    /// <summary>参照を検証し、通常・Criticalの数値ラベルを固定数生成して非表示で待機させる。</summary>
    /// <returns>表示プールを使用可能になった場合はtrue。</returns>
    private bool Initialize()
    {
        if (normal != null) return true;
        if (_container == null || _normalTemplate == null || _criticalTemplate == null)
        {
            Debug.LogError("DamageNumbers requires a container and both uGUI text templates.", this);
            return false;
        }
        canvas = _container.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ダメージ表示のコンテナをCanvasの子に配置してください。", this);
            return false;
        }
        int count = Mathf.Clamp(_poolSize, 1, 128);
        criticalHit = new bool[count];
        normal = new TMP_Text[count]; critical = new TMP_Text[count]; active = new TMP_Text[count];
        positions = new Vector3[count]; started = new float[count]; opacity = new float[count];
        randomOffsets = new Vector2[count];
        _normalTemplate.gameObject.SetActive(false);
        _criticalTemplate.gameObject.SetActive(false);
        for (int i = 0; i < count; i++)
        {
            normal[i] = Instantiate(_normalTemplate, _container, false);
            critical[i] = Instantiate(_criticalTemplate, _container, false);
            normal[i].name = "Damage " + i;
            critical[i].name = "Critical " + i;
            normal[i].raycastTarget = critical[i].raycastTarget = false;
        }
        return true;
    }
    /// <summary>有効な正のダメージだけを表示し、演出による抑制中は表示要求を無視する。</summary>
    public void Show(Vector3 position, float amount, bool isCritical)
    {
        if (!_initialized || _presentationSuppressed || !isActiveAndEnabled || amount <= 0) return;
        Display(position, amount, isCritical);
    }
    /// <summary>Critical種別に応じた再利用ラベルへ数値を設定し、位置と表示寿命を初期化する。</summary>
    private void Display(Vector3 position, float amount, bool isCritical)
    {
        int index = next;
        next = (next + 1) % active.Length;
        if (active[index] != null) active[index].gameObject.SetActive(false);
        var label = isCritical ? critical[index] : normal[index];
        active[index] = label;
        criticalHit[index] = isCritical;
        positions[index] = position + _worldOffset;
        randomOffsets[index] = CreateRandomOffset();
        started[index] = Time.unscaledTime;
        opacity[index] = isCritical ? _criticalTemplate.color.a : _normalTemplate.color.a;
        label.SetText("{0}", Mathf.Ceil(amount));
        label.gameObject.SetActive(true);
        PositionLabel(index, 0);
    }
    /// <summary>Inspectorの範囲内で数値の画面上の位置ずれを生成する。</summary>
    /// <returns>各軸の指定範囲内のランダムOffset。</returns>
    private Vector2 CreateRandomOffset()
    {
        // 上昇中に文字が揺れないよう、ランダムなずれは命中時に一度だけ決める。
        return Vector2.Scale(Random.insideUnitCircle,
            new Vector2(Mathf.Abs(_randomOffsetRange.x), Mathf.Abs(_randomOffsetRange.y)));
    }
    /// <summary>実時間でラベルの浮上・透明度・寿命を更新し、期限を過ぎた表示をプールへ戻す。</summary>
    private void LateUpdate()
    {
        if (active == null) return;
        for (int i = 0; i < active.Length; i++)
        {
            if (active[i] == null) continue;
            float age = Time.unscaledTime - started[i];
            if (age >= Mathf.Max(.05f, _lifetime))
            {
                active[i].gameObject.SetActive(false); active[i] = null; continue;
            }
            PositionLabel(i, age);
        }
    }
    /// <summary>ワールド命中位置をCanvasへ投影し、上昇とOffsetを加えてラベルを配置する。</summary>
    private void PositionLabel(int index, float age)
    {
        var label = active[index];
        if (_camera == null || !_camera.isActiveAndEnabled) { label.alpha = 0; return; }
        Vector3 screen = _camera.WorldToScreenPoint(positions[index] + Vector3.up * age * _riseSpeed);
        if (screen.z <= 0) { label.alpha = 0; return; }
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screen, uiCamera, out var point);
        // アンカーが中央にあると仮定せず、ローカル座標でテンプレートの基準点とサイズを尊重する。
        var offset = criticalHit[index] ? _criticalTemplate.rectTransform.localPosition : _normalTemplate.rectTransform.localPosition;
        var randomOffset = randomOffsets[index];
        label.rectTransform.localPosition = new Vector3(point.x + _screenOffset.x + offset.x + randomOffset.x,
            point.y + _screenOffset.y + offset.y + randomOffset.y, 0);
        label.alpha = opacity[index] * (1f - age / Mathf.Max(.05f, _lifetime));
    }
    /// <summary>使用中の数値ラベルを非表示にして、停止後に表示が取り残されないようにする。</summary>
    private void OnDisable()
    {
        if (active == null) return;
        for (int i = 0; i < active.Length; i++) if (active[i] != null) { active[i].gameObject.SetActive(false); active[i] = null; }
    }
}
