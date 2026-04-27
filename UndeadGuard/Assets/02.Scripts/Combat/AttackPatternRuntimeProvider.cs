using UnityEngine;

[DefaultExecutionOrder(-500)]
public class AttackPatternRuntimeProvider : MonoBehaviour
{
    [SerializeField] private AttackPatternDatabase database;

    private void Awake()
    {
        if (database != null)
            AttackPatternResolver.SetDatabase(database);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && database != null)
            AttackPatternResolver.SetDatabase(database);
    }
#endif
}