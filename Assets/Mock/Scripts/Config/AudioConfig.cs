using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

public class AudioConfig : ScriptableObject
{
    public string StartSE => _startSE;
    public string TitleBGM => _titleBGM;
    public string InGameBGM => _inGameBGM;
    public string AttackSound => _attackSound;

    [SerializeField] private string _titleBGM = "TitleBGM";
    [SerializeField] private string _startSE = "StartSE";
    [SerializeField] private string _inGameBGM = "InGameBGM";
    [SerializeField] private string _attackSound = "Attack";
}
