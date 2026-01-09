using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private string _moveVelocity = "MoveVelocity";
    private Animator _animator;
    private int _moveVelocityHash;

    /// <summary>
    /// Moveのアニメーション。
    /// </summary>
    /// <param name="speed"></param>
    public void MoveVelocity(float speed)
    {
        _animator?.SetFloat(_moveVelocityHash, speed, 0.1f, Time.deltaTime);
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
    }
}
