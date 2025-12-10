using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator _animator;
    private string _moveVelocity = "MoveVelocity";
    private int _moveVelocityHash;

    /// <summary>
    /// Moveのアニメーション。
    /// </summary>
    /// <param name="speed"></param>
    public void MoveVelocity(float speed)
    {
        _animator?.SetFloat(_moveVelocityHash, speed, 0.1f, Time.deltaTime);
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnValidate()
    {
        _moveVelocityHash = Animator.StringToHash(_moveVelocity);
    }
}
