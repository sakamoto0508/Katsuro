using System;
using INab.VFXAssets;
using UniRx;
using UnityEngine;

/// <summary>移動・攻撃Stateから独立した自傷能力の通知へ、身体オーラと専用BGMを同期する。</summary>
public sealed class PlayerBuffPresentation : MonoBehaviour
{
    /// <summary>Healと別のインスタンスを所有するBuff専用CharacterEffect。身体MeshをPrefabで割り当てる。</summary>
    [SerializeField, Tooltip("Buff専用CharacterEffect。Healに停止・置換されない独立した身体オーラです。")]
    private CharacterEffect _buffEffect;
    private PlayerSelfSacrifice _ability;
    private AudioManager _audio;
    private VFXConfig _config;
    private IDisposable _subscription;
    private bool _presenting;

    /// <summary>能力の有効状態を一度購読し、開始成功・終了・自動終了を同じ経路で表示へ反映する。</summary>
    public void Init(PlayerSelfSacrifice ability, AudioManager audio, VFXConfig config)
    {
        _subscription?.Dispose();
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        _ability = ability; _audio = audio; _config = config;
        if (_ability != null) _subscription = _ability.IsActiveRx.Subscribe(SetActive);
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    /// <summary>能力開始時だけ専用オーラを準備し、以後は同じVFXを再開する。終了時は即座に非表示にする。</summary>
    private void SetActive(bool active)
    {
        active &= isActiveAndEnabled;
        if (active == _presenting) return;
        _presenting = active;
        if (active)
        {
            if (_buffEffect != null && _config != null)
            {
                if (_buffEffect.instantiatedEffectPrefab != null)
                {
                    _buffEffect.instantiatedEffectPrefab.SetActive(true);
                    _buffEffect.enabled = true;
                    _buffEffect.StartEffect();
                    _buffEffect.effectState = UniformMeshSample.EffectState.On;
                }
                else
                {
                    _buffEffect.enabled = true;
                    _buffEffect.PlayEffectByKey(_config.PlayEffectBuff);
                }
            }
            _audio?.PlayBGM("BuffBGM", 2, 1f);
        }
        else
        {
            if (_buffEffect != null)
            {
                _buffEffect.StopEffect();
                _buffEffect.effectState = UniformMeshSample.EffectState.Off;
                if (_buffEffect.instantiatedEffectPrefab != null) _buffEffect.instantiatedEffectPrefab.SetActive(false);
                _buffEffect.enabled = false;
            }
            _audio?.StopBGM(2);
        }
    }

    /// <summary>死亡・勝利・タイトル復帰で能力を終了し、攻撃Stateに残っている場合も表示とBGMを止める。</summary>
    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Defeat || state == GameManager.GameState.Victory || state == GameManager.GameState.Title)
            _ability?.End();
    }

    /// <summary>再有効化時は能力の現在状態を表示へ反映する。</summary>
    private void OnEnable() { if (_ability != null) SetActive(_ability.IsActive); }

    /// <summary>所有者の無効化で能力と表示を終了し、Sceneを離れたオーラや専用BGMを残さない。</summary>
    private void OnDisable() { _ability?.End(); SetActive(false); }

    /// <summary>能力とゲーム進行の購読を解除して、RetryやScene遷移で購読を蓄積させない。</summary>
    private void OnDestroy()
    {
        _subscription?.Dispose(); _subscription = null;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }
}
