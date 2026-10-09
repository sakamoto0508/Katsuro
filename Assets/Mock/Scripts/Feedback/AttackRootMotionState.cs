using UnityEngine;
// Incoming combo states replace ownership before the outgoing state's Exit.
/// <summary>攻撃Stateへの入退場をControllerへ通知し、Root Motionを適用するStateの所有権を管理する。</summary>
public sealed class AttackRootMotionState : StateMachineBehaviour
{
    /// <summary>ベースLayerの攻撃開始StateをPlayerまたはEnemy Controllerへ通知してRoot Motionを受け付ける。</summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (layer != 0) return;
        animator.GetComponent<PlayerController>()?.BeginAnimationAttackRootMotion(info.fullPathHash);
        animator.GetComponent<EnemyController>()?.BeginAnimationAttackRootMotion(info.fullPathHash);
    }
    /// <summary>終了StateのハッシュをControllerへ渡し、所有者に一致する攻撃Root Motionだけを解除する。</summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo info, int layer)
    {
        if (layer != 0) return;
        animator.GetComponent<PlayerController>()?.EndAnimationAttackRootMotion(info.fullPathHash);
        animator.GetComponent<EnemyController>()?.EndAnimationAttackRootMotion(info.fullPathHash);
    }
}
