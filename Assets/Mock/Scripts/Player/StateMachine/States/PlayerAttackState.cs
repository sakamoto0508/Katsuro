using UnityEngine;

/// <summary>
/// 攻撃系ステートの共通挙動をまとめた抽象クラス。
/// コンボ入力の受付や攻撃アニメーションの再トリガーを管理する。
/// </summary>
public abstract class PlayerAttackState : PlayerState
{
    protected PlayerAttackState(PlayerStateContext context, PlayerStateMachine stateMachine, float attackDuration)
        : base(context, stateMachine)
    {
        _fallbackAttackDuration = Mathf.Max(0.1f, attackDuration);
    }

    /// <summary>ステップ情報が無いときに使うフォールバック継続秒数。</summary>
    private readonly float _fallbackAttackDuration;

    /// <summary>現在の攻撃開始からの経過時間。</summary>
    private float _elapsedTime;

    /// <summary>次段コンボが入力済みか。</summary>
    private bool _comboQueued;

    /// <summary>アニメーション側でコンボ受付が開いているか。</summary>
    private bool _comboWindowOpen;

    /// <summary>現在のコンボ段（0 起点）。</summary>
    private int _comboStepIndex;

    /// <summary>現行攻撃の継続秒数。</summary>
    private float _currentAttackDuration;

    /// <summary>コンボ入力を消費できる最短時刻（受付ディレイを吸収する）。</summary>
    private float _comboConsumeUnlockTime;

    /// <summary>最大コンボ段数。派生クラスでクリップ数に応じて上書きする。</summary>
    protected virtual int MaxComboSteps => 1;

    /// <summary>段数に応じた攻撃継続秒数を返す。未設定ならフォールバックを使用。</summary>
    protected virtual float ResolveAttackDuration(int comboStep) => _fallbackAttackDuration;

    /// <summary>段数ごとのコンボ受付遅延（秒）。既定では遅延なし。</summary>
    protected virtual float ResolveComboWindowDelay(int comboStep) => 0f;

    /// <summary>
    /// 攻撃開始時に段数とタイマーを初期化し、1 段目を再生する。
    /// </summary>
    public override void Enter()
    {
        base.Enter();

        Context?.Mover?.MoveStop();
        if (Context.Attacker == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        Context.Mover?.SetSprint(false);
        _comboStepIndex = 0;
        _comboQueued = false;
        _comboWindowOpen = false;
        _comboConsumeUnlockTime = float.PositiveInfinity;
        BeginCurrentAttack();
    }

    /// <summary>
    /// ステート離脱時にヒットボックス等をクリーンアップする。
    /// </summary>
    public override void Exit()
    {
        base.Exit();
        Context.Attacker?.EndAttack();
    }

    /// <summary>
    /// タイムアウト判定とモーション更新を実行する。
    /// </summary>
    public override void Update(float deltaTime)
    {
        Context.Mover?.Update();
        _elapsedTime += deltaTime;

        if (_elapsedTime >= _currentAttackDuration && !_comboWindowOpen && !_comboQueued)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>
    /// 攻撃中でも移動物理を更新し続ける。
    /// </summary>
    public override void FixedUpdate(float deltaTime)
    {
        //Context.Mover?.FixedUpdate();
    }

    /// <summary>ライト攻撃入力を受けたら次段予約する。</summary>
    public override void OnLightAttack() => QueueComboRequest();

    /// <summary>強攻撃入力でも同様に予約する。</summary>
    public override void OnStrongAttack() => QueueComboRequest();

    /// <summary>アニメーションイベントで受付開始が通知されたら、遅延解除時刻をセットしてから消費を試みる。</summary>
    public override void OnComboWindowOpened()
    {
        _comboWindowOpen = true;
        _comboConsumeUnlockTime = _elapsedTime + Mathf.Max(0f, ResolveComboWindowDelay(_comboStepIndex));
        TryConsumeComboRequest();
    }

    /// <summary>アニメーションイベントで受付終了を通知されたときの処理。</summary>
    public override void OnComboWindowClosed()
    {
        _comboWindowOpen = false;
    }

    /// <summary>
    /// 攻撃アニメーションが終わった際、予約があれば次段へ、無ければ終了。
    /// </summary>
    public override void OnAttackAnimationFinished()
    {
        if (!TryConsumeComboRequest())
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>comboStep に応じて具体的な攻撃アニメーションを再生する。</summary>
    protected abstract void TriggerAttack(int comboStep);

    /// <summary>現在の段の攻撃を開始し、タイマーと受付状態をリセットする。</summary>
    private void BeginCurrentAttack()
    {
        _elapsedTime = 0f;
        _comboWindowOpen = false;
        _comboConsumeUnlockTime = float.PositiveInfinity;
        _currentAttackDuration = Mathf.Max(0.1f, ResolveAttackDuration(_comboStepIndex));
        TriggerAttack(_comboStepIndex);
    }

    /// <summary>次段予約が可能ならキューへ登録し、即消費できるか判定する。</summary>
    private void QueueComboRequest()
    {
        if (!CanQueueNextCombo())
        {
            return;
        }

        _comboQueued = true;
        TryConsumeComboRequest();
    }

    /// <summary>
    /// 受付中かつ予約済みで、遅延解除時刻を過ぎていれば次段を開始する。
    /// </summary>
    private bool TryConsumeComboRequest()
    {
        if (!_comboQueued || !_comboWindowOpen || !CanQueueNextCombo() 
            || _elapsedTime < _comboConsumeUnlockTime)
        {
            return false;
        }

        _comboQueued = false;
        _comboWindowOpen = false;
        _comboStepIndex = Mathf.Min(_comboStepIndex + 1, MaxComboSteps - 1);
        BeginCurrentAttack();
        return true;
    }

    /// <summary>次段に進める余地があるか判定する。</summary>
    private bool CanQueueNextCombo() => _comboStepIndex + 1 < MaxComboSteps;
}