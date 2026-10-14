using System.Collections.Generic;
using UnityEngine;

/// <summary>Presentation only. No Animator state, AI, physics, hitbox or movement writes.</summary>
public sealed class EnemyBoneHitReaction
{
    sealed class BoneOffset
    {
        public Transform Bone;
        public float Weight, Delay, HitWeight;
        public HumanBodyBones Id;
        public Quaternion BasePose, AppliedPose;
        public Vector3 Start, Current;
        public bool Applied;
    }
    readonly Transform _root;
    readonly List<BoneOffset> _bones = new List<BoneOffset>();
    Vector3 _target;
    float _elapsed, _duration;
    float _impactFraction, _followThroughFraction, _delayScale;
    bool _active, _continuing;
    public bool IsActive => _active;
    public float Elapsed => _elapsed;

    /// <summary>Humanoidの上半身の骨を取得し、通常被弾を分散する重みと遅延を準備する。</summary>
    public EnemyBoneHitReaction(Transform root, Animator animator)
    {
        _root = root;
        if (animator == null || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
        Add(animator, HumanBodyBones.Spine, .22f, 0f);
        Add(animator, HumanBodyBones.Chest, .42f, 0f);
        Add(animator, HumanBodyBones.UpperChest, .16f, .004f);
        Add(animator, HumanBodyBones.LeftShoulder, .06f, .006f);
        Add(animator, HumanBodyBones.RightShoulder, .06f, .006f);
        Add(animator, HumanBodyBones.Neck, .06f, .012f);
        Add(animator, HumanBodyBones.Head, .02f, .020f);
        float sum = 0f;
        foreach (var bone in _bones) sum += bone.Weight;
        if (sum > 0f) foreach (var bone in _bones) bone.Weight /= sum;
    }

    void Add(Animator animator, HumanBodyBones id, float weight, float delay)
    {
        var bone = animator.GetBoneTransform(id);
        if (bone != null && bone != _root && !_bones.Exists(x => x.Bone == bone))
                _bones.Add(new BoneOffset { Bone = bone, Id = id, Weight = weight, Delay = delay });
    }

    /// <summary>攻撃者から離れる水平衝撃方向を求め、Enemyのローカル座標へ変換する。</summary>
    /// <returns>通常被弾の傾きに使用するローカル衝撃方向。</returns>
    public static Vector3 GetLocalImpulse(Transform root, DamageInfo info)
    {
        if (root == null) return Vector3.back;
        Vector3 away = info.Instigator != null ? root.position - info.Instigator.transform.position : Vector3.zero;
        away.y = 0f;
        if (!Usable(away)) away = Vector3.ProjectOnPlane(root.position - info.HitPoint, Vector3.up);
        if (!Usable(away)) away = Vector3.ProjectOnPlane(info.HitNormal, Vector3.up);
        if (!Usable(away)) away = -root.forward;
        return root.InverseTransformDirection(away.normalized);
    }

    /// <summary>命中方向と攻撃角度から骨補正の目標を設定する。再被弾は現在値から接続して補正を累積しない。</summary>
    public void Begin(DamageInfo info, float angle, float duration, float attackScale,
        float impactFraction = .16f, float followThroughFraction = .14f)
    {
        if (!Finite(angle) || !Finite(duration) || !Finite(attackScale) || _root == null) return;
        if (info.IsJustAvoidCounter) { Clear(); return; }
        _continuing = _active;
        RemoveOffsets();
        Vector3 contact = _root.InverseTransformPoint(info.HitPoint);
        float side = Finite(contact.x) ? Mathf.Clamp(contact.x * 2f, -1f, 1f) : 0f;
        foreach (var bone in _bones)
        {
            bone.Start = _continuing ? bone.Current : Vector3.zero;
            float shoulder = bone.Id == HumanBodyBones.LeftShoulder ? -side :
                bone.Id == HumanBodyBones.RightShoulder ? side : 0f;
            bone.HitWeight = bone.Weight * (1f + shoulder * .4f);
        }
        _target = GetLocalRotationAxis(_root, info) * Mathf.Clamp(angle, 0f, 16f) * Mathf.Clamp01(attackScale);
        _elapsed = 0f;
        _duration = Mathf.Max(.01f, duration);
        _impactFraction = Finite(impactFraction) ? Mathf.Clamp(impactFraction, .08f, .3f) : .16f;
        _followThroughFraction = Finite(followThroughFraction) ? Mathf.Clamp(followThroughFraction, 0f, .25f) : .14f;
        _delayScale = info.IsHeavy ? 1.6f : 1f;
        _active = _bones.Count > 0;
    }

    /// <summary>刀の移動方向による胸の捻りと、攻撃者から離れる傾きを合成する。面法線を斬撃方向として扱わない。</summary>
    public static Vector3 GetLocalRotationAxis(Transform root, DamageInfo info)
    {
        Vector3 recoil = Vector3.Cross(Vector3.up, GetLocalImpulse(root, info));
        if (root == null || !Usable(info.SlashDirection)) return recoil.normalized;
        Vector3 slash = root.InverseTransformDirection(info.SlashDirection.normalized);
        Vector3 contact = root.InverseTransformPoint(info.HitPoint);
        // 横斬りは胸を捻り、縦斬りは接触した左右に応じてわずかに身体を傾ける。
        Vector3 cut = new Vector3(-slash.z * .35f, -slash.x * .8f,
            slash.x * .25f + slash.y * Mathf.Clamp(contact.x, -.5f, .5f));
        // 接触位置による回転モーメントも少量だけ混ぜ、左右の肩への命中を区別する。
        if (Finite(contact.x) && Finite(contact.y) && Finite(contact.z))
            cut += Vector3.Cross(Vector3.ClampMagnitude(contact - Vector3.up * 1.2f, .65f), slash) * .3f;
        Vector3 combined = recoil * .65f + cut;
        return Usable(combined) ? combined.normalized : recoil.normalized;
    }

    /// <summary>反応の数値入力が有限かを確認し、不正な角度・時間を骨へ渡さない。</summary>
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>方向ベクトルが有限で、正規化できる長さを持つか確認する。</summary>
    private static bool Usable(Vector3 value) => Finite(value.sqrMagnitude) && value.sqrMagnitude > .0001f;

    /// <summary>Quaternion.Angleの微小角度の丸め誤差を避け、前回加算した姿勢が残っているか成分で比較する。</summary>
    private static bool SamePose(Quaternion a, Quaternion b)
    {
        float sign = Quaternion.Dot(a, b) < 0f ? -1f : 1f;
        return Mathf.Abs(a.x - b.x * sign) < .00001f && Mathf.Abs(a.y - b.y * sign) < .00001f &&
            Mathf.Abs(a.z - b.z * sign) < .00001f && Mathf.Abs(a.w - b.w * sign) < .00001f;
    }

    /// <summary>衝撃初期の立ち上がりと後半の減衰を持つ骨反応の重みを計算する。</summary>
    /// <returns>被弾経過時間に対応する0から1の重み。</returns>
    public static float Envelope(float age, float duration, float impactFraction = .16f, float followThroughFraction = .14f)
    {
        if (!Finite(age) || !Finite(duration) || duration <= 0f || age < 0f || age >= duration) return 0f;
        float peak = duration * (Finite(impactFraction) ? Mathf.Clamp(impactFraction, .08f, .3f) : .16f);
        float follow = duration * (Finite(followThroughFraction) ? Mathf.Clamp(followThroughFraction, 0f, .25f) : .14f);
        if (age < peak) return Mathf.Lerp(.5f, 1f, Mathf.SmoothStep(0f, 1f, age / peak));
        if (follow > 0f && age < peak + follow)
            return Mathf.Lerp(1f, .88f, Mathf.SmoothStep(0f, 1f, (age - peak) / follow));
        return (follow > 0f ? .88f : 1f) * (1f - Mathf.SmoothStep(0f, 1f, (age - peak - follow) / (duration - peak - follow)));
    }

    /// <summary>Animator速度を掛けた時間で骨反応を進め、HitStop中は進行を止める。</summary>
    public void Tick(float deltaTime, float animatorSpeed)
    {
        if (!_active || !Finite(deltaTime) || !Finite(animatorSpeed)) return;
        _elapsed += Mathf.Max(0f, deltaTime) * Mathf.Max(0f, animatorSpeed);
        if (_elapsed >= _duration + .020f * _delayScale) Clear();
    }

    /// <summary>Animatorが更新した基準姿勢へ重み付き骨回転を重ね、次の更新で除去する姿勢を記録する。</summary>
    public void Apply()
    {
        // Also makes repeated LateUpdate/Hit calls safe when Animator did not evaluate.
        RemoveOffsets();
        if (!_active) return;
        foreach (var bone in _bones)
        {
            if (bone.Bone == null) continue;
            Vector3 target = _target * bone.HitWeight * Envelope(_elapsed - bone.Delay * _delayScale,
                _duration, _impactFraction, _followThroughFraction);
            float blend = _continuing ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / (_duration * _impactFraction))) : 1f;
            bone.Current = Vector3.ClampMagnitude(Vector3.Lerp(bone.Start, target, blend), 16f * bone.Weight * 1.4f);
            bone.BasePose = bone.Bone.localRotation;
            Vector3 axis = bone.Bone.parent.InverseTransformDirection(_root.TransformDirection(bone.Current));
            Quaternion offset = axis.sqrMagnitude > .000001f ? Quaternion.AngleAxis(bone.Current.magnitude, axis.normalized) : Quaternion.identity;
            bone.AppliedPose = offset * bone.BasePose;
            bone.Bone.localRotation = bone.AppliedPose;
            bone.Applied = true;
        }
    }

    /// <summary>前回の補正姿勢が残っている骨だけ基準姿勢へ戻し、新しく評価されたAnimator姿勢を保護する。</summary>
    public void RemoveOffsets()
    {
        foreach (var bone in _bones)
        {
            if (bone.Bone != null && bone.Applied && SamePose(bone.Bone.localRotation, bone.AppliedPose))
                bone.Bone.localRotation = bone.BasePose;
            // If Animator has already written a fresh pose, never subtract an old offset.
            bone.Applied = false;
        }
    }

    /// <summary>全骨の補正を除去し、通常被弾の目標・時計・所有状態を消去する。</summary>
    public void Clear()
    {
        RemoveOffsets();
        _active = _continuing = false;
        _elapsed = 0f;
        foreach (var bone in _bones) bone.Start = bone.Current = Vector3.zero;
    }
}
