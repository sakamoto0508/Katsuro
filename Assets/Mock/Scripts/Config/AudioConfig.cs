using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

public class AudioConfig : ScriptableObject
{
    public string StartSE => _startSE;
    public string TitleBGM => _titleBGM;

    [SerializeField] private string _titleBGM = "TitleBGM";
    [SerializeField] private string _startSE = "StartSE";
}
