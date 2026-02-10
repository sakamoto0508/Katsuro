using UnityEngine;

[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    public string PlayEffectHeal => _playEffectHeal;
    public string PlayEffectGhost => _playEffectGhost;
    public string PlayEffectBuff => _playEffectBuff;
    [SerializeField] private string _playEffectHeal;
    [SerializeField] private string _playEffectGhost;
    [SerializeField] private string _playEffectBuff;
}
