using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/AttackData", fileName = "EnemyAttackData")]
public class EnemyAttackData : ScriptableObject
{
    public EnemyActionType actionType;
    public string animatorTrigger;
    public float damage = 10f;
    public float range = 1.5f;
    public int hitboxIndex = 0; 
    public float hitboxEnableDelay = 0f; 
    public float hitboxDisableDelay = 0.5f; 
}
