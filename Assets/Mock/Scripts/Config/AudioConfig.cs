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
    /// <summary>通常Lightの接触音。未設定なら従来の登録名を使う。</summary>
    [SerializeField, Tooltip("Light命中音。既存SEプールとユーザー音量設定を使います。")]
    private AudioClip _lightHitClip;
    /// <summary>通常Heavyの衝撃音。物理命中時だけ再生する。</summary>
    [SerializeField, Tooltip("Heavy命中音。空振りでは再生しません。")]
    private AudioClip _heavyHitClip;
    /// <summary>追撃の接触音。ジャスト回避成功時の音とは別の命中音。</summary>
    [SerializeField, Tooltip("Just Avoid追撃の命中音。成功演出のSEは変更しません。")]
    private AudioClip _counterHitClip;
    /// <summary>命中音の基準音量。X=Light、Y=Heavy、Z=追撃。ユーザー音量をさらに合成する。</summary>
    [SerializeField, Tooltip("命中音量。X=Light、Y=Heavy、Z=追撃。")]
    private Vector3 _hitVolumes = new Vector3(.7f, .9f, .65f);
    /// <summary>素材先頭の間を除く再生開始位置。音程や音源そのものは変更しない。</summary>
    [SerializeField, Tooltip("再生開始位置（秒）。X=Light、Y=Heavy、Z=追撃。")]
    private Vector3 _hitStartOffsets = new Vector3(.04f, 0f, .195f);
    /// <summary>命中種別のClipを取得する。追撃音が未設定ならHeavyへ戻す。</summary>
    public AudioClip GetHitClip(bool heavy, bool counter) => counter ? (_counterHitClip != null ? _counterHitClip : _heavyHitClip) : heavy ? _heavyHitClip : _lightHitClip;
    /// <summary>ユーザー音量へ掛ける命中種別の基準音量を取得する。</summary>
    public float GetHitVolume(bool heavy, bool counter) => Mathf.Clamp01(_hitVolumes[counter ? 2 : heavy ? 1 : 0]);
    /// <summary>選択Clipの先頭の間を除く開始位置を取得する。</summary>
    public float GetHitStartOffset(bool heavy, bool counter) => Mathf.Max(0f, _hitStartOffsets[counter && _counterHitClip != null ? 2 : heavy || counter ? 1 : 0]);
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
