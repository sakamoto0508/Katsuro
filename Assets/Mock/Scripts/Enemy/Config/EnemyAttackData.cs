using UnityEngine;

/// <summary>Enemy行動ごとのAnimator名、攻撃力・武器などをInspectorで定義する。</summary>
[CreateAssetMenu(menuName = "Enemy/AttackData", fileName = "EnemyAttackData")]
public class EnemyAttackData : ScriptableObject
{
    /// <summary>敵攻撃の強弱を明示する分類。Lightは軽攻撃、Heavyは強攻撃。</summary>
    public enum AttackStrength { Light = 0, Heavy = 1 }

    /// <summary>攻撃の強弱。Animator名やActionTypeから推測しない。</summary>
    [SerializeField, Tooltip("攻撃の強弱。Animator名やActionTypeから推測しない。")]
    private AttackStrength _strength;
    /// <summary>設定された敵攻撃の強弱を取得する。</summary>
    public AttackStrength Strength => _strength;
    /// <summary>この攻撃が明示的にHeavyとして設定されているか。</summary>
    public bool IsHeavy => _strength == AttackStrength.Heavy;
    /// <summary>この攻撃設定を使うEnemy行動の種類。</summary>
    public EnemyActionType ActionType => _actionType;
    /// <summary>この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。</summary>
    public string AnimatorTrigger => _animatorTrigger;
    /// <summary>この行動の基準ダメージ。攻撃開始時に武器へ渡す。</summary>
    public float Damage => _damage;
    /// <summary>未使用の想定射程（m）。Warpの出現距離はEnemyAttackPattern.TeleportDistanceで調整する。</summary>
    public float Range => _range;
    /// <summary>EnemyAttackerへ渡された武器グループの番号。現行Enemyは全刀Colliderをまとめた0番を使う。</summary>
    public int HitboxIndex => _hitboxIndex;
    /// <summary>Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    public float HitboxEnableDelay => _hitboxEnableDelay;
    /// <summary>Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    public float HitboxDisableDelay => _hitboxDisableDelay;

    /// <summary>この攻撃設定を使うEnemy行動の種類。</summary>
    [UnityEngine.Tooltip("この攻撃設定を使うEnemy行動の種類。")]
    [SerializeField] private EnemyActionType _actionType;
    /// <summary>この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。</summary>
    [UnityEngine.Tooltip("この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。")]
    [SerializeField] private string _animatorTrigger;
    /// <summary>この行動の基準ダメージ。攻撃開始時に武器へ渡す。</summary>
    [UnityEngine.Tooltip("この行動の基準ダメージ。攻撃開始時に武器へ渡す。")]
    [SerializeField] private float _damage;
    /// <summary>行動の想定射程（Unity単位）。現在の攻撃実行処理では直接参照しない設定。</summary>
    [UnityEngine.Tooltip("互換用の想定射程（m）。現在の攻撃実行処理では未使用。Warpの出現距離はEnemyAttackPattern.TeleportDistance、発動する距離帯はEnemyDecisionConfigで調整する。")]
    [SerializeField] private float _range;
    /// <summary>EnemyAttackerへ渡された武器グループの番号。</summary>
    [UnityEngine.Tooltip("武器グループ番号。現在のEnemyでは全刀Colliderをまとめた0を指定。Collider配列の番号ではない。")]
    [SerializeField] private int _hitboxIndex;
    /// <summary>Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    [UnityEngine.Tooltip("Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。")]
    [SerializeField] private float _hitboxEnableDelay;
    /// <summary>Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    [UnityEngine.Tooltip("Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。")]
    [SerializeField] private float _hitboxDisableDelay;
}
