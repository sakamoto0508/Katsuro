using UnityEngine;

/// <summary>
/// 攻撃系ステートの共通挙動をまとめた抽象クラス。
/// コンボ入力の受付や攻撃アニメーションの再トリガーを管理する。
/// </summary>
public abstract class PlayerAttackState : PlayerState
{
    /// <summary>単発攻撃として想定される継続時間（保険用タイムアウト）。</summary>
    private readonly float _attackDuration;

    /// <summary>状態遷移後に経過した時間。</summary>
    private float _elapsedTime;

    /// <summary>コンボ入力がバッファリングされているか。</summary>
    private bool _comboQueued;

    /// <summary>現在コンボ受付ウィンドウが開いているか。</summary>
    private bool _comboWindowOpen;

    protected PlayerAttackState(PlayerStateContext context, PlayerStateMachine stateMachine, float attackDuration)
        : base(context, stateMachine)
    {
        _attackDuration = Mathf.Max(0.1f, attackDuration);
    }

    /// <summary>
    /// ステート突入時に攻撃を開始し、タイマー／フラグを初期化する。
    /// </summary>
    public override void Enter()
    {
        base.Enter();

        if (Context.Attacker == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        Context.Mover?.SetSprint(false);
        _elapsedTime = 0f;
        _comboQueued = false;
        _comboWindowOpen = false;
        TriggerAttack();
    }

    /// <summary>
    /// 攻撃終了時にヒットボックスを閉じる。
    /// </summary>
    public override void Exit()
    {
        base.Exit();
        Context.Attacker?.EndAttack();
    }

    /// <summary>
    /// コンボ入力が無く、タイムアウトした場合のみロコモーションへ戻す。
    /// </summary>
    public override void Update(float deltaTime)
    {
        Context.Mover?.Update();
        _elapsedTime += deltaTime;

        if (_elapsedTime >= _attackDuration && !_comboWindowOpen && !_comboQueued)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>
    /// 攻撃中も移動物理処理を継続する。
    /// </summary>
    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover?.FixedUpdate();
    }

    /// <summary>ライト攻撃入力を受けたらコンボ予約を行う。</summary>
    public override void OnLightAttack() => QueueComboRequest();

    /// <summary>強攻撃入力でも同様にコンボ予約する。</summary>
    public override void OnStrongAttack() => QueueComboRequest();

    /// <summary>アニメーションイベント経由でコンボ受付を開始。</summary>
    public override void OnComboWindowOpened()
    {
        _comboWindowOpen = true;
        TryConsumeComboRequest();
    }

    /// <summary>コンボ受付終了イベント。</summary>
    public override void OnComboWindowClosed()
    {
        _comboWindowOpen = false;
    }

    /// <summary>
    /// 攻撃アニメ完了時、予約があれば次の攻撃を即時再生し、
    /// 無ければロコモーションへ戻す。
    /// </summary>
    public override void OnAttackAnimationFinished()
    {
        if (!TryConsumeComboRequest())
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>コンボ入力をバッファし、可能ならその場で消費する。</summary>
    private void QueueComboRequest()
    {
        _comboQueued = true;
        TryConsumeComboRequest();
    }

    /// <summary>
    /// コンボ受付中かつ予約済みなら、攻撃アニメを再トリガーする。
    /// </summary>
    private bool TryConsumeComboRequest()
    {
        if (!_comboQueued || !_comboWindowOpen)
        {
            return false;
        }

        _comboQueued = false;
        _comboWindowOpen = false;
        _elapsedTime = 0f;
        TriggerAttack();
        return true;
    }

    /// <summary>具体的な攻撃アニメーションを再生する具象クラス実装。</summary>
    protected abstract void TriggerAttack();
}