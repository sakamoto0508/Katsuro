using UnityEngine;

/// <summary>
/// プレイヤーの行動状態を表す抽象基底クラス。
/// 各ステートはこのクラスを継承し、Enter/Exit/Update などのライフサイクルを実装する。
/// </summary>
public abstract class PlayerState
{
    protected PlayerState(PlayerStateContext context, PlayerStateMachine stateMachine)
    {
        Context = context;
        StateMachine = stateMachine;
    }

    /// <summary>状態間で共有される依存情報。</summary>
    protected PlayerStateContext Context { get; }

    /// <summary>状態遷移を制御するステートマシン。</summary>
    protected PlayerStateMachine StateMachine { get; }

    /// <summary>状態の識別子。</summary>
    public abstract PlayerStateId Id { get; }

    /// <summary>状態突入時に呼び出される。</summary>
    public virtual void Enter() { }

    /// <summary>状態離脱時に呼び出される。</summary>
    public virtual void Exit() { }

    /// <summary>フレーム更新時の処理。</summary>
    public virtual void Update(float deltaTime) { }

    /// <summary>物理更新時の処理。</summary>
    public virtual void FixedUpdate(float deltaTime) { }

    /// <summary>移動入力を受け取り、デフォルトでは Mover へ入力値を渡す。</summary>
    public virtual void OnMove(Vector2 input)
    {
        Context?.Mover?.OnMove(input);
    }

    /// <summary>スプリント入力開始時のフック。</summary>
    public virtual void OnSprintStarted() { }

    /// <summary>スプリント入力終了時のフック。</summary>
    public virtual void OnSprintCanceled() { }

    /// <summary>通常攻撃のフック。</summary>
    public virtual void OnLightAttack() { }

    /// <summary>強攻撃のフック。</summary>
    public virtual void OnStrongAttack() { }

    /// <summary>アニメーションのコンボ受付開始イベント。</summary>
    public virtual void OnComboWindowOpened() { }

    /// <summary>アニメーションのコンボ受付終了イベント。</summary>
    public virtual void OnComboWindowClosed() { }

    /// <summary>攻撃アニメーションが完了した際のフック。</summary>
    public virtual void OnAttackAnimationFinished() { }
}