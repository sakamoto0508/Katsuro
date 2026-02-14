using UnityEngine;

public class AudioConfig : ScriptableObject
{
    public string TitleClickSE => _titleClickSE;
    public string TitleBGM => _titleBGM;

    [SerializeField] private string _titleBGM = "Title";
    [SerializeField] private string _titleClickSE = "TitleClick";
}
