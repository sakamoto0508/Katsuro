using UnityEngine;

/// <summary>Clip time controls presentation only; gameplay events and global HitStop remain authoritative.</summary>
public sealed class NormalAttackSpeedState : StateMachineBehaviour
{
    public const string AttackTag = "NormalAttack";
    public const string ParameterPrefix = "AttackPhase_";

    /// <summary>通常攻撃のBlend中に古いClipから届くEventを除外する。通常攻撃以外の既存Event受付は維持する。</summary>
    /// <returns>現在または遷移先が所有する通常攻撃Event、または通常攻撃以外ならtrue。</returns>
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
    /// <summary>この攻撃State専用のAnimator速度Floatパラメータ名。他のコンボ段階と共有しない。</summary>
    [UnityEngine.Tooltip("この攻撃State専用のAnimator速度Floatパラメータ名。他のコンボ段階と共有しない。")]
    [SerializeField] private string _speedParameter;
    /// <summary>振りかぶり区間のアニメーション速度倍率。1でClip本来の速度。</summary>
    [UnityEngine.Tooltip("振りかぶり区間のアニメーション速度倍率。1でClip本来の速度。")]
    [SerializeField, Range(.5f, 1.5f)] private float _windupSpeed = .95f;
    /// <summary>斬撃区間のアニメーション速度倍率。1でClip本来の速度。</summary>
    [UnityEngine.Tooltip("斬撃区間のアニメーション速度倍率。1でClip本来の速度。")]
    [SerializeField, Range(.5f, 1.5f)] private float _slashSpeed = 1.15f;
    /// <summary>攻撃後の回復区間のアニメーション速度倍率。1でClip本来の速度。</summary>
    [UnityEngine.Tooltip("攻撃後の回復区間のアニメーション速度倍率。1でClip本来の速度。")]
    [SerializeField, Range(.5f, 1.5f)] private float _recoverySpeed = 1f;
    /// <summary>斬撃速度へ切り替えるClipの正規化時刻（0〜1）。</summary>
    [UnityEngine.Tooltip("斬撃速度へ切り替えるClipの正規化時刻（0〜1）。")]
    [SerializeField, Range(0f, 1f)] private float _slashStart;
    /// <summary>斬撃区間が終わるClipの正規化時刻（0〜1）。ここから回復速度へ補間する。</summary>
    [UnityEngine.Tooltip("斬撃区間が終わるClipの正規化時刻（0〜1）。ここから回復速度へ補間する。")]
    [SerializeField, Range(0f, 1f)] private float _slashEnd;
    /// <summary>回復速度への補間が完了するClipの正規化時刻（0〜1）。</summary>
    [UnityEngine.Tooltip("回復速度への補間が完了するClipの正規化時刻（0〜1）。")]
    [SerializeField, Range(0f, 1f)] private float _recoveryStart;

    /// <summary>Clipの正規化時刻から振りかぶり・斬撃・回復の速度倍率を選び、斬撃後は滑らかに補間する。</summary>
    /// <returns>該当する攻撃段階の速度倍率。</returns>
    public float EvaluateSpeed(float normalizedTime)
    {
        if (normalizedTime < _slashStart) return _windupSpeed;
        if (normalizedTime <= _slashEnd) return _slashSpeed;
        float t = Mathf.InverseLerp(_slashEnd, _recoveryStart, normalizedTime);
        return Mathf.Lerp(_slashSpeed, _recoverySpeed, Mathf.SmoothStep(0f, 1f, t));
    }

    /// <summary>ベースLayerの攻撃Stateに割り当てた速度パラメータを通常倍率で初期化する。</summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer == 0) SetSpeed(animator, 1f);
    }

    /// <summary>Blend中は通常倍率を保ち、単独再生中だけClip時刻に応じた専用速度を設定する。</summary>
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer != 0) return;
        // Preserve the existing blend's time mapping and root extraction. Each state has
        // its own parameter, so an outgoing combo cannot overwrite the incoming speed.
        SetSpeed(animator, animator.IsInTransition(layer) ? 1f : EvaluateSpeed(state.normalizedTime));
    }

    /// <summary>終了したState自身の速度パラメータを戻し、次のコンボ段階のパラメータを保護する。</summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo state, int layer)
    {
        if (layer == 0) SetSpeed(animator, 1f);
    }

    /// <summary>割り当て済みの専用Animatorパラメータだけに速度倍率を反映する。</summary>
    private void SetSpeed(Animator animator, float speed)
    {
        if (!string.IsNullOrEmpty(_speedParameter)) animator.SetFloat(_speedParameter, speed);
    }
}
