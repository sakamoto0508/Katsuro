using UnityEngine;

[CreateAssetMenu(fileName = "SceneNameConfig", menuName = "Config/SceneNameConfig")]

public class SceneNameConfig : ScriptableObject
{
    public string TitleScene => _titleScene;
    public string GameScene => _gameScene;

    [SerializeField] private string _titleScene = "TitleScene";
    [SerializeField] private string _gameScene = "GameScene";
}
