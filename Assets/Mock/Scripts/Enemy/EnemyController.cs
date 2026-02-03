using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Enemy Status")]
    [SerializeField] private EnemyStuts _enemyStuts;

    [Header("Tags")]
    [SerializeField] private string _playerWeaponTag = "PlayerWeapon";

    [Header("Weapon")]
    [SerializeField] private Collider[] _enemyWeaponColliders;
    [SerializeField] private Collider[] _playerWeaponColliders;

    [Header("Attack")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyAttackData[] _attackData;

    private EnemyHealth _health;
    private EnemyAttacker _attacker;

    /// <summary>
    /// 初期化処理：必要なランタイムコンポーネントを生成して接続します。
    /// </summary>
    public void Init()
    {
        var rb = GetComponent<Rigidbody>();
        //クラスの初期化
        var mover = new EnemyMover(rb, _enemyStuts);
        _health = new EnemyHealth(_enemyStuts);
        var fallback = _enemyStuts != null ? _enemyStuts.EnemyPower : 0f;
        var wrapper = new EnemyWeapon(_enemyWeaponColliders, fallback);
        _attacker = new EnemyAttacker(_animator, _attackData, new EnemyWeapon[] { wrapper }, _enemyStuts, this.transform);
    }

    /// <summary>
    /// 公開: ダメージを適用するエントリポイント。DamageInfo を受け取り HP を減算します。
    /// </summary>
    // IDamageable インターフェース実装（正確なシグネチャ）
    public void ApplyDamage(DamageInfo info)
    {
        ApplyDamage(info, false);
    }

    /// <summary>
    /// 公開: ダメージを適用するエントリポイント（拡張）。クリティカルフラグなどの追加引数を受けます。
    /// </summary>
    public void ApplyDamage(DamageInfo info, bool isCritical = false)
    {
        if (_health == null)
        {
            // 予防: Health が未割当ての場合は生成してから適用する
            _health = new EnemyHealth(_enemyStuts);
        }

        _health.ApplyDamage(info.DamageAmount);

        Debug.Log($"Enemy took {info.DamageAmount} damage, currentHp={_health.CurrentHp}");

        if (_health.CurrentHp <= 0f)
        {
            Debug.Log("Enemy dead");
            // とりあえずオブジェクトを破棄する。必要に応じて死亡処理を拡張してください。
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        _attacker?.Dispose();
        _health?.Dispose();
    }

    // ---------- Animation Events (Enemy) ----------
    /// <summary>
    /// アニメーションイベント用: ヒットボックスを有効化する（攻撃有効フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        _attacker?.EnableWeaponHitbox();
    }

    /// <summary>
    /// アニメーションイベント用: ヒットボックスを無効化する（攻撃終了フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_DisableWeaponHitbox()
    {
        _attacker?.DisableWeaponHitbox();
    }

    /// <summary>
    /// アニメーションイベント用: アニメから攻撃種別を指定して PerformAttack を呼ぶ（optional）。
    /// アニメ上で攻撃トリガーを開始したい場合に使用します。
    /// </summary>
    /// <param name="attackIndex">_attackData の配列インデックス</param>
    public void AnimEvent_PerformAttack(int attackIndex)
    {
        if (_attackData == null || attackIndex < 0 || attackIndex >= _attackData.Length)
        {
            Debug.LogWarning("AnimEvent_PerformAttack: invalid attackIndex");
            return;
        }
        var data = _attackData[attackIndex];
        if (data == null) return;
        _attacker?.PerformAttack(data.actionType);
    }
}
