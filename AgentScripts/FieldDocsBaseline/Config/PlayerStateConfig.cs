using System.Collections.Generic;
using UnityEngine;

/// <summary>コンボ段階ごとのClip、受付遅延、固定ダメージを保持し、未設定時の代替値を提供する。</summary>
[System.Serializable]
public class AttackClipList
{
    [SerializeField] private List<AnimationClip> _clips = new();
    [SerializeField] private List<float> _comboWindowDelaySeconds = new();
    [SerializeField] private List<float> _clipFlatDamage = new();

    public IReadOnlyList<AnimationClip> Clips => _clips;

    /// <summary>段階番号を有効範囲へ制限してClipを取得する。未設定ならnullを返す。</summary>
    /// <returns>指定段階に対応するClip。リストが空ならnull。</returns>
    public AnimationClip GetClip(int index)
    {
        if (_clips == null || _clips.Count == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, _clips.Count - 1);
        return _clips[index];
    }

    /// <summary>Clip長を返し、Clip未設定時は指定の代替秒数を使用する。</summary>
    /// <returns>最低0.1秒を保証したClip長または代替秒数。</returns>
    public float GetDuration(int index, float fallbackSeconds)
    {
        var clip = GetClip(index);
        if (clip == null)
        {
            return Mathf.Max(0.1f, fallbackSeconds);
        }

        return Mathf.Max(0.1f, clip.length);
    }

    /// <summary>段階別のコンボ受付遅延を取得し、未設定・負値の場合は代替値を使用する。</summary>
    /// <returns>0以上の受付遅延秒数。</returns>
    public float GetComboWindowDelay(int index, float fallbackSeconds)
    {
        if (_comboWindowDelaySeconds == null || _comboWindowDelaySeconds.Count == 0)
        {
            return Mathf.Max(0f, fallbackSeconds);
        }

        index = Mathf.Clamp(index, 0, _comboWindowDelaySeconds.Count - 1);
        float value = _comboWindowDelaySeconds[index];
        return value >= 0f ? value : Mathf.Max(0f, fallbackSeconds);
    }

    /// <summary>段階別の固定ダメージを取得する。設定済みの負数やゼロはそのまま返す。</summary>
    /// <returns>設定されたダメージ値。未設定の場合は非負の代替値。</returns>
    public float GetClipDamage(int index, float fallback = 0f)
    {
        if (_clipFlatDamage == null || _clipFlatDamage.Count == 0)
        {
            return Mathf.Max(0f, fallback);
        }

        index = Mathf.Clamp(index, 0, _clipFlatDamage.Count - 1);
        float value = _clipFlatDamage[index];
        return value; // 設定された負数やゼロもそのまま許可する。
    }
}

/// <summary>Playerのスキルゲージ、幽体化、攻撃Clipとコンボ受付時間を共有する設定。</summary>
[CreateAssetMenu(fileName = "PlayerStateConfig", menuName = "ScriptableObjects/Player/PlayerStateConfig")]
public class PlayerStateConfig : ScriptableObject
{
    /// <summary>スキルゲージの最大値。</summary>
    public float MaxSkillGauge => _maxSkillGauge;
    /// <summary>スキルゲージの秒あたり回復量。</summary>
    public float SkillGaugeRecoveryPerSecond => _skillGaugeRecoveryPerSecond;
    /// <summary>ジャスト回避（Just Avoid）成功時間。</summary>
    public float JustAvoidTime => _justAvoidTime;

    /// <summary>ゴースト化時の色。</summary>
    public Color GhostColor => _ghostColor;

    /// <summary>ゴースト化時の透明度（アルファ値）。</summary>
    public float GhostAlpha => _alpha;

    public IReadOnlyList<AnimationClip> LightAttackClips => _lightAttackClips.Clips;
    public IReadOnlyList<AnimationClip> LockOnLightAttackClips => _lockOnLightAttackClips.Clips;
    public IReadOnlyList<AnimationClip> StrongAttackClips => _strongAttackClips.Clips;
    public IReadOnlyList<AnimationClip> JustAvoidAttackClips => _justAvoidAttackClips.Clips;

    /// <summary>Lock-Onに応じた弱攻撃Clipの長さを取得し、未設定時は弱攻撃用の代替時間を返す。</summary>
    /// <returns>指定段階のClip長または代替秒数。</returns>
    public float GetLightAttackDuration(int comboIndex = 0) => GetLightAttackDuration(false, comboIndex);

    /// <summary>Lock-Onに応じた弱攻撃Clipの長さを取得し、未設定時は弱攻撃用の代替時間を返す。</summary>
    /// <returns>指定段階のClip長または代替秒数。</returns>
    public float GetLightAttackDuration(bool isLockOn, int comboIndex = 0)
        => SelectLightAttackList(isLockOn).GetDuration(comboIndex, 0.8f);

    /// <summary>Lock-Onの有無とコンボ段階に対応する弱攻撃の固定ダメージを取得する。</summary>
    /// <returns>指定段階のダメージ設定値。</returns>
    public float GetLightAttackClipDamage(bool isLockOn, int comboIndex = 0)
        => SelectLightAttackList(isLockOn).GetClipDamage(comboIndex, 0f);

    /// <summary>Lock-Onの有無に応じて弱攻撃のコンボ受付遅延を取得する。</summary>
    /// <returns>受付遅延秒数。</returns>
    public float GetLightAttackComboWindowDelay(bool isLockOn, int comboIndex = 0)
        => SelectLightAttackList(isLockOn).GetComboWindowDelay(comboIndex, _defaultLightComboWindowDelay);

    /// <summary>使用する弱攻撃Clipリストからコンボ段数を求める。</summary>
    /// <returns>使用可能な弱攻撃段数。</returns>
    public int GetLightAttackComboCount(bool isLockOn)
    {
        var clips = SelectLightAttackList(isLockOn).Clips;
        return Mathf.Max(1, clips?.Count ?? 0);
    }

    /// <summary>Lock-On用Clipが設定されていればそのリストを選び、なければ通常弱攻撃を使用する。</summary>
    /// <returns>選択された弱攻撃Clipリスト。</returns>
    public IReadOnlyList<AnimationClip> GetLightAttackClips(bool isLockOn)
        => SelectLightAttackList(isLockOn).Clips;

    /// <summary>強攻撃の段階別Clip長を取得し、未設定時は代替時間を使用する。</summary>
    /// <returns>指定段階の強攻撃時間。</returns>
    public float GetStrongAttackDuration(int comboIndex = 0) => _strongAttackClips.GetDuration(comboIndex, 1.0f);

    /// <summary>強攻撃のコンボ段階に対応する固定ダメージを取得する。</summary>
    /// <returns>指定段階のダメージ設定値。</returns>
    public float GetStrongAttackClipDamage(int comboIndex = 0) => _strongAttackClips.GetClipDamage(comboIndex, 0f);

    /// <summary>強攻撃の段階別コンボ受付遅延を取得する。</summary>
    /// <returns>受付遅延秒数。</returns>
    public float GetStrongAttackComboWindowDelay(int comboIndex = 0)
        => _strongAttackClips.GetComboWindowDelay(comboIndex, _defaultStrongComboWindowDelay);

    /// <summary>追撃の段階別Clip長を取得し、未設定時は追撃用の代替時間を使用する。</summary>
    /// <returns>指定段階の追撃時間。</returns>
    public float GetJustAvoidAttackDuration(int comboIndex = 0) => _justAvoidAttackClips.GetDuration(comboIndex, 0.9f);

    [Header("Skill Gauge")]
    [SerializeField, Min(1f)] private float _maxSkillGauge = 100f;
    [SerializeField, Min(0f)] private float _skillGaugeRecoveryPerSecond = 10f;

    [Header("Avoid")]
    [SerializeField, Min(0.01f)] private float _justAvoidTime = 0.2f;
    [SerializeField] private Color _ghostColor = Color.gray;
    [SerializeField, Range(0f, 1f)] private float _alpha = 0.7f;

    [Header("Combo Window Timing")]
    [SerializeField, Min(0f)] private float _defaultLightComboWindowDelay = 0.05f;
    [SerializeField, Min(0f)] private float _defaultStrongComboWindowDelay = 0.1f;

    [Header("Attack Clips")]
    [SerializeField] private AttackClipList _lightAttackClips = new();
    [SerializeField] private AttackClipList _lockOnLightAttackClips = new();
    [SerializeField] private AttackClipList _strongAttackClips = new();
    [SerializeField] private AttackClipList _justAvoidAttackClips = new();

    /// <summary>Lock-On用Clipの存在を確認し、実際に使用する弱攻撃リストを選ぶ。</summary>
    /// <returns>使用対象の弱攻撃設定。</returns>
    private AttackClipList SelectLightAttackList(bool isLockOn)
    {
        bool hasLockOnVariant = _lockOnLightAttackClips != null
            && _lockOnLightAttackClips.Clips != null
            && _lockOnLightAttackClips.Clips.Count > 0;

        if (isLockOn && hasLockOnVariant)
        {
            return _lockOnLightAttackClips;
        }

        return _lightAttackClips;
    }
}
