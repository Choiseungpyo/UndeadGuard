using UnityEngine;

[CreateAssetMenu(fileName = "StructureDefinition", menuName = "Battle/Structure Definition")]
public sealed class StructureDefinition : ScriptableObject
{
    [SerializeField] private StructureType structureType = StructureType.Core;
    [SerializeField] private int maxHp = 20;
    [SerializeField] private bool blocksMovement = true;
    [SerializeField] private bool enablesResurrection = false;

    public StructureType StructureType => structureType;
    public int MaxHp => maxHp;
    public bool BlocksMovement => blocksMovement;
    public bool EnablesResurrection => enablesResurrection;
}