using System;
using UnityEngine;

[Serializable]
public struct MapCellData
{
    public Vector2Int position;
    public StructureType objectType;
    public SpawnZoneType spawnZone;

    public bool IsBlocked
    {
        get { return objectType == StructureType.Wall; }
    }
}