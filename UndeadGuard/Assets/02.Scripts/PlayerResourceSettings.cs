using UnityEngine;

[CreateAssetMenu(fileName = "PlayerResourceSettings", menuName = "Battle/Player Resource Settings")]
public sealed class PlayerResourceSettings : ScriptableObject
{
    [SerializeField] private int startDarkEnergy = 5;

    public int StartDarkEnergy => startDarkEnergy;
}