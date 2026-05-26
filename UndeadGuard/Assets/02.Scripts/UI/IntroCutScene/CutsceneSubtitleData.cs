using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CutsceneSubtitleData", menuName = "UndeadGuard/Cutscene Subtitle Data")]
public sealed class CutsceneSubtitleData : ScriptableObject
{
    [SerializeField] private List<CutsceneSubtitleEntry> entries = new List<CutsceneSubtitleEntry>();

    public IReadOnlyList<CutsceneSubtitleEntry> Entries => entries;
}

[Serializable]
public sealed class CutsceneSubtitleEntry
{
    [Min(0f)] public float startTime;
    [Min(0f)] public float duration = 2f;
    [TextArea(2, 4)] public string text;

    public float EndTime => startTime + duration;
}
