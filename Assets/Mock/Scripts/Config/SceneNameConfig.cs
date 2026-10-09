using UnityEngine;

[CreateAssetMenu(fileName = "SceneNameConfig", menuName = "Config/SceneNameConfig")]

/// <summary>TitleとGameの遷移先名を共有し、呼び出し側の文字列重複を避ける設定。</summary>
public class SceneNameConfig : ScriptableObject
{
    /// <summary>タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。</summary>
    public string TitleScene => _titleScene;
    /// <summary>戦闘開始時にロードするScene名。Build設定のScene名と一致させる。</summary>
    public string GameScene => _gameScene;

    /// <summary>タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。</summary>
    [UnityEngine.Tooltip("タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。")]
    [SerializeField] private string _titleScene = "TitleScene";
    /// <summary>戦闘開始時にロードするScene名。Build設定のScene名と一致させる。</summary>
    [UnityEngine.Tooltip("戦闘開始時にロードするScene名。Build設定のScene名と一致させる。")]
    [SerializeField] private string _gameScene = "GameScene";
}
