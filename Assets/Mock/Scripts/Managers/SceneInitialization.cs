using UnityEngine;

/// <summary>Called by the title/game entry point. Initializes authored services before gameplay.</summary>
public static class SceneInitialization
{
    public static void Init()
    {
        InitAll<AudioManager>(x => x.Init());
        InitAll<GlobalFader>(x => x.Init());
        InitAll<LoadSceneManager>(x => x.Init());
        InitAll<HitStopManager>(x => x.Init());
        InitAll<FinalBlowManager>(x => x.Init());
        InitAll<PlayerDeadManager>(x => x.Init());
        InitAll<InputBuffer>(x => x.Init());
        InitAll<PlayerAnimationController>(x => x.Init());
        InitAll<EnemyAnimationController>(x => x.Init());
        InitAll<RunSetupUI>(x => x.Init());
        InitAll<RunHUD>(x => x.Init());
        InitAll<DamageNumbers>(x => x.Init());
    }

    private static void InitAll<T>(System.Action<T> initialize) where T : MonoBehaviour
    {
        // Scene entry only; no per-frame searching. Ignore disabled legacy objects.
        foreach (var component in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (component.isActiveAndEnabled) initialize(component);
    }
}
