using UnityEngine;

/// <summary>
/// Animator パラメーター操作を一元化し、移動値や攻撃トリガーを安全に更新するコンポーネント。
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    /// <summary>Player Animatorの移動・攻撃・回避・抜刀/納刀パラメータ名の設定。</summary>
    public AnimationName AnimName => _animName;
    /// <summary>Player Animatorの移動・攻撃・回避・抜刀/納刀パラメータ名の設定。</summary>
    [UnityEngine.Tooltip("Player Animatorの移動・攻撃・回避・抜刀/納刀パラメータ名の設定。")]
    [SerializeField] private AnimationName _animName;
    private Animator _animator;
    private int _moveVelocityHash;
    private int _moveVectorXHash;
    private int _moveVectorYHash;

    /// <summary>
    /// 速度をスムージング付きで Animator に反映する。
    /// </summary>
    public void MoveVelocity(float speed)
    {
        _animator?.SetFloat(_moveVelocityHash, speed, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// 移動ベクトル（X/Z）をスムージング付きで設定する。
    /// </summary>
    public void MoveVector(Vector2 input)
    {
        _animator?.SetFloat(_moveVectorXHash, input.x, 0.1f, Time.deltaTime);
        _animator?.SetFloat(_moveVectorYHash, input.y, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// 指定トリガーを発火する。未設定名は無視する。
    /// </summary>
    public void PlayTrigger(string animationName)
    {
        _animator?.SetTrigger(animationName);
    }

    /// <summary>
    /// 指定 Bool パラメーターを更新する。
    /// </summary>
    public void PlayBool(string animationName, bool value)
    {
        _animator?.SetBool(animationName, value);
    }

    /// <summary>
    /// 整数パラメーター（例: ComboStep）を設定する。空文字は無視。
    /// </summary>
    public void SetInteger(string parameterName, int value)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        _animator?.SetInteger(parameterName, value);
    }

    private bool _initialized;
    /// <summary>Animatorを取得し、移動パラメータのハッシュを一度だけ初期化する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _animator = GetComponent<Animator>();
        // 検証処理が呼ばれていなくても、実行時にパラメーターのハッシュ値を初期化する。
        _moveVelocityHash = Animator.StringToHash(_animName.MoveVelocity);
        _moveVectorXHash = Animator.StringToHash(_animName.MoveVectorX);
        _moveVectorYHash = Animator.StringToHash(_animName.MoveVectorY);
    }

    /// <summary>
    /// インスペクター更新時にハッシュ値を再計算しておく。
    /// </summary>
    private void OnValidate()
    {
        _moveVelocityHash = Animator.StringToHash(_animName.MoveVelocity);
        _moveVectorXHash = Animator.StringToHash(_animName.MoveVectorX);
        _moveVectorYHash = Animator.StringToHash(_animName.MoveVectorY);
    }
}
