using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/AttackData", fileName = "EnemyAttackData")]
public class EnemyAttackData : ScriptableObject
{
    public EnemyActionType ActionType => _actionType;
    public string AnimatorTrigger => _animatorTrigger;
    public float Damage => _damage;
    public float Range => _range;
    public int HitboxIndex => _hitboxIndex;
    public float HitboxEnableDelay => _hitboxEnableDelay;
    public float HitboxDisableDelay => _hitboxDisableDelay;

    [SerializeField] private EnemyActionType _actionType;
    [SerializeField] private string _animatorTrigger;
    [SerializeField] private float _damage;
    [SerializeField] private float _range;
    [SerializeField] private int _hitboxIndex;
    [SerializeField] private float _hitboxEnableDelay;
    [SerializeField] private float _hitboxDisableDelay;
}
