using UnityEngine;

/// <summary>
/// プレイヤー HUD の View インターフェース（MVP パターン用）。
/// Presenter はこのインターフェースを通じて HUD を更新します。
/// </summary>
public interface IPlayerHUDView
{
    /// <summary>
    /// プレイヤーの HP 表示を正規化された値 (0..1) で設定します。
    /// </summary>
    /// <param name="normalized">正規化された HP 値（0..1）</param>
    public void SetHpNormalized(float normalized);

    /// <summary>
    /// プレイヤーのスキルゲージ表示を正規化された値 (0..1) で設定します。
    /// </summary>
    /// <param name="normalized">正規化されたスキルゲージ値（0..1）</param>
    public void SetSkillNormalized(float normalized);
}
