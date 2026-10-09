using UniRx;
using System;

namespace Mock.UI
{
    /// <summary>
    /// モデルのリアクティブプロパティを購読して View を更新する Presenter（MVP）。
    /// </summary>
    public class PlayerHUDPresenter : IDisposable
    {
        /// <summary>
        /// Presenter が更新対象とする View（UI への唯一の参照）。
        /// </summary>
        private readonly IKatsuroPlayerHUDView _view;

        /// <summary>
        /// 現在の HP（生値）を公開するリアクティブプロパティ。
        /// Presenter はこれを購読して View を更新します。
        /// </summary>
        private readonly IReadOnlyReactiveProperty<float> _hpReactive;

        /// <summary>
        /// HP の最大値。正規化に用います。
        /// </summary>
        private readonly float _maxHp;

        /// <summary>
        /// スキルゲージの正規化値（0..1）を公開するリアクティブプロパティ。
        /// </summary>
        private readonly IReadOnlyReactiveProperty<float> _skillNormalizedReactive;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="view">HUD を更新する View 実装。</param>
        /// <param name="hpReactive">現在 HP のリアクティブプロパティ（生値）。</param>
        /// <param name="maxHp">HP の最大値（正規化に使用）。</param>
        /// <param name="skillNormalizedReactive">スキルゲージの正規化リアクティブプロパティ（0..1）。</param>
        public PlayerHUDPresenter(IKatsuroPlayerHUDView view, IReadOnlyReactiveProperty<float> hpReactive, float maxHp, IReadOnlyReactiveProperty<float> skillNormalizedReactive)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _hpReactive = hpReactive ?? throw new ArgumentNullException(nameof(hpReactive));
            _maxHp = maxHp;
            _skillNormalizedReactive = skillNormalizedReactive ?? throw new ArgumentNullException(nameof(skillNormalizedReactive));

            _hpReactive
                .Subscribe(hp => UpdateHp(hp))
                .AddTo(_disposables);

            _skillNormalizedReactive
                .Subscribe(norm => _view.SetSkillNormalized(norm))
                .AddTo(_disposables);

            // 初期表示を更新
            UpdateHp(_hpReactive.Value);
            _view.SetSkillNormalized(_skillNormalizedReactive.Value);
        }

        private void UpdateHp(float hp)
        {
            var norm = _maxHp > 0f ? hp / _maxHp : 0f;
            _view.SetHpNormalized(norm);
        }

        /// <summary>
        /// 購読を解除してリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

