using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity UI (Image) を使ったシンプルなプレイヤー HUD の実装。
/// </summary>
public class PlayerHUDView : MonoBehaviour, IPlayerHUDView
{
    [SerializeField] private Image _hpFill;
    [SerializeField] private Image _skillFill;

    /// <summary>
    /// HPバーの表示を設定します。
    /// </summary>
    /// <param name="normalized">正規化された HP 値（0 = 空、1 = 満タン）。</param>
    public void SetHpNormalized(float normalized)
    {
        if (_hpFill != null)
            _hpFill.fillAmount = Mathf.Clamp01(normalized);
    }

    /// <summary>
    /// スキルゲージの表示を設定します。
    /// </summary>
    /// <param name="normalized">正規化されたスキルゲージ値（0 = 空、1 = 満タン）。</param>
    public void SetSkillNormalized(float normalized)
    {
        if (_skillFill != null)
            _skillFill.fillAmount = Mathf.Clamp01(normalized);
    }
}
