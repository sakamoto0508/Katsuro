using UnityEngine;

[CreateAssetMenu(fileName = "AnimName", menuName = "ScriptableObjects/Player/AnimName")]
public class AnimationName : ScriptableObject
{
    public string MoveVelocity => _moveVelocity;
    public string MoveVectorX => _moveVectorX;
    public string MoveVectorY => _moveVectorY;
    public string IsDrawingSword => _isDrawingSword;
    public string IsLockOn => _isLockOn;
    public string LightAttack => _lightAttack;
    public string StrongAttack => _strongAttack;
    public string JustAvoidAttack => _justAvoidAttack;

    [SerializeField] private string _moveVelocity = "MoveVelocity";
    [SerializeField] private string _moveVectorX = "MoveVectorX";
    [SerializeField] private string _moveVectorY = "MoveVectorY";
    [SerializeField] private string _isDrawingSword = "";
    [SerializeField] private string _isLockOn = "";
    [SerializeField] private string _lightAttack = "";
    [SerializeField] private string _strongAttack = "";
    [SerializeField] private string _justAvoidAttack = "";
}
