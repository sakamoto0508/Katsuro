using Unity.Cinemachine;
using UnityEngine;

public class LockOnTargetGroup : CinemachineTargetGroup
{
    [NoSaveDuringPlay]
    [SerializeField]] private Transform _palyerTarget;
    [NoSaveDuringPlay]
    [SerializeField] private Transform _targetTarget;
}