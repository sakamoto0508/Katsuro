using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Status")]
    [SerializeField] private EnemyStuts _enemyStuts;

    [Header("Weapon")]
    [SerializeField] private Collider[] _enemyWeaponColliders;
    [SerializeField] private Collider[] _playerWeaponColliders;


    public void Init()
    {
        var rb = GetComponent<Rigidbody>();

        var mover = new EnemyMover(rb,_enemyStuts);
    }
}
