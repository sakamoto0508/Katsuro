using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>AudioManagerで参照する登録名とAudioClipを対応付ける。</summary>
[System.Serializable]
public class SoundData
{
    /// <summary>AudioManagerからClipを検索する登録名。AudioConfigやAnimation Eventの指定名と一致させる。</summary>
    [UnityEngine.Tooltip("AudioManagerからClipを検索する登録名。AudioConfigやAnimation Eventの指定名と一致させる。")]
    public string name;      // サウンドの名前（識別用）。
    /// <summary>この登録名で再生するAudioClip。</summary>
    [UnityEngine.Tooltip("この登録名で再生するAudioClip。")]
    public AudioClip clip;   // 再生する AudioClip。
}

/// <summary>複数BGMチャンネルとSEプールを管理する。BGM基準音量と一時的なDucking倍率を分離して保持する。</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    /// <summary>タイトル・戦闘・命中などの再生登録名を共有する設定。</summary>
    public AudioConfig AudioConfig => _audioConfig; 

    /// <summary>単一BGM Sourceの互換参照。複数BGMチャンネルの初期化に使用する。</summary>
    [UnityEngine.Tooltip("単一BGM Sourceの互換参照。複数BGMチャンネルの初期化に使用する。")]
    [Header("BGM 用 AudioSource")]
    [SerializeField] private AudioSource bgmSource;
    /// <summary>BGMを再生するAudioSource一覧。各チャンネルの基準音量へ一時Ducking倍率を掛ける。</summary>
    [UnityEngine.Tooltip("BGMを再生するAudioSource一覧。各チャンネルの基準音量へ一時Ducking倍率を掛ける。")]
    [Header("BGM 用 AudioSources (複数チャンネル対応)")]
    [SerializeField] private List<AudioSource> bgmSources = new List<AudioSource>();
    /// <summary>SEプールを生成するAudioSource Prefab。SE再生用のSource設定を引き継ぐ。</summary>
    [UnityEngine.Tooltip("SEプールを生成するAudioSource Prefab。SE再生用のSource設定を引き継ぐ。")]
    [Header("SE 用 AudioSource (Prefab)")]
    [SerializeField] private AudioSource sfxSourcePrefab;
    /// <summary>BGMの登録名とAudioClipの対応一覧。登録名からClipを検索する。</summary>
    [UnityEngine.Tooltip("BGMの登録名とAudioClipの対応一覧。登録名からClipを検索する。")]
    [Header("BGM リスト")]
    [SerializeField] private List<SoundData> bgmList = new List<SoundData>();
    /// <summary>SEの登録名とAudioClipの対応一覧。AudioConfigやAnimation Eventの名前と一致させる。</summary>
    [UnityEngine.Tooltip("SEの登録名とAudioClipの対応一覧。AudioConfigやAnimation Eventの名前と一致させる。")]
    [Header("SE リスト")]
    [SerializeField] private List<SoundData> seList = new List<SoundData>();
    /// <summary>初期化時に用意するSE用AudioSource数。</summary>
    [UnityEngine.Tooltip("初期化時に用意するSE用AudioSource数。")]
    [Header("SFX プールサイズ")]
    [SerializeField] private int sfxPoolSize = 10;
    /// <summary>SE用AudioSourceプールの最大数。同時再生で増やせる上限。</summary>
    [UnityEngine.Tooltip("SE用AudioSourceプールの最大数。同時再生で増やせる上限。")]
    [SerializeField, Min(1)] private int maxSfxPoolSize = 32;
    /// <summary>タイトル・戦闘・命中などの再生登録名を共有する設定。</summary>
    [UnityEngine.Tooltip("タイトル・戦闘・命中などの再生登録名を共有する設定。")]
    [Header("オーディオ設定")]
    [SerializeField] private AudioConfig _audioConfig;
    /// <summary>敗北演出のローパスを適用するAudioListener参照。</summary>
    [UnityEngine.Tooltip("敗北演出のローパスを適用するAudioListener参照。")]
    [SerializeField] private AudioListener _audioListener;

    private Dictionary<string, AudioClip> _bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> _seDict = new Dictionary<string, AudioClip>();
    private List<AudioSource> _sfxPool = new List<AudioSource>();

    private AudioLowPassFilter _lowPass;
    /// <summary>初期化で確保するBGMチャンネル数。複数BGMの切り替えに使用する。</summary>
    [UnityEngine.Tooltip("初期化で確保するBGMチャンネル数。複数BGMの切り替えに使用する。")]
    [SerializeField, Min(3)] private int _bgmChannelCount = 3;
    private bool _initialized;
    /// <summary>Just Avoid成功中のBGM音量倍率。0で無音、1で基準音量。SEには適用しない。</summary>
    [UnityEngine.Tooltip("Just Avoid成功中のBGM音量倍率。0で無音、1で基準音量。SEには適用しない。")]
    [Header("Just Avoid BGM Duck (unscaled seconds)")]
    [SerializeField, Range(0, 1)] private float _justAvoidDuckVolume = .25f;
    /// <summary>BGMが一時音量へ下がる時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("BGMが一時音量へ下がる時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _justAvoidDuckEnter = .03f;
    /// <summary>Just Avoidの一時BGM音量を保持する時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Just Avoidの一時BGM音量を保持する時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _justAvoidDuckHold = .10f;
    /// <summary>BGMが基準音量へ戻る時間（実時間の秒）。停止済みBGMを再開しない。</summary>
    [UnityEngine.Tooltip("BGMが基準音量へ戻る時間（実時間の秒）。停止済みBGMを再開しない。")]
    [SerializeField, Min(0)] private float _justAvoidDuckRestore = .18f;
    private readonly Dictionary<AudioSource, float> _bgmBaseVolumes = new();
    private JustAvoidEnvelope _duckEnvelope;
    private float _duckMultiplier = 1;
    /// <summary>ユーザー設定の音量倍率。個々の再生基準音量とは独立する。</summary>
    public float MasterVolume { get; private set; } = 1f;
    /// <summary>全BGMチャンネルに適用するユーザー音量倍率。</summary>
    public float BGMVolume { get; private set; } = 1f;
    /// <summary>再利用・新規SE Sourceにも適用するユーザー音量倍率。</summary>
    public float SEVolume { get; private set; } = 1f;
    private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new();
    private const string VolumeKey = "Katsuro.Audio.";
    /// <summary>保存値の異常を除外し、音量を0～1に収める。</summary>
    private static float ValidVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    /// <summary>三つのユーザー倍率を即時反映し、再生状態に触れず保存値を更新する。</summary>
    public void SetUserVolumes(float master, float bgm, float se)
    {
        MasterVolume = ValidVolume(master); BGMVolume = ValidVolume(bgm); SEVolume = ValidVolume(se);
        PlayerPrefs.SetFloat(VolumeKey + "Master", MasterVolume);
        PlayerPrefs.SetFloat(VolumeKey + "BGM", BGMVolume);
        PlayerPrefs.SetFloat(VolumeKey + "SE", SEVolume);
        ApplyUserVolumes();
    }
    /// <summary>ユーザー倍率のみを100%へ戻し、個別音量とDuckingを保持して保存する。</summary>
    public void ResetUserVolumes() { SetUserVolumes(1f, 1f, 1f); SaveUserVolumes(); }
    /// <summary>設定終了・アプリ終了時に音量設定をディスクへ保存する。</summary>
    public void SaveUserVolumes() => PlayerPrefs.Save();
    /// <summary>保存済みの倍率を復元し、初回起動では各100%にする。</summary>
    private void LoadUserVolumes()
    {
        MasterVolume = ValidVolume(PlayerPrefs.GetFloat(VolumeKey + "Master", 1f));
        BGMVolume = ValidVolume(PlayerPrefs.GetFloat(VolumeKey + "BGM", 1f));
        SEVolume = ValidVolume(PlayerPrefs.GetFloat(VolumeKey + "SE", 1f));
        ApplyUserVolumes();
    }
    /// <summary>基準音量から全Sourceの実音量を再計算する。停止した音を再開しない。</summary>
    private void ApplyUserVolumes()
    {
        if (bgmSources != null) foreach (var source in bgmSources)
            if (source != null) source.volume = BaseVolume(source) * BGMVolume * _duckMultiplier * MasterVolume;
        foreach (var source in _sfxPool) if (source != null)
            source.volume = (_sfxBaseVolumes.TryGetValue(source, out var baseline) ? baseline : 1f) * SEVolume * MasterVolume;
    }
    /// <summary>アプリ終了時に変更済み音量を保存する。</summary>
    private void OnApplicationQuit() { if (_initialized) SaveUserVolumes(); }
    /// <summary>アプリの中断時に変更済み音量を保存する。</summary>
    private void OnApplicationPause(bool paused) { if (paused && _initialized) SaveUserVolumes(); }
    /// <summary>チャンネルの基準音量を取得し、未登録なら現在のAudioSource音量を保存する。</summary>
    /// <returns>一時倍率を掛ける前のチャンネル音量。</returns>
    private float BaseVolume(AudioSource source)
    {
        if (!_bgmBaseVolumes.TryGetValue(source, out float volume))
            _bgmBaseVolumes[source] = volume = source.volume;
        return volume;
    }
    /// <summary>チャンネルの基準音量を更新し、現在のDucking倍率を掛けた実音量を反映する。</summary>
    private void SetBaseVolume(AudioSource source, float volume)
    {
        _bgmBaseVolumes[source] = volume;
        source.volume = volume * _duckMultiplier * BGMVolume * MasterVolume;
    }
    /// <summary>現在の音量包絡線から回避用Duckingを再開始し、連続成功でも倍率を累積しない。</summary>
    public void PlayJustAvoidDuck()
    {
        if (!isActiveAndEnabled) return;
        UpdateDuck(Time.unscaledTime);
        _duckEnvelope.Begin(Time.unscaledTime);
    }
    /// <summary>回避用Duckingを実時間で進め、各BGMの基準音量に倍率を掛けて反映する。</summary>
    private void Update() => UpdateDuck(Time.unscaledTime);
    /// <summary>指定実時刻からDucking倍率を求め、SEに触れず全BGMチャンネルへ適用する。</summary>
    private void UpdateDuck(float now)
    {
        _duckMultiplier = Mathf.Lerp(1, _justAvoidDuckVolume,
            _duckEnvelope.Evaluate(now, _justAvoidDuckEnter, _justAvoidDuckHold, _justAvoidDuckRestore));
        if (bgmSources != null) foreach (var source in bgmSources)
            if (source != null) source.volume = BaseVolume(source) * _duckMultiplier * BGMVolume * MasterVolume;
    }
    /// <summary>一時倍率を通常へ戻す。基準音量・Clip・停止状態を保持し、BGMを再生し直さない。</summary>
    public void CancelJustAvoidDuck() { _duckEnvelope.Reset(); UpdateDuck(Time.unscaledTime); }
    /// <summary>Scene切り替え時に回避用Duckingを解除する。</summary>
    private void SceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next) => CancelJustAvoidDuck();
    /// <summary>SceneのUnload時に一時Duckingを通常音量へ戻す。</summary>
    private void SceneUnloaded(UnityEngine.SceneManagement.Scene scene) => CancelJustAvoidDuck();
    /// <summary>勝利・敗北・Titleなど戦闘外へ移った場合にDuckingを解除する。</summary>
    private void GameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.InGame && state != GameManager.GameState.Pause) CancelJustAvoidDuck();
    }
    /// <summary>Sceneとゲーム進行の中断通知を購読する。</summary>
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneChanged;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneUnloaded;
        GameManager.OnGameStateChanged += GameStateChanged;
    }
    /// <summary>中断通知の購読を解除し、一時Duckingを通常倍率へ戻す。</summary>
    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneChanged;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= SceneUnloaded;
        GameManager.OnGameStateChanged -= GameStateChanged;
        CancelJustAvoidDuck();
    }
    /// <summary>共有AudioManagerを確立し、ListenerのLowPass参照とBGM・SEプールを一度だけ準備する。</summary>
    public void Init(AudioListener listener)
    {
        if (Instance != null && Instance != this)
        {
            Instance.Init(listener);
            Destroy(gameObject);
            return;
        }
        bool listenerChanged = listener != null && listener != _audioListener;
        if (listenerChanged) _audioListener = listener;
        if (_audioListener != null && (listenerChanged || _lowPass == null))
        {
            _lowPass = _audioListener.GetComponent<AudioLowPassFilter>();
            if (_lowPass == null) _lowPass = _audioListener.gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.enabled = false;
        }
        if (_initialized) return;
        _initialized = true;
        // シングルトン初期化。
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>BGMチャンネル、登録名のClip辞書とSEプールを初期化する。</summary>
    private void InitializeAudioManager()
    {
        // BGM 用 AudioSource を準備する（複数チャンネル対応）。
        EnsureBGMSources();
        CreateBgmChannels();
        foreach (var source in bgmSources) RetainSceneSource(source);
        RetainSceneSource(sfxSourcePrefab);
        LoadUserVolumes();

        // BGM リストを辞書に登録。
        _bgmDict.Clear();
        foreach (var bgm in bgmList)
        {
            if (bgm != null && !string.IsNullOrEmpty(bgm.name) && !_bgmDict.ContainsKey(bgm.name) && bgm.clip != null)
            {
                _bgmDict.Add(bgm.name, bgm.clip);
            }
        }

        // SE リストを辞書に登録。
        _seDict.Clear();
        foreach (var se in seList)
        {
            if (se != null && !string.IsNullOrEmpty(se.name) && !_seDict.ContainsKey(se.name) && se.clip != null)
            {
                _seDict.Add(se.name, se.clip);
            }
        }

        // SFX 用のプールを生成。
        CreateSFXPool();
    }

    /// <summary>Sceneに配置されたBGM音源とSE生成元を共有Managerの子にし、Scene遷移による参照消失を防ぐ。</summary>
    private void RetainSceneSource(AudioSource source)
    {
        if (source != null && source.gameObject.scene.IsValid() && !source.transform.IsChildOf(transform))
            source.transform.SetParent(transform, true);
    }

    /// <summary>既存の単一参照を複数チャンネルへ引き継ぎ、BGM Sourceがなければ先頭チャンネルを生成する。</summary>
    private void EnsureBGMSources()
    {
        // bgmSources リストを保証し、既存の単一 bgmSource が割り当てられている場合はそれを利用する。
        if (bgmSources == null) bgmSources = new List<AudioSource>();

        // 既存の inspector で設定された単一 bgmSource があり、リストが空なら追加する。
        if ((bgmSource != null && !bgmSource.Equals(null)) && bgmSources.Count == 0)
        {
            bgmSources.Add(bgmSource);
        }

        // 少なくとも 1 つの BGM ソースが存在しなければ生成する。
        if (bgmSources.Count == 0)
        {
            GameObject bgmObj = new GameObject("BGM_Source_0");
            bgmObj.transform.SetParent(transform);
            var source = bgmObj.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            bgmSources.Add(source);
            // 保守のため古い単一参照にもセットしておく。
            bgmSource = source;
        }
    }

    /// <summary>設定数のBGMチャンネルを確保し、SEから独立したAudioSourceを生成する。</summary>
    private void CreateBgmChannels()
    {
        while (bgmSources.Count < Mathf.Max(3, _bgmChannelCount))
        {
            var sourceObject = new GameObject("BGM_Source_" + bgmSources.Count);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            bgmSources.Add(source);
        }
    }

    /// <summary>負のチャンネルを先頭へ補正して対応するBGM Sourceを取得する。</summary>
    /// <returns>指定チャンネルのSource。範囲外ならnull。</returns>
    private AudioSource GetBgmSource(int channel)
    {
        channel = Mathf.Max(0, channel);
        return bgmSources != null && channel < bgmSources.Count ? bgmSources[channel] : null;
    }
    /// <summary>以前のSEプールを片付け、指定Prefabから上限内の再利用Sourceを生成する。</summary>
    private void CreateSFXPool()
    {
        // 既存のプールを破棄してから再生成する。
        foreach (var source in _sfxPool)
        {
            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }
        _sfxPool.Clear();
        _sfxBaseVolumes.Clear();

        // 指定数だけ SFX 用 AudioSource を生成してプールに追加する。
        for (int i = 0; i < Mathf.Clamp(sfxPoolSize, 0, Mathf.Max(1, maxSfxPoolSize)); i++)
        {
            if (sfxSourcePrefab != null)
            {
                var sfxSource = Instantiate(sfxSourcePrefab, transform);
                sfxSource.playOnAwake = false;
                sfxSource.loop = false; // SFX はループしない。
                sfxSource.clip = null;  // クリップは後で設定する。
                _sfxPool.Add(sfxSource);
                _sfxBaseVolumes[sfxSource] = 1f;
                sfxSource.volume = SEVolume * MasterVolume;
            }
        }
    }


    /// <summary>指定チャンネルへClipと基準音量を設定してループ再生する。同一Clip・基準音量で再生中なら継続する。</summary>
    public void PlayBGM(AudioClip clip, int channel = 0, float volume = 1f)
    {
        if (clip == null) return;
        var source = GetBgmSource(channel);
        if (source == null)
        {
            Debug.LogError("指定チャンネルの BGM ソースが作成されていません。 channel=" + channel);
            return;
        }

        // 同じクリップを別の SFX ソースが再生している場合は停止する。
        foreach (var sfx in _sfxPool)
        {
            if (sfx != null && sfx.isPlaying && sfx.clip == clip)
            {
                sfx.Stop();
                sfx.clip = null;
            }
        }

        // 既に指定チャンネルで同じクリップ・同じ音量で再生中なら処理しない。
        float clampedVolume = Mathf.Clamp01(volume);
        if (source.clip == clip && source.isPlaying && Mathf.Approximately(BaseVolume(source), clampedVolume)) return;

        source.Stop();
        source.clip = clip;
        source.loop = true;
        SetBaseVolume(source, clampedVolume);
        source.Play();
    }


    /// <summary>
    /// BGM を名前で再生する。
    /// </summary>
    public void PlayBGM(string bgmName)
    {
        if (string.IsNullOrEmpty(bgmName)) return;
        if (_bgmDict.TryGetValue(bgmName, out var clip))
        {
            PlayBGM(clip);
        }
        else
        {
            Debug.LogWarning($"指定された BGM '{bgmName}' が見つかりません。");
        }
    }

    /// <summary>
    /// 既存互換: 名前とボリューム指定で再生 (チャンネルはデフォルト 0)
    /// </summary>
    public void PlayBGM(string bgmName, float volume)
    {
        PlayBGM(bgmName, 0, volume);
    }

    /// <summary>
    /// 名前で指定して BGM を再生します。チャンネルとボリュームを指定可能（既定は channel=0, volume=1）。
    /// </summary>
    public void PlayBGM(string bgmName, int channel = 0, float volume = 1f)
    {
        if (string.IsNullOrEmpty(bgmName)) return;
        if (_bgmDict.TryGetValue(bgmName, out var clip))
        {
            PlayBGM(clip, channel, volume);
        }
        else
        {
            Debug.LogWarning($"指定された BGM '{bgmName}' が見つかりません。");
        }
    }

    /// <summary>
    /// BGM を停止する。
    /// </summary>
    public void StopBGM()
    {
        // 既存呼び出し互換: デフォルトチャンネル 0 を停止
        StopBGM(0);
    }

    /// <summary>
    /// 指定チャンネルの BGM を停止します。
    /// </summary>
    public void StopBGM(int channel)
    {
        var src = GetBgmSource(channel);
        if (src != null)
        {
            src.Stop();
            src.clip = null;
        }
    }

    /// <summary>
    /// すべての BGM チャンネルを停止します。
    /// </summary>
    public void StopAllBGMs()
    {
        if (bgmSources == null) return;
        foreach (var s in bgmSources)
        {
            if (s != null)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }

    /// <summary>
    /// BGMの再生基準音量を設定する。ユーザー倍率の変更にはSetUserVolumesを使用する。
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        // 既存呼び出し互換: チャンネル 0 を設定
        SetBGMVolume(volume, 0);
    }

    /// <summary>
    /// 指定チャンネルまたは全チャンネルの BGM 音量を設定する。
    /// channel が -1 の場合は全チャンネルに適用。
    /// </summary>
    public void SetBGMVolume(float volume, int channel)
    {
        volume = Mathf.Clamp01(volume);
        if (channel < 0)
        {
            if (bgmSources == null) return;
            foreach (var s in bgmSources)
            {
                if (s != null) SetBaseVolume(s, volume);
            }
        }
        else
        {
            var src = GetBgmSource(channel);
            if (src != null) SetBaseVolume(src, volume);
        }
    }

    /// <summary>
    /// SE を名前で再生する。
    /// </summary>
    public void PlaySE(string seName, float volume = 1f)
    {
        if (string.IsNullOrEmpty(seName)) return;
        if (_seDict.TryGetValue(seName, out var clip))
        {
            var src = GetAvailableSfxSource();
            if (src != null)
            {
                src.clip = clip;
                _sfxBaseVolumes[src] = Mathf.Clamp01(volume);
                src.volume = _sfxBaseVolumes[src] * SEVolume * MasterVolume;
                src.Play();
            }
        }
        else
        {
            Debug.LogWarning($"指定された SE '{seName}' が見つかりません。");
        }
    }

    /// <summary>
    /// 利用可能な SFX 用 AudioSource を取得する。
    /// </summary>
    private AudioSource GetAvailableSfxSource()
    {
        foreach (var s in _sfxPool)
        {
            if (s != null && !s.isPlaying) return s;
        }

        // 必要なら予備の SFX ソースを動的に生成する。
        if (sfxSourcePrefab != null && _sfxPool.Count < Mathf.Max(1, maxSfxPoolSize))
        {
            var extra = Instantiate(sfxSourcePrefab, transform);
            extra.playOnAwake = false;
            extra.loop = false;
            _sfxPool.Add(extra);
            _sfxBaseVolumes[extra] = 1f;
            extra.volume = SEVolume * MasterVolume;
            return extra;
        }

        return null;
    }

    /// <summary>
    /// ユーザーSE倍率を更新し、次回再生・再利用Sourceにも適用する。
    /// </summary>
    public void SetSEVolume(float volume)
    {
        SetUserVolumes(MasterVolume, BGMVolume, volume);
    }

    /// <summary>
    /// AudioListener に対してローパスフィルタを適用します（cutoff に Hz を指定）。
    /// </summary>
    public void ApplyLowPassToListener(float cutoffFrequency)
    {
        if (_lowPass == null) return;
        _lowPass.cutoffFrequency = cutoffFrequency;
        _lowPass.enabled = true;
    }

    /// <summary>
    /// AudioListener のローパスフィルタを削除します。
    /// </summary>
    public void RemoveLowPassFromListener()
    {
        if (_lowPass != null) _lowPass.enabled = false;
    }

    /// <summary>
    /// 即座に全てのオーディオを停止します（BGM と SFX）。
    /// </summary>
    public void StopAllAudioImmediate()
    {
        StopAllBGMs();
        foreach (var s in _sfxPool)
        {
            if (s != null)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }

    /// <summary>一時音量を解除し、自身が共有インスタンスの場合は参照を消去する。</summary>
    private void OnDestroy()
    {
        CancelJustAvoidDuck();
        if (Instance == this) Instance = null;
    }
}
