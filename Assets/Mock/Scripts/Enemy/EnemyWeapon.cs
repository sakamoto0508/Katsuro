using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [SerializeField] private EnemyStuts _enemyStuts;

    public float Damage()
    {
        return _enemyStuts.EnemyPower;
    }
}
