using System.Collections.Generic;
using UnityEngine;

/// <summary>Presentation only. No Animator state, AI, physics, hitbox or movement writes.</summary>
public sealed class EnemyBoneHitReaction
{
    sealed class BoneOffset
    {
        public Transform Bone;
        public float Weight, Delay;
        public Quaternion BasePose, AppliedPose;
        public Vector3 Start, Current;
        public bool Applied;
    }
    readonly Transform _root;
    readonly List<BoneOffset> _bones = new List<BoneOffset>();
    Vector3 _target;
    float _elapsed, _duration;
    bool _active, _continuing;
    public bool IsActive => _active;
    public float Elapsed => _elapsed;

    /// <summary>Humanoidの上半身の骨を取得し、通常被弾を分散する重みと遅延を準備する。</summary>
    public EnemyBoneHitReaction(Transform root, Animator animator)
    {
        _root = root;
        if (animator == null || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
        Add(animator, HumanBodyBones.Spine, .10f, 0f);
        Add(animator, HumanBodyBones.Chest, .78f, 0f);
        Add(animator, HumanBodyBones.UpperChest, .06f, .006f);
        Add(animator, HumanBodyBones.Neck, .04f, .012f);
        Add(animator, HumanBodyBones.Head, .02f, .020f);
        float sum = 0f;
        foreach (var bone in _bones) sum += bone.Weight;
        if (sum > 0f) foreach (var bone in _bones) bone.Weight /= sum;
    }

    void Add(Animator animator, HumanBodyBones id, float weight, float delay)
    {
        var bone = animator.GetBoneTransform(id);
        if (bone != null && bone != _root && !_bones.Exists(x => x.Bone == bone))
            _bones.Add(new BoneOffset { Bone = bone, Weight = weight, Delay = delay });
    }

    /// <summary>攻撃者から離れる水平衝撃方向を求め、Enemyのローカル座標へ変換する。</summary>
    /// <returns>通常被弾の傾きに使用するローカル衝撃方向。</returns>
    public static Vector3 GetLocalImpulse(Transform root, DamageInfo info)
    {
        Vector3 away = info.Instigator != null ? root.position - info.Instigator.transform.position : Vector3.zero;
        away.y = 0f;
        if (away.sqrMagnitude < .0001f) away = Vector3.ProjectOnPlane(info.HitNormal, Vector3.up);
        if (away.sqrMagnitude < .0001f) away = Vector3.ProjectOnPlane(root.position - info.HitPoint, Vector3.up);
        if (away.sqrMagnitude < .0001f) away = -root.forward;
        return root.InverseTransformDirection(away.normalized);
    }

    /// <summary>命中方向と攻撃角度から骨補正の目標を設定する。再被弾は現在値から接続して補正を累積しない。</summary>
    public void Begin(DamageInfo info, float angle, float duration, float attackScale)
    {
        _continuing = _active;
        RemoveOffsets();
        foreach (var bone in _bones) bone.Start = _continuing ? bone.Current : Vector3.zero;
        _target = Vector3.Cross(Vector3.up, GetLocalImpulse(_root, info)).normalized * Mathf.Clamp(angle, 0f, 16f) * Mathf.Clamp01(attackScale);
        _elapsed = 0f;
        _duration = Mathf.Max(.01f, duration);
        _active = _bones.Count > 0;
    }

    /// <summary>衝撃初期の立ち上がりと後半の減衰を持つ骨反応の重みを計算する。</summary>
    /// <returns>被弾経過時間に対応する0から1の重み。</returns>
    public static float Envelope(float age, float duration)
    {
        if (age < 0f || age >= duration) return 0f;
        float peak = duration * .18f;
        if (age < peak) return Mathf.Lerp(.28f, 1f, Mathf.SmoothStep(0f, 1f, age / peak));
        return 1f - Mathf.SmoothStep(0f, 1f, (age - peak) / (duration - peak));
    }

    /// <summary>Animator速度を掛けた時間で骨反応を進め、HitStop中は進行を止める。</summary>
    public void Tick(float deltaTime, float animatorSpeed)
    {
        if (!_active) return;
        _elapsed += Mathf.Max(0f, deltaTime) * Mathf.Max(0f, animatorSpeed);
        if (_elapsed >= _duration + .020f) Clear();
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
            Vector3 target = _target * bone.Weight * Envelope(_elapsed - bone.Delay, _duration);
            float blend = _continuing ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / .025f)) : 1f;
            bone.Current = Vector3.Lerp(bone.Start, target, blend);
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
            if (bone.Bone != null && bone.Applied && Quaternion.Angle(bone.Bone.localRotation, bone.AppliedPose) < .005f)
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
