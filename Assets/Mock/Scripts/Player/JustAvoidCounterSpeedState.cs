using UnityEngine;

/// <summary>タグ付き追撃Stateの入退場だけを通知する。</summary>
public sealed class JustAvoidCounterSpeedState : StateMachineBehaviour
{
    /// <summary>ベースLayerの追撃State開始を通知し、そのStateに速度制御の所有権を渡す。</summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (layerIndex == 0 && stateInfo.IsTag(PlayerAttacker.JustAvoidCounterTag))
            animator.GetComponent<JustAvoidCounterAnimation>()?.BeginCounter(stateInfo.fullPathHash);
    }

    /// <summary>ベースLayerの終了Stateを通知し、所有者に一致する追撃速度だけを解除する。</summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (layerIndex == 0) animator.GetComponent<JustAvoidCounterAnimation>()?.ExitCounter(stateInfo.fullPathHash);
    }
}
