using UnityEngine;

/// <summary>Clip time controls presentation only; gameplay events and global HitStop remain authoritative.</summary>
public sealed class NormalAttackSpeedState : StateMachineBehaviour
{
    public const string AttackTag = "NormalAttack";
    public const string ParameterPrefix = "AttackPhase_";

    public static bool AcceptGameplayEvent(Animator animator, AnimationEvent animationEvent)
    {
        // Counter and other existing clips keep their receivers and behavior.
        if (animator == null || animationEvent == null || !animationEvent.animatorStateInfo.IsTag(AttackTag)) return true;
        var current = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag(AttackTag) || next.IsTag(PlayerAttacker.JustAvoidCounterTag)) current = next;
        }
        return animationEvent.animatorStateInfo.fullPathHash == current.fullPathHash;
    }
    [SerializeField] private string _speedParameter;
    [SerializeField, Range(.5f, 1.5f)] private float _windupSpeed = .95f;
    [SerializeField, Range(.5f, 1.5f)] private float _slashSpeed = 1.15f;
    [SerializeField, Range(.5f, 1.5f)] private float _recoverySpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float _slashStart;
    [SerializeField, Range(0f, 1f)] private float _slashEnd;
    [SerializeField, Range(0f, 1f)] private float _recoveryStart;

    public float EvaluateSpeed(float normalizedTime)
    {
        if (normalizedTime < _slashStart) return _windupSpeed;
        if (normalizedTime <= _slashEnd) return _slashSpeed;
        float t = Mathf.InverseLerp(_slashEnd, _recoveryStart, normalizedTime);
        return Mathf.Lerp(_slashSpeed, _recoverySpeed, Mathf.SmoothStep(0f, 1f, t));
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer == 0) SetSpeed(animator, 1f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer != 0) return;
        // Preserve the existing blend's time mapping and root extraction. Each state has
        // its own parameter, so an outgoing combo cannot overwrite the incoming speed.
        SetSpeed(animator, animator.IsInTransition(layer) ? 1f : EvaluateSpeed(state.normalizedTime));
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer == 0) SetSpeed(animator, 1f);
    }

    private void SetSpeed(Animator animator, float speed)
    {
        if (!string.IsNullOrEmpty(_speedParameter)) animator.SetFloat(_speedParameter, speed);
    }
}
