using UnityEngine;

/// <summary>既存追撃Reactionの時計で動く骨補正。Root、Collider、AI、拘束時間には触れない。</summary>
internal sealed class CounterHitPose
{
    private sealed class Bone
    {
        public Transform Transform;
        public float Weight;
        public int Kind;
        public Quaternion Base, Applied;
        public bool HasOffset;
    }
    private readonly System.Collections.Generic.List<Bone> _bones = new System.Collections.Generic.List<Bone>();
    private readonly Transform _root;
    private JustAvoidCounterConfig.Strike _style;
    private Vector3 _impactAxis, _followAxis;
    private float _duration;

    public CounterHitPose(Transform root, Animator animator)
    {
        _root = root;
        if (animator == null || !animator.isHuman) return;
        Add(animator, HumanBodyBones.Spine, .28f, 0);
        Add(animator, HumanBodyBones.Chest, .42f, 0);
        Add(animator, HumanBodyBones.UpperChest, .16f, 0);
        Add(animator, HumanBodyBones.LeftShoulder, .07f, 0);
        Add(animator, HumanBodyBones.RightShoulder, .07f, 0);
        Add(animator, HumanBodyBones.Neck, .35f, 1);
        Add(animator, HumanBodyBones.Head, .65f, 1);
        Add(animator, HumanBodyBones.Hips, 1f, 2);
    }

    private void Add(Animator animator, HumanBodyBones id, float weight, int kind)
    {
        var bone = animator.GetBoneTransform(id);
        if (bone != null) _bones.Add(new Bone { Transform = bone, Weight = weight, Kind = kind });
    }

    public void Begin(DamageInfo info, JustAvoidCounterConfig.Strike style, float duration)
    {
        Clear();
        _style = style;
        _duration = duration;
        _impactAxis = EnemyBoneHitReaction.GetLocalRotationAxis(_root, info);
        Vector3 away = EnemyBoneHitReaction.GetLocalImpulse(_root, info);
        _followAxis = (_impactAxis * .45f + new Vector3(-Mathf.Sign(away.z) * .8f, 0f, Mathf.Sign(away.x) * .25f)).normalized;
        Apply(0f);
    }

    public void Apply(float elapsed)
    {
        RemoveOffsets();
        if (_style == null) return;
        float peak = Mathf.Min(_duration * .6f, _style.FollowThroughPeak);
        float toFollow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, peak)));
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(peak, _duration, elapsed));
        Vector3 torso = Vector3.Lerp(_impactAxis * _style.TorsoImpact, _followAxis * _style.TorsoFollowThrough, toFollow) * fade;
        foreach (var bone in _bones)
        {
            if (bone.Transform == null || bone.Transform.parent == null) continue;
            Vector3 rotation = bone.Kind == 0 ? torso * bone.Weight : bone.Kind == 1
                ? Vector3.Lerp(-_impactAxis * _style.HeadRecoil, _followAxis * (_style.HeadRecoil * .65f), toFollow) * (fade * bone.Weight)
                : _followAxis * (_style.HipFollowThrough * Mathf.Lerp(.25f, 1f, toFollow) * fade);
            bone.Base = bone.Transform.localRotation;
            Vector3 worldAxis = _root.TransformDirection(rotation);
            Vector3 localAxis = bone.Transform.parent.InverseTransformDirection(worldAxis);
            bone.Applied = localAxis.sqrMagnitude > .000001f ? Quaternion.AngleAxis(rotation.magnitude, localAxis.normalized) * bone.Base : bone.Base;
            bone.Transform.localRotation = bone.Applied;
            bone.HasOffset = true;
        }
    }

    public void RemoveOffsets()
    {
        foreach (var bone in _bones)
        {
            if (bone.Transform != null && bone.HasOffset && Mathf.Abs(Quaternion.Dot(bone.Transform.localRotation, bone.Applied)) > .9999999f)
                bone.Transform.localRotation = bone.Base;
            bone.HasOffset = false;
        }
    }

    public void Clear() { RemoveOffsets(); _style = null; }
}
