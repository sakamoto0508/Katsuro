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
    private Renderer[] renderers;
    private Material[][] original, glow;
    private Material material;
    private Transform bone;
    private Quaternion applied = Quaternion.identity;
    private float hitUntil;
    private bool ghost, showing;

    /// <summary>
    /// 指定した GameObject に CombatFeedback コンポーネントを取得または追加します。
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    public static CombatFeedback For(GameObject owner)
    {
        var value = owner.GetComponent<CombatFeedback>();
        return value != null ? value : owner.AddComponent<CombatFeedback>();
    }

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        var shader = Resources.Load<Shader>("CombatGlow");
        if (shader != null) material = new Material(shader);
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        original = new Material[renderers.Length][];
        glow = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            original[i] = renderers[i].sharedMaterials;
            glow[i] = new Material[original[i].Length];
            for (int j = 0; j < glow[i].Length; j++) glow[i][j] = material;
        }
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) bone = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (bone == null && renderers.Length > 0) bone = ((SkinnedMeshRenderer)renderers[0]).rootBone;
    }

    /// <summary>
    /// ゴースト表示を有効または無効にします。
    /// </summary>
    /// <param name="active"></param>
    public void SetGhost(bool active) { ghost = active; Refresh(); }

    /// <summary>
    /// 攻撃や被弾時のヒットエフェクトをトリガーします。
    /// </summary>
    public void Hit() { hitUntil = Time.unscaledTime + .18f; Refresh(); }

    private void Update() 
    { 
        RemoveOffset(); 
        Refresh(); 
    }

    private void LateUpdate()
    {
        if (bone == null) return;
        float remaining = Mathf.Clamp01((hitUntil - Time.unscaledTime) / .18f);
        applied = Quaternion.Euler(-10f * remaining * Mathf.Sin(remaining * Mathf.PI), 0, 0);
        bone.localRotation *= applied;
    }

    /// <summary>
    /// ボーンの回転オフセットをリセットします。
    /// </summary>
    private void RemoveOffset()
    {
        if (bone != null) bone.localRotation *= Quaternion.Inverse(applied);
        applied = Quaternion.identity;
    }

    /// <summary>
    /// レンダラーのマテリアルを更新して、ゴースト表示やヒットエフェクトの状態を反映します。
    /// </summary>
    private void Refresh()
    {
        bool hit = Time.unscaledTime < hitUntil;
        bool visible = material != null && (hit || ghost);
        if (material != null && visible)
        {
            material.SetFloat("_Ghost", ghost && !hit ? 1f : 0f);
            material.SetColor("_Tint", hit ? new Color(1f, .75f, .6f, .9f) : _ghostTint);
            if (ghost && !hit)
            {
                material.SetFloat("_GhostTime", Time.unscaledTime);
                material.SetFloat("_GhostSway", _ghostSway);
                material.SetFloat("_GhostFlowSpeed", _ghostFlowSpeed);
            }
        }
        if (visible == showing) return;
        showing = visible;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sharedMaterials = visible ? glow[i] : original[i];
    }

    private void OnDisable()
    {
        RemoveOffset();
        ghost = false; hitUntil = 0;
        Refresh();
    }

    private void OnDestroy() 
    { 
        if (material != null) Destroy(material); 
    }
}
