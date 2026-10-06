using UnityEngine;

[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    [Header("ジャスト回避")]
    [Tooltip("薄い円形衝撃波のParticleSystem Prefab。未設定なら衝撃波のみ省略。自動破棄スクリプトは付けない。")]
    [SerializeField] private GameObject _justAvoidShockwavePrefab;
    [SerializeField, Min(.01f)] private float _shockwaveDuration = .25f;
    [SerializeField] private bool _shockwaveEnable;
    [SerializeField, Range(0f, 1f)] private float _shockwaveStrength = .2f;
    [SerializeField, Min(.01f)] private float _shockwaveSize = .5f;
    [Header("命中")]
    [Tooltip("追加のParticleSystem Prefab。未設定でも小さい接触フラッシュは表示する。")]
    [SerializeField] private GameObject _hitVFX;
    [SerializeField, Min(.01f)] private float _hitVFXDuration = .15f;
    public GameObject HitVFX => _hitVFX;
    public float HitVFXDuration => _hitVFXDuration;
    public bool ShockwaveEnable => _shockwaveEnable;
    public float ShockwaveStrength => _shockwaveStrength;
    public float ShockwaveSize => _shockwaveSize;
    public GameObject JustAvoidShockwavePrefab => _justAvoidShockwavePrefab;
    public float ShockwaveDuration => _shockwaveDuration;
    public string PlayEffectHeal => _playEffectHeal;
    public string PlayEffectGhost => _playEffectGhost;
    public string PlayEffectBuff => _playEffectBuff;
    [SerializeField] private string _playEffectHeal;
    [SerializeField] private string _playEffectGhost;
    [SerializeField] private string _playEffectBuff;
}
