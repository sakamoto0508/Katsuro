using UnityEngine;

/// <summary>シーン開始時に依存先を接続する。各処理には必要な参照だけを渡す。</summary>
public sealed class SceneInitialization
{
    public AudioManager Audio { get; private set; }
    public GlobalFader Fader { get; private set; }
    public LoadSceneManager Loader { get; private set; }
    public HitStopManager HitStop { get; private set; }
    public FinalBlowManager FinalBlow { get; private set; }
    public PlayerDeadManager PlayerDead { get; private set; }
    private bool _initialized;

    public void Init(GameManager game = null)
    {
        if (_initialized) return;
        _initialized = true;
        var listener = Object.FindFirstObjectByType<AudioListener>();
        InitAll<AudioManager>(x => x.Init(listener));
        Audio = AudioManager.Instance;
        InitAll<GlobalFader>(x => x.Init());
        Fader = GlobalFader.Instance;
        if (Fader == null)
        {
            Debug.LogError("シーンにGlobalFaderを配置してください。");
        }
        InitAll<LoadSceneManager>(x => x.Init(Fader));
        Loader = LoadSceneManager.Instance;
        InitAll<HitStopManager>(x => x.Init());
        HitStop = HitStopManager.Instance;
        InitAll<FinalBlowManager>(x => x.Init(game, Audio, HitStop, Loader, Fader));
        FinalBlow = FinalBlowManager.Instance;
        InitAll<PlayerDeadManager>(x => x.Init(game, Audio, HitStop, Loader, Fader));
        PlayerDead = PlayerDeadManager.Instance;
        InitAll<InputBuffer>(x => x.Init());
        InitAll<PlayerAnimationController>(x => x.Init());
        InitAll<EnemyAnimationController>(x => x.Init());
        InitAll<RunSetupUI>(x => x.Init());
        InitAll<RunHUD>(x => x.Init(Fader));
    }

    public void InitTitleTexts(TitleManager title)
    {
        InitAll<TitleText>(x => x.Init(title, Fader));
    }

    private void InitAll<T>(System.Action<T> initialize) where T : MonoBehaviour
    {
        // シーン開始時にだけ配置済みの対象を収集する。実際の処理中には検索しない。
        foreach (var component in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (component.isActiveAndEnabled) initialize(component);
    }
}