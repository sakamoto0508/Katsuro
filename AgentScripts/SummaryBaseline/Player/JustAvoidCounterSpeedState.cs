using UnityEngine;

/// <summary>タグ付き追撃Stateの入退場だけを通知する。</summary>
public sealed class JustAvoidCounterSpeedState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (layerIndex == 0 && stateInfo.IsTag(PlayerAttacker.JustAvoidCounterTag))
            animator.GetComponent<JustAvoidCounterAnimation>()?.BeginCounter(stateInfo.fullPathHash);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (layerIndex == 0) animator.GetComponent<JustAvoidCounterAnimation>()?.ExitCounter(stateInfo.fullPathHash);
    }
}
