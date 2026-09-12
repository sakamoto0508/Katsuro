using UnityEngine;

/// <summary> キャラクターの攻撃や被弾時のフィードバックを管理するコンポーネント。 </summary>
[DisallowMultipleComponent]
public sealed class CombatFeedback : MonoBehaviour
{
    [Header("Ghost shimmer")]
    /// <summary> ゴースト表示時の色。 </summary>
    [SerializeField] private Color _ghostTint = new Color(.66f, .78f, .8f, .35f);
    /// <summary>
    /// ゴースト表示時の揺れの大きさ。値が大きいほど揺れが大きくなる。
    /// </summary>
    [SerializeField, Range(0f, .03f)] private float _ghostSway = .009f;
    /// <summary>
    /// ゴースト表示時の流れる速度。値が大きいほど流れが速くなる。
    /// </summary>
    [SerializeField, Range(.1f, 4f)] private float _ghostFlowSpeed = 1.2f;
    private Renderer[] _renderers;
    private Material[][] _original, _glow;
    private Material _material;
    private Transform _bone;
    private Quaternion _applied = Quaternion.identity;
    private float _hitUntil;
    private bool _ghost, _showing;
    private bool _initialized;

    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        var shader = Resources.Load<Shader>("CombatGlow");
        if (shader != null) _material = new Material(shader);
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        _original = new Material[_renderers.Length][];
        _glow = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _original[i] = _renderers[i].sharedMaterials;
            _glow[i] = new Material[_original[i].Length];
            for (int j = 0; j < _glow[i].Length; j++) _glow[i][j] = _material;
        }
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) _bone = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (_bone == null && _renderers.Length > 0) _bone = ((SkinnedMeshRenderer)_renderers[0]).rootBone;
    }

    /// <summary>
    /// ゴースト表示を有効または無効にします。
    /// </summary>
    /// <param name="active"></param>
    public void SetGhost(bool active) { _ghost = active; Refresh(); }

    /// <summary>
    /// 攻撃や被弾時のヒットエフェクトをトリガーします。
    /// </summary>
    public void Hit() { _hitUntil = Time.unscaledTime + .18f; Refresh(); }

    private void Update() 
    { 
        RemoveOffset(); 
        Refresh(); 
    }

    private void LateUpdate()
    {
        if (_bone == null) return;
        // ヒットエフェクトの残り時間に応じてボーンを揺らす
        float remaining = Mathf.Clamp01((_hitUntil - Time.unscaledTime) / .18f);
        _applied = Quaternion.Euler(-10f * remaining * Mathf.Sin(remaining * Mathf.PI), 0, 0);
        _bone.localRotation *= _applied;
    }

    /// <summary>
    /// ボーンの回転オフセットをリセットします。
    /// </summary>
    private void RemoveOffset()
    {
        if (_bone != null) _bone.localRotation *= Quaternion.Inverse(_applied);
        _applied = Quaternion.identity;
    }

    /// <summary>
    /// レンダラーのマテリアルを更新して、ゴースト表示やヒットエフェクトの状態を反映します。
    /// </summary>
    private void Refresh()
    {
        bool hit = Time.unscaledTime < _hitUntil;
        bool visible = _material != null && (hit || _ghost);
        if (_material != null && visible)
        {
            _material.SetFloat("_Ghost", _ghost && !hit ? 1f : 0f);
            _material.SetColor("_Tint", hit ? new Color(1f, .75f, .6f, .9f) : _ghostTint);
            if (_ghost && !hit)
            {
                _material.SetFloat("_GhostTime", Time.unscaledTime);
                _material.SetFloat("_GhostSway", _ghostSway);
                _material.SetFloat("_GhostFlowSpeed", _ghostFlowSpeed);
            }
        }
        if (visible == _showing) return;
        _showing = visible;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].sharedMaterials = visible ? _glow[i] : _original[i];
    }

    private void OnDisable()
    {
        RemoveOffset();
        _ghost = false; _hitUntil = 0;
        Refresh();
    }

    private void OnDestroy() 
    { 
        if (_material != null) Destroy(_material); 
    }
}
