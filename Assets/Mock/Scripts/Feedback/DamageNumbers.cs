using TMPro;
using UnityEngine;

/// <summary>シーンに配置したキャンバスと文字テンプレートを使い、戦闘前に一度だけ表示用プールを準備する。</summary>
public sealed class DamageNumbers : MonoBehaviour
{
    [Header("uGUI references")]
    [SerializeField] private RectTransform _container;
    [SerializeField] private TMP_Text _normalTemplate;
    [SerializeField] private TMP_Text _criticalTemplate;
    [SerializeField] private Camera _camera;
    [Header("Motion")]
    [SerializeField, Min(.05f)] private float _lifetime = .85f;
    [SerializeField] private Vector3 _worldOffset = new Vector3(0, 1.85f, 0);
    [SerializeField] private Vector2 _screenOffset;
    [Tooltip("Random offset range in UI local units. Set both values to zero to disable.")]
    [SerializeField] private Vector2 _randomOffsetRange = new Vector2(24f, 12f);
    [SerializeField] private float _riseSpeed = .8f;
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
    public void Init(Camera camera)
    {
        if (_initialized) return;
        if (camera != null) _camera = camera;
        _initialized = Initialize();
    }
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
    public void Show(Vector3 position, float amount, bool isCritical)
    {
        if (!_initialized || !isActiveAndEnabled || amount <= 0) return;
        Display(position, amount, isCritical);
    }
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
    private Vector2 CreateRandomOffset()
    {
        // 上昇中に文字が揺れないよう、ランダムなずれは命中時に一度だけ決める。
        return Vector2.Scale(Random.insideUnitCircle,
            new Vector2(Mathf.Abs(_randomOffsetRange.x), Mathf.Abs(_randomOffsetRange.y)));
    }
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
    private void OnDisable()
    {
        if (active == null) return;
        for (int i = 0; i < active.Length; i++) if (active[i] != null) { active[i].gameObject.SetActive(false); active[i] = null; }
    }
}
