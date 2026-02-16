using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    public AnimationName AnimName => _animName;
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

    /// <summary>
    /// Animator 参照を取得する。
    /// </summary>
    private void Start()
    {
        _animator = GetComponent<Animator>();
        // Ensure parameter hashes are initialized at runtime in case OnValidate wasn't called in editor.
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

    /// <summary>
    /// アニメーションイベント受け口（Animator がアタッチされた GameObject 上でイベントを呼ぶ場合用）。
    /// EnemyController の同名メソッドへ転送し、ログを出力します。
    /// </summary>
    public void AnimEvent_OnAttackFinished()
    {
        var enemy = GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.AnimEvent_OnAttackFinished();
        }
        else
        {
            Debug.LogWarning("EnemyAnimationController: EnemyController not found in parents to forward AnimEvent_OnAttackFinished");
        }
    }
}
