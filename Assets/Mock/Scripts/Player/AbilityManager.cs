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
    private float _justAvoidRemaining = 0f;

    public AbilityManager(PlayerStateContext context)
    {
        _context = context;
    }

    /// <summary>毎フレーム呼ぶ Tick。ゴーストの継続消費とジャスト回避ウィンドウの管理を行う。</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // Ghost の継続消費を行う
        _context?.Ghost?.Tick(deltaTime);

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
        if (!(_context?.Ghost?.IsGhosting ?? false))
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
        if (_context?.Ghost == null) return GhostToggleResult.Failed;

        if (_context.Ghost.IsActive)
        {
            // アクティブなら終了
            _context.Ghost.End();
            _context.IsGhostMode = false;
            if (_context.IsInJustAvoidWindow)
            {
                _context.SetJustAvoidWindow(false);
                _justAvoidRemaining = 0f;
            }
            return GhostToggleResult.Ended;
        }

        // 起動を試みる
        if (_context.Ghost.TryBegin())
        {
            _context.IsGhostMode = true;
            // ジャスト回避ウィンドウをセット
            _context.SetJustAvoidWindow(true);
            _justAvoidRemaining = _context.StateConfig?.JustAvoidTime ?? 0f;
            return GhostToggleResult.Began;
        }

        return GhostToggleResult.Failed;
    }
}

