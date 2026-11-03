using UnityEngine;

[CreateAssetMenu(fileName = "SceneNameConfig", menuName = "Config/SceneNameConfig")]

/// <summary>TitleとGameの遷移先名を共有し、呼び出し側の文字列重複を避ける設定。</summary>
public class SceneNameConfig : ScriptableObject
{
    /// <summary>タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。</summary>
    public string TitleScene => _titleScene;
    /// <summary>戦闘開始時にロードするScene名。Build設定のScene名と一致させる。</summary>
    public string GameScene => _gameScene;
    /// <summary>正式Runと独立した修練場のScene名。</summary>
    public string TutorialScene => _tutorialScene;
    /// <summary>修練場へ移動する際に使用するScene名。</summary>
    [SerializeField, Tooltip("修練場のScene名。Build Scene Listに登録します。")]
    private string _tutorialScene = "TutorialScene";

    /// <summary>タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。</summary>
    [UnityEngine.Tooltip("タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。")]
    [SerializeField] private string _titleScene = "TitleScene";
    /// <summary>戦闘開始時にロードするScene名。Build設定のScene名と一致させる。</summary>
    [UnityEngine.Tooltip("戦闘開始時にロードするScene名。Build設定のScene名と一致させる。")]
    [SerializeField] private string _gameScene = "GameScene";
}
