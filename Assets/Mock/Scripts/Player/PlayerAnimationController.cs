using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private AnimationName _animName;
    private Animator _animator;
    private int _moveVelocityHash;
    private int _moveVectorXHash;
    private int _moveVectorYHash;

    /// <summary>
    /// Moveのアニメーション。
    /// </summary>
    /// <param name="speed"></param>
    public void MoveVelocity(float speed)
    {
        _animator?.SetFloat(_moveVelocityHash, speed, 0.1f, Time.deltaTime);
    }

    public void MoveVector(Vector2 input)
    {
        _animator?.SetFloat(_moveVectorXHash, input.x, 0.1f, Time.deltaTime);
        _animator?.SetFloat(_moveVectorYHash, input.y, 0.1f, Time.deltaTime);
    }

    public void PlayTrriger(string animationName)
    {
        _animator?.SetTrigger(animationName);
    }

    public void PlayBool(string animationName, bool value)
    {
        _animator?.SetBool(animationName, value);
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnValidate()
    {
        _moveVelocityHash = Animator.StringToHash(_animName.MoveVelocity);
        _moveVectorXHash = Animator.StringToHash(_animName.MoveVectorX);
        _moveVectorYHash = Animator.StringToHash(_animName.MoveVectorY);
    }
}
