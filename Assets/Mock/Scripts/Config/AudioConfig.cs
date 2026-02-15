using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

public class AudioConfig : ScriptableObject
{
    public string StartSE => _startSE;
    public string TitleBGM => _titleBGM;
    public string InGameBGM => _inGameBGM;
    public string AttackSound => _attackSound;
    public string HitSound => _hitSound;
    public string PlayerDeadSound => _plaeyrDeadSound;
    public string EnemyDeadSound => _enemyDeadSound;

    [SerializeField] private string _titleBGM = "TitleBGM";
    [SerializeField] private string _startSE = "StartSE";
    [SerializeField] private string _inGameBGM = "InGameBGM";
    [SerializeField] private string _attackSound = "Attack";
    [SerializeField] private string _hitSound = "Hit";
    [SerializeField] private string _plaeyrDeadSound = "PlayerDead";
    [SerializeField] private string _enemyDeadSound = "EnemyDead";  
}
