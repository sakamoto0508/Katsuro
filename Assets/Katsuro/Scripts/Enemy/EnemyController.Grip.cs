using UnityEngine;

/// <summary>Poseに合わせた一時的な刀のGrip。装備Prefabと他の攻撃のGripは変更しない。</summary>
public partial class EnemyController
{
    private Transform _patternGripWeapon;
    private Transform _releasedPatternGripWeapon;
    private int _patternGripStateHash;
    private int _releasedPatternGripStateHash;
    private Vector3 _patternGripPosition;
    private Quaternion _patternGripRotation;
    private Vector3 _patternGripTargetPosition;
    private Quaternion _patternGripTargetRotation;

    private void ApplyPatternWeaponGrip(EnemyAttackPattern pattern)
    {
        RestorePatternWeaponGrip();
        _releasedPatternGripWeapon = null;
        if (pattern == null || !pattern.UsesDashTuning || !pattern.OverrideWeaponGrip || _animator == null || !_animator.isHuman) return;
        var hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null) return;
        // The existing sword mesh owns its colliders and trail underneath RightHand.
        foreach (var mesh in hand.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.GetComponentInChildren<WeaponHitboxRelay>(true) == null) continue;
            _patternGripWeapon = mesh.transform;
            _patternGripStateHash = pattern.StateHash;
            _patternGripPosition = mesh.transform.localPosition;
            _patternGripRotation = mesh.transform.localRotation;
            _patternGripTargetPosition = pattern.WeaponGripLocalPosition;
            _patternGripTargetRotation = Quaternion.Euler(pattern.WeaponGripLocalEuler);
            MaintainPatternWeaponGrip();
            break;
        }
    }

    private void MaintainPatternWeaponGrip()
    {
        if (_patternGripWeapon != null)
        {
            _patternGripWeapon.SetLocalPositionAndRotation(_patternGripTargetPosition, _patternGripTargetRotation);
            return;
        }
        if (_releasedPatternGripWeapon == null || _animator == null) return;
        // The outgoing Rush clip can still evaluate its baked weapon curves during a reaction blend.
        bool outgoing = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash == _releasedPatternGripStateHash;
        bool incoming = _animator.IsInTransition(0) && _animator.GetNextAnimatorStateInfo(0).fullPathHash == _releasedPatternGripStateHash;
        if (outgoing || incoming)
            _releasedPatternGripWeapon.SetLocalPositionAndRotation(_patternGripPosition, _patternGripRotation);
        else _releasedPatternGripWeapon = null;
    }

    private void RestorePatternWeaponGrip()
    {
        if (_patternGripWeapon != null)
        {
            _patternGripWeapon.SetLocalPositionAndRotation(_patternGripPosition, _patternGripRotation);
            _releasedPatternGripWeapon = _patternGripWeapon;
            _releasedPatternGripStateHash = _patternGripStateHash;
        }
        _patternGripWeapon = null;
    }
}
