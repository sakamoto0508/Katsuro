using System;
using UniRx;

/// <summary>
/// プレイヤーステート間で共有する依存オブジェクトや状態をまとめたコンテキスト。
/// 各ステートはこのコンテキスト経由でコントローラーや移動系コンポーネントへアクセスする。
/// </summary>
public sealed class PlayerStateContext
{
    public PlayerStateContext(PlayerController controller, SkillGauge skillGauge, PlayerStatus status
        , PlayerMover mover, PlayerSprint sprint, PlayerGhost playerGhost, PlayerSelfSacrifice selfSacrifice
        , PlayerHeal healer, LockOnCamera lockOnCamera, PlayerStateConfig stateConfig, PlayerAttacker attacker
        , IAnimationEventStream animationEvents)
    {
        Controller = controller;
        SkillGauge = skillGauge;
        Status = status;
        Mover = mover;
        Sprint = sprint;
        Ghost = playerGhost;
        SelfSacrifice = selfSacrifice;
        Healer = healer;
        LockOnCamera = lockOnCamera;
        StateConfig = stateConfig;
        Attacker = attacker;
        AnimationEvents = animationEvents;
    }

    /// <summary>プレイヤー本体のコントローラー。</summary>
    public PlayerController Controller { get; }

    /// <summary>SkillGauge（実行時のゲージ）。UI や Ability が直接購読可能にするため Context に載せる。</summary>
    public SkillGauge SkillGauge { get; }

    /// <summary>プレイヤーのステータス（移動速度や加速度など）。</summary>
    public PlayerStatus Status { get; }

    /// <summary>移動・回転計算を担うコンポーネント。</summary>
    public PlayerMover Mover { get; }

    /// <summary>スキルゲージを消費・管理するスプリント制御。</summary>
    public PlayerSprint Sprint { get; }

    /// <summary>ゴースト（幽霊化）を管理するコンポーネント。</summary>
    public PlayerGhost Ghost { get; }

    /// <summary>自傷（Self Sacrifice）を管理するコンポーネント。</summary>
    public PlayerSelfSacrifice SelfSacrifice { get; }

    /// <summary>回復（Heal）を管理するコンポーネント。</summary>
    public PlayerHeal Healer { get; }

    /// <summary>ロックオン状態の参照先。</summary>
    public LockOnCamera LockOnCamera { get; }

    /// <summary>現在のステートに設定されたプレイヤーの設定。</summary>
    public PlayerStateConfig StateConfig { get; }

    /// <summary>プレイヤーの攻撃関連の操作を行うコンポーネント。</summary>
    public PlayerAttacker Attacker { get; }

    /// <summary>アニメーションイベントの配信ストリーム。</summary>
    public IAnimationEventStream AnimationEvents { get; }

    /// <summary>ゴーストモードかどうか。</summary>
    public bool IsGhostMode { get; set; }

    /// <summary>現在ロックオン中かどうかを示すショートカット。</summary>
    public bool IsLockOn => LockOnCamera != null && LockOnCamera.IsLockOn;

    /// <summary>Ability のいずれかがアクティブかを返すユーティリティ。</summary>
    public bool AnyAbilityActive =>
            (Sprint?.IsActive ?? false)
         || (Ghost?.IsActive ?? false)
         || (SelfSacrifice?.IsActive ?? false)
         || (Healer?.IsActive ?? false);

    /// <summary>
    /// 回避（ジャスト回避）無敵ウィンドウ中かを通知するリアクティブプロパティ。
    /// </summary>
    public IReadOnlyReactiveProperty<bool> IsInJustAvoidWindowReactive => _isInJustAvoidWindow;

    /// <summary>
    /// 現在ジャスト回避の無敵ウィンドウ中か（即時参照用）。
    /// </summary>
    public bool IsInJustAvoidWindow => _isInJustAvoidWindow.Value;

    /// <summary>
    /// プレイヤーに付与されたジャスト回避スタック数（UI などは Reactive を購読してください）。
    /// </summary>
    public IReadOnlyReactiveProperty<int> JustAvoidStacksReactive => _justAvoidStacks;

    /// <summary>
    /// 現在のジャスト回避スタック数（即時参照用）。
    /// </summary>
    public int JustAvoidStacks => _justAvoidStacks.Value;

    /// <summary>
    /// 回避ウィンドウを設定します（ステートが開始・終了で呼びます）。
    /// </summary>
    public void SetJustAvoidWindow(bool enabled) => _isInJustAvoidWindow.Value = enabled;

    /// <summary>
    /// ジャスト回避スタックを加算します（amount は正の値）。UI は <see cref="JustAvoidStacksReactive"/> を購読してください。
    /// </summary>
    public void AddJustAvoidStack(int amount = 1) => _justAvoidStacks.Value = Math.Max(0, _justAvoidStacks.Value + amount);

    /// <summary>
    /// ジャスト回避スタックをクリアします。
    /// </summary>
    public void ClearJustAvoidStacks() => _justAvoidStacks.Value = 0;

    private readonly ReactiveProperty<bool> _isInJustAvoidWindow = new UniRx.ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> _justAvoidStacks=new ReactiveProperty<int>(0);

}