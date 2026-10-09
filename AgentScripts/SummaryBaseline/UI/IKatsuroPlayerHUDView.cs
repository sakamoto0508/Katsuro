using UnityEngine;

namespace Mock.UI
{
    /// <summary>
    /// プレイヤー HUD のビューインターフェース（Katsuro 固有）。
    /// Presenter はこのインターフェースを通じて HUD を更新します。
    /// </summary>
    public interface IKatsuroPlayerHUDView
    {
        /// <summary>
        /// HP 表示を正規化値 (0..1) で設定します。
        /// </summary>
        /// <param name="normalized">正規化された HP 値（0=空, 1=満タン）</param>
        void SetHpNormalized(float normalized);

        /// <summary>
        /// スキルゲージ表示を正規化値 (0..1) で設定します。
        /// </summary>
        /// <param name="normalized">正規化されたスキルゲージ値（0=空, 1=満タン）</param>
        void SetSkillNormalized(float normalized);
    }
}
