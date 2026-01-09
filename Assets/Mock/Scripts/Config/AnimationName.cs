using UnityEngine;

[CreateAssetMenu(fileName = "AnimName", menuName = "ScriptableObjects/Player/AnimName")]
public class AnimationName : ScriptableObject
{
    public string IsDrawingSword => _isDrawingSword;
    [SerializeField] string _isDrawingSword = "";
}
