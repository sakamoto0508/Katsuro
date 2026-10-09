using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

/// <summary>BGMと戦闘SEをAudioManagerの登録名で指定する設定。AudioClip自体の再生は管理しない。</summary>
public class AudioConfig : ScriptableObject
{
    public string StartSE => _startSE;
    public string TitleBGM => _titleBGM;
    public string InGameBGM => _inGameBGM;
    public string AttackSound => _attackSound;
    public string HitSound => _hitSound;
    public string PlayerDeadSound => _plaeyrDeadSound;
    public string EnemyDeadSound => _enemyDeadSound;
    public string LightHitSound => _lightHitSound;
    public string HeavyHitSound => _heavyHitSound;
    [SerializeField] private string _lightHitSound = "Damage";
    [SerializeField] private string _heavyHitSound = "Damage";
    public string JustAvoidSound => _justAvoidSound;
    [Tooltip("AudioManagerのSEリストに登録した名前。空欄なら再生しない。")]
    [SerializeField] private string _justAvoidSound;

    [SerializeField] private string _titleBGM = "TitleBGM";
    [SerializeField] private string _startSE = "StartSE";
    [SerializeField] private string _inGameBGM = "InGameBGM";
    [SerializeField] private string _attackSound = "Attack";
    [SerializeField] private string _hitSound = "Hit";
    [SerializeField] private string _plaeyrDeadSound = "PlayerDead";
    [SerializeField] private string _enemyDeadSound = "EnemyDead";  
}
