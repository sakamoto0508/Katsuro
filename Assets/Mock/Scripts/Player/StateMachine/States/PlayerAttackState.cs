using UnityEngine;

public abstract class PlayerAttackState : PlayerState
{
    private readonly float _attackDuration;
    private float _elapsedTime;
    private bool _comboQueued;
    private bool _comboWindowOpen;

    protected PlayerAttackState(PlayerStateContext context, PlayerStateMachine stateMachine, float attackDuration)
        : base(context, stateMachine)
    {
        _attackDuration = Mathf.Max(0.1f, attackDuration);
    }

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

    public override void Exit()
    {
        base.Exit();
        Context.Attacker?.EndAttack();
    }

    public override void Update(float deltaTime)
    {
        Context.Mover?.Update();
        _elapsedTime += deltaTime;

        if (_elapsedTime >= _attackDuration && !_comboWindowOpen && !_comboQueued)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover?.FixedUpdate();
    }

    public override void OnLightAttack() => QueueComboRequest();

    public override void OnStrongAttack() => QueueComboRequest();

    public override void OnComboWindowOpened()
    {
        _comboWindowOpen = true;
        TryConsumeComboRequest();
    }

    public override void OnComboWindowClosed()
    {
        _comboWindowOpen = false;
    }

    public override void OnAttackAnimationFinished()
    {
        if (!TryConsumeComboRequest())
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    private void QueueComboRequest()
    {
        _comboQueued = true;
        TryConsumeComboRequest();
    }

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

    protected abstract void TriggerAttack();
}