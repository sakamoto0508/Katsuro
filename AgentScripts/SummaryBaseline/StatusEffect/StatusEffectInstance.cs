using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffectDef Def { get; }
    public float Remaining { get; set; }
    public int Stacks { get; set; }
    public GameObject Source { get; }
    public GameObject Vfx { get; set; }

    public StatusEffectInstance(StatusEffectDef def, GameObject source)
    {
        Def = def;
        Source = source;
        Remaining = def.Duration;
        Stacks = 1;
        Vfx = null;
    }
}
