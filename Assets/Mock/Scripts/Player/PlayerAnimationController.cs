using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private string _moveVelocity = "MoveVelocity";
    [SerializeField] private string _moveX = "MoveVectorX";
    [SerializeField] private string _moveY = "MoveVectorY";
    private Animator _animator;
    private int _moveVelocityHash;
    private int _moveXHash;
    private int _moveYHash;

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
        _animator?.SetFloat(_moveXHash, input.x, 0.1f, Time.deltaTime);
        _animator?.SetFloat(_moveYHash, input.y, 0.1f, Time.deltaTime);
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
        _moveVelocityHash = Animator.StringToHash(_moveVelocity);
        _moveXHash = Animator.StringToHash(_moveX);
        _moveYHash = Animator.StringToHash(_moveY);
    }
}
