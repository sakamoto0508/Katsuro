using TMPro;
using UnityEngine;

/// <summary>Scene-authored canvas and label templates, pooled once before combat.</summary>
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
    [SerializeField] private float _riseSpeed = .8f;
    [SerializeField, Range(1, 128)] private int _poolSize = 32;
    private static DamageNumbers instance;
    private TMP_Text[] normal, critical;
    private TMP_Text[] active;
    private Vector3[] positions;
    private float[] started, opacity;
    private int next;
    private Canvas canvas;
    private bool[] criticalHit;

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (instance != null && instance != this) { enabled = false; return; }
        instance = this;
        Initialize();
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
        int count = Mathf.Clamp(_poolSize, 1, 128);
        criticalHit = new bool[count];
        normal = new TMP_Text[count]; critical = new TMP_Text[count]; active = new TMP_Text[count];
        positions = new Vector3[count]; started = new float[count]; opacity = new float[count];
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
    public static void Prepare(Camera camera = null)
    {
        if (instance == null) instance = FindFirstObjectByType<DamageNumbers>();
        if (instance == null) { Debug.LogError("Place DamageNumbersCanvas in the combat scene."); return; }
        if (camera != null) instance._camera = camera;
        instance.Init();
    }
    public static void Show(Vector3 position, float amount, bool isCritical)
    {
        if (amount <= 0) return;
        if (instance == null) return;
        if (instance == null || instance.normal == null) return;
        instance.Display(position, amount, isCritical);
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
        started[index] = Time.unscaledTime;
        opacity[index] = isCritical ? _criticalTemplate.color.a : _normalTemplate.color.a;
        label.SetText("{0}", Mathf.Ceil(amount));
        label.gameObject.SetActive(true);
        PositionLabel(index, 0);
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
        if (_camera == null || !_camera.isActiveAndEnabled) _camera = Camera.main;
        var label = active[index];
        if (_camera == null) { label.alpha = 0; return; }
        Vector3 screen = _camera.WorldToScreenPoint(positions[index] + Vector3.up * age * _riseSpeed);
        if (screen.z <= 0) { label.alpha = 0; return; }
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screen, uiCamera, out var point);
        // localPosition respects the template's pivot/size without assuming centered anchors.
        var offset = criticalHit[index] ? _criticalTemplate.rectTransform.localPosition : _normalTemplate.rectTransform.localPosition;
        label.rectTransform.localPosition = new Vector3(point.x + _screenOffset.x + offset.x, point.y + _screenOffset.y + offset.y, 0);
        label.alpha = opacity[index] * (1f - age / Mathf.Max(.05f, _lifetime));
    }
    private void OnDisable()
    {
        if (active == null) return;
        for (int i = 0; i < active.Length; i++) if (active[i] != null) { active[i].gameObject.SetActive(false); active[i] = null; }
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
