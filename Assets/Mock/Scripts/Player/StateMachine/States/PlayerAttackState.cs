using UnityEngine;

public abstract class PlayerAttackState : PlayerState
{
    private readonly float _attackDuration;
    private float _elapsedTime;

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

        if (_elapsedTime >= _attackDuration)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover?.FixedUpdate();
    }

    protected abstract void TriggerAttack();
}