using UnityEngine;

public class EnemyMover
{
    public EnemyMover(Rigidbody rb, EnemyStuts enemyStuts)
    {
        _rb = rb;
        _enemyStuts = enemyStuts;
    }

    private Rigidbody _rb;
    private EnemyStuts _enemyStuts;

}
