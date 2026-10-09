using UnityEngine;
// Incoming combo states replace ownership before the outgoing state's Exit.
public sealed class AttackRootMotionState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (layer != 0) return;
        animator.GetComponent<PlayerController>()?.BeginAnimationAttackRootMotion(info.fullPathHash);
        animator.GetComponent<EnemyController>()?.BeginAnimationAttackRootMotion(info.fullPathHash);
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (layer != 0) return;
        animator.GetComponent<PlayerController>()?.EndAnimationAttackRootMotion(info.fullPathHash);
        animator.GetComponent<EnemyController>()?.EndAnimationAttackRootMotion(info.fullPathHash);
    }
}
