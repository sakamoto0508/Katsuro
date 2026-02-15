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
    public string ComboStep => _comboStep;
    public string BackStep => _backStep;
    public string IsSwordDrawn => _isSwordDrawn;
    public string JustAvoid => _justAvoid;
    public string JustAvoidWindow => _justAvoidWindow;
    public string EnemyDead => _enemyDead;
    public string PlayerDead => _playerDead;
    public string SwordSheathing => _swordSheathing;

    [SerializeField] private string _moveVelocity = "MoveVelocity";
    [SerializeField] private string _moveVectorX = "MoveVectorX";
    [SerializeField] private string _moveVectorY = "MoveVectorY";
    [SerializeField] private string _isDrawingSword = "";
    [SerializeField] private string _isLockOn = "";
    [SerializeField] private string _lightAttack = "";
    [SerializeField] private string _strongAttack = "";
    [SerializeField] private string _justAvoidAttack = "";
    [SerializeField] private string _comboStep = "ComboStep";
    [SerializeField] private string _backStep = "BackStep";
    [SerializeField] private string _isSwordDrawn = "IsSwordDrawn";
    [SerializeField] private string _justAvoid = "JustAvoid";
    [SerializeField] private string _justAvoidWindow = "JustAvoidWindow";
    [SerializeField] private string _enemyDead = "EnemyDead";
    [SerializeField] private string _playerDead = "PlayerDead";
    [SerializeField] private string _swordSheathing = "SwordSheathing";
}
