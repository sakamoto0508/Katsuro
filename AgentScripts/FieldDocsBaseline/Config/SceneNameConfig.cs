using UnityEngine;

[CreateAssetMenu(fileName = "SceneNameConfig", menuName = "Config/SceneNameConfig")]

/// <summary>TitleとGameの遷移先名を共有し、呼び出し側の文字列重複を避ける設定。</summary>
public class SceneNameConfig : ScriptableObject
{
    public string TitleScene => _titleScene;
    public string GameScene => _gameScene;

    [SerializeField] private string _titleScene = "TitleScene";
    [SerializeField] private string _gameScene = "GameScene";
}
