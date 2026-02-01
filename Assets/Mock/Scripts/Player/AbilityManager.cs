using UnityEngine;

/// <summary>
/// 段階移行で使用する Ability レイヤの外部クラス（まずは Ghost の移行のみ）。
/// `PlayerStateContext` を受け取り、ゴーストの開始/継続/終了を管理します。
/// </summary>
public sealed class AbilityManager
{
    public enum GhostToggleResult
    {
        Began,
        Ended,
        Failed,
    }

    private readonly PlayerStateContext _context;
    private readonly PlayerGhost _ghost;
    private readonly PlayerSelfSacrifice _selfSacrifice;
    private readonly PlayerHeal _healer;
    private readonly PlayerResource _playerResource;
    private readonly PlayerStateConfig _stateConfig;

    private float _justAvoidRemaining = 0f;

    public AbilityManager(PlayerStateContext context, PlayerGhost ghost, PlayerSelfSacrifice selfSacrifice, PlayerHeal healer, PlayerResource playerResource, PlayerStateConfig stateConfig)
    {
        _context = context;
        _ghost = ghost;
        _selfSacrifice = selfSacrifice;
        _healer = healer;
        _playerResource = playerResource;
        _stateConfig = stateConfig;
    }

    /// <summary>毎フレーム呼ぶ Tick。ゴーストの継続消費とジャスト回避ウィンドウの管理を行う。</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // 継続処理を直接各 Ability に委譲
        _ghost?.Tick(deltaTime);
        _selfSacrifice?.Tick(deltaTime);
        _healer?.Tick(deltaTime);

        // ジャスト回避ウィンドウの経過管理
        if (_justAvoidRemaining > 0f)
        {
            _justAvoidRemaining -= deltaTime;
            if (_justAvoidRemaining <= 0f)
            {
                _justAvoidRemaining = 0f;
                if (_context.IsInJustAvoidWindow)
                    _context.SetJustAvoidWindow(false);
            }
        }

        // ゴーストが自動終了（ゲージ枯渇など）していたらフラグを解除
        if (!(_ghost?.IsGhosting ?? false))
        {
            if (_context.IsGhostMode)
            {
                _context.IsGhostMode = false;
                if (_context.IsInJustAvoidWindow)
                {
                    _context.SetJustAvoidWindow(false);
                    _justAvoidRemaining = 0f;
                }
            }
        }
    }

    /// <summary>ゴーストのトグル（開始 / 終了）を行う。結果を返す。</summary>
    public GhostToggleResult ToggleGhost()
    {
        if (_ghost == null) return GhostToggleResult.Failed;

        if (_ghost.IsActive)
        {
            _ghost.End();
            _context.IsGhostMode = false;
            if (_context.IsInJustAvoidWindow)
            {
                _context.SetJustAvoidWindow(false);
                _justAvoidRemaining = 0f;
            }
            return GhostToggleResult.Ended;
        }

        if (_ghost.TryBegin())
        {
            _context.IsGhostMode = true;
            _context.SetJustAvoidWindow(true);
            _justAvoidRemaining = _stateConfig?.JustAvoidTime ?? 0f;
            return GhostToggleResult.Began;
        }

        return GhostToggleResult.Failed;
    }

    /// <summary>
    /// SelfSacrifice のトグル（開始 / 終了）。開始は現在HP比で許可されるかを確認してから行う。
    /// </summary>
    public bool ToggleSelfSacrifice()
    {
        var s = _selfSacrifice;
        if (s == null) return false;

        if (s.IsActive)
        {
            s.End();
            return false;
        }

        float currentHpRatio = _playerResource != null ? _playerResource.CurrentHpRatio : 1f;
        if (s.CanBegin(currentHpRatio))
        {
            s.Begin();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Heal のトグル（開始 / 終了）。開始時はデフォルトの回復速度を使用します。
    /// </summary>
    public bool ToggleHeal()
    {
        var h = _healer;
        if (h == null) return false;

        if (h.IsHealing)
        {
            h.End();
            return false;
        }

        // デフォルト回復速度（%/s）を PlayerHealState と合わせる
        float defaultPercentPerSecond = 5f;
        return h.TryBegin(defaultPercentPerSecond);
    }
}

