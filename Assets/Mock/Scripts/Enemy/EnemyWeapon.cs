using UnityEngine;

/// <summary>
/// 敵の武器コンポーネント：Collider を isTrigger=true に固定し、
/// 攻撃時のみ有効化（enabled=true）／終了時に無効化する API を提供します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyWeapon : MonoBehaviour
{
    [SerializeField] private EnemyStuts _enemyStuts;
    private Collider _collider;

    /// <summary>攻撃開始：ヒット判定を有効にする。</summary>
    public void StartAttack()
    {
        if (_collider != null)
            _collider.enabled = true;
    }

    /// <summary>攻撃終了：ヒット判定を無効にする。</summary>
    public void EndAttack()
    {
        if (_collider != null)
            _collider.enabled = false;
    }

    /// <summary>この武器のダメージ量（ステータス参照）。</summary>
    public float Damage()
    {
        return _enemyStuts != null ? _enemyStuts.EnemyPower : 0f;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            _collider.isTrigger = true; // 常にトリガーとして扱う
            _collider.enabled = false;  // 通常は無効（攻撃時に有効化）
        }
    }
}
