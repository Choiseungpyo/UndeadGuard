using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택 여부에 따라 아웃라인 머티리얼을 붙였다 뗐다 하는 컴포넌트.
/// </summary>
public sealed class UnitOutlineToggle : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;

    private Renderer targetRenderer;
    private Material[] normalMaterials;
    private Material[] selectedMaterials;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer == null || outlineMaterial == null)
        {
            return;
        }

        Material[] currentMaterials = targetRenderer.sharedMaterials;
        List<Material> normalList = new List<Material>();

        for (int i = 0; i < currentMaterials.Length; i++)
        {
            Material mat = currentMaterials[i];
            if (mat != null && mat != outlineMaterial)
            {
                normalList.Add(mat);
            }
        }

        normalMaterials = normalList.ToArray();

        selectedMaterials = new Material[normalMaterials.Length + 1];
        for (int i = 0; i < normalMaterials.Length; i++)
        {
            selectedMaterials[i] = normalMaterials[i];
        }

        selectedMaterials[selectedMaterials.Length - 1] = outlineMaterial;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (targetRenderer == null || normalMaterials == null || selectedMaterials == null)
        {
            return;
        }

        targetRenderer.sharedMaterials = selected ? selectedMaterials : normalMaterials;
    }
}