using UnityEngine;

[CreateAssetMenu(fileName = "AnimName", menuName = "ScriptableObjects/Player/AnimName")]
public class AnimationName : ScriptableObject
{
    public string IsDrawingSword => _isDrawingSword;
    public string IsLockOn => _isLockOn;

    [SerializeField] string _isDrawingSword = "";
    [SerializeField] string _isLockOn = "";
}
