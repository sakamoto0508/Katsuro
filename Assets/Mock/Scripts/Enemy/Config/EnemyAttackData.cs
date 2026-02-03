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
    /// <summary>
    /// アニメ内で使うバリアント（例: 同じトリガーで複数の攻撃バリエーションを切り分けるための整数）。
    /// Animator の整数パラメーター（例: "ComboStep"）にセットされます。
    /// </summary>
    public int variant = 0;
}
