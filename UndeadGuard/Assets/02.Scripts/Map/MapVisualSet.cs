using UnityEngine;

[CreateAssetMenu(fileName = "MapVisualSet", menuName = "Map/Map Visual Set")]
public sealed class MapVisualSet : ScriptableObject
{
    [Header("Preview Prefabs")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private GameObject revivalAltarPrefab;

    [Header("Editor Icons")]
    [SerializeField] private Texture2D noneIcon;
    [SerializeField] private Texture2D wallIcon;
    [SerializeField] private Texture2D coreIcon;
    [SerializeField] private Texture2D revivalAltarIcon;

    public GameObject GroundPrefab => groundPrefab;
    public GameObject WallPrefab => wallPrefab;
    public GameObject CorePrefab => corePrefab;
    public GameObject RevivalAltarPrefab => revivalAltarPrefab;

    public Texture2D NoneIcon => noneIcon;
    public Texture2D WallIcon => wallIcon;
    public Texture2D CoreIcon => coreIcon;
    public Texture2D RevivalAltarIcon => revivalAltarIcon;
}