using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]

/// <summary>BGMと戦闘SEをAudioManagerの登録名で指定する設定。AudioClip自体の再生は管理しない。</summary>
public class AudioConfig : ScriptableObject
{
    /// <summary>ゲーム開始操作で再生するSEの登録名。</summary>
    public string StartSE => _startSE;
    /// <summary>タイトル画面で再生するBGMの登録名。AudioManagerの一覧と一致させる。</summary>
    public string TitleBGM => _titleBGM;
    /// <summary>戦闘中に再生するBGMの登録名。</summary>
    public string InGameBGM => _inGameBGM;
    /// <summary>攻撃時に再生するSEの登録名。</summary>
    public string AttackSound => _attackSound;
    /// <summary>被弾時に再生する共通SEの登録名。</summary>
    public string HitSound => _hitSound;
    /// <summary>Player死亡時に再生するSEの登録名。</summary>
    public string PlayerDeadSound => _plaeyrDeadSound;
    /// <summary>Final Blow開始時に再生するEnemy死亡SEの登録名。</summary>
    public string EnemyDeadSound => _enemyDeadSound;
    /// <summary>通常Light命中時にAudioManagerのSE一覧から検索する登録名。</summary>
    public string LightHitSound => _lightHitSound;
    /// <summary>通常Heavy命中時にAudioManagerのSE一覧から検索する登録名。</summary>
    public string HeavyHitSound => _heavyHitSound;
    /// <summary>通常Light命中時にAudioManagerのSE一覧から検索する登録名。</summary>
    [UnityEngine.Tooltip("通常Light命中時にAudioManagerのSE一覧から検索する登録名。")]
    [SerializeField] private string _lightHitSound = "Damage";
    /// <summary>通常Heavy命中時にAudioManagerのSE一覧から検索する登録名。</summary>
    [UnityEngine.Tooltip("通常Heavy命中時にAudioManagerのSE一覧から検索する登録名。")]
    [SerializeField] private string _heavyHitSound = "Damage";
    /// <summary>AudioManagerのSEリストに登録した名前。空欄なら再生しない。</summary>
    public string JustAvoidSound => _justAvoidSound;
    /// <summary>AudioManagerのSEリストに登録した名前。空欄なら再生しない。</summary>
    [Tooltip("AudioManagerのSEリストに登録した名前。空欄なら再生しない。")]
    [SerializeField] private string _justAvoidSound;

    /// <summary>タイトル画面で再生するBGMの登録名。AudioManagerの一覧と一致させる。</summary>
    [UnityEngine.Tooltip("タイトル画面で再生するBGMの登録名。AudioManagerの一覧と一致させる。")]
    [SerializeField] private string _titleBGM = "TitleBGM";
    /// <summary>ゲーム開始操作で再生するSEの登録名。</summary>
    [UnityEngine.Tooltip("ゲーム開始操作で再生するSEの登録名。")]
    [SerializeField] private string _startSE = "StartSE";
    /// <summary>戦闘中に再生するBGMの登録名。</summary>
    [UnityEngine.Tooltip("戦闘中に再生するBGMの登録名。")]
    [SerializeField] private string _inGameBGM = "InGameBGM";
    /// <summary>攻撃時に再生するSEの登録名。</summary>
    [UnityEngine.Tooltip("攻撃時に再生するSEの登録名。")]
    [SerializeField] private string _attackSound = "Attack";
    /// <summary>被弾時に再生する共通SEの登録名。</summary>
    [UnityEngine.Tooltip("被弾時に再生する共通SEの登録名。")]
    [SerializeField] private string _hitSound = "Hit";
    /// <summary>Player死亡時に再生するSEの登録名。</summary>
    [UnityEngine.Tooltip("Player死亡時に再生するSEの登録名。")]
    [SerializeField] private string _plaeyrDeadSound = "PlayerDead";
    /// <summary>Final Blow開始時に再生するEnemy死亡SEの登録名。</summary>
    [UnityEngine.Tooltip("Final Blow開始時に再生するEnemy死亡SEの登録名。")]
    [SerializeField] private string _enemyDeadSound = "EnemyDead";  
}
