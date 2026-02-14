using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

public class AudioConfig : ScriptableObject
{
    public string StartSE => _startSE;
    public string TitleBGM => _titleBGM;
    public string InGameBGM => _inGameBGM;

    [SerializeField] private string _titleBGM = "TitleBGM";
    [SerializeField] private string _startSE = "StartSE";
    [SerializeField] private string _inGameBGM = "InGameBGM";
}
