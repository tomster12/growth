using System.Linq;
using UnityEngine;

public class OutlineController2D : MonoBehaviour
{
    public Color OutlineColor
    {
        get => outlineColor;
        set
        {
            outlineColor = value;
            UpdateProperties();
        }
    }

    public float OutlineWidth
    {
        get => outlineWidth;
        set
        {
            outlineWidth = value;
            UpdateProperties();
        }
    }

    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.3f;

    private Renderer[] renderers;
    private Material outlineMask, outlineFill;
    private bool isInitialized;

    private void Awake() => InitializeController();

    private void OnEnable() => AddMaterials();

    private void OnDisable() => RemoveMaterials();

    private void OnDestroy() => DeinitializeController();

    private void OnValidate() => UpdateProperties();

    private void UpdateProperties()
    {
        if (!isInitialized) return;
        outlineMask.SetColor("_OutlineColor", OutlineColor);
        outlineFill.SetColor("_OutlineColor", OutlineColor);
        outlineFill.SetFloat("_OutlineWidth", outlineWidth);
    }

    [ContextMenu("Initialize Controller")]
    private void InitializeController()
    {
        if (isInitialized) return;

        renderers = GetComponentsInChildren<Renderer>();
        outlineMask = Instantiate(AssetManager.GetMaterial("ScaleOutlineMask"));
        outlineFill = Instantiate(AssetManager.GetMaterial("ScaleOutlineFill"));
        outlineMask.name = "OutlineMask (Instance)";
        outlineFill.name = "OutlineFill (Instance)";
        isInitialized = true;
    }

    [ContextMenu("Add Materials")]
    private void AddMaterials()
    {
        if (!isInitialized) InitializeController();

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMask);
            materials.Add(outlineFill);
            renderer.materials = materials.ToArray();
        }

        UpdateProperties();
    }

    [ContextMenu("Remove Materials")]
    private void RemoveMaterials()
    {
        if (!isInitialized) return;

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMask);
            materials.Remove(outlineFill);
            renderer.materials = materials.ToArray();
        }
    }

    [ContextMenu("Deinitialize Controller")]
    private void DeinitializeController()
    {
        if (!isInitialized) return;

        RemoveMaterials();
        DestroyImmediate(outlineMask);
        DestroyImmediate(outlineFill);
        isInitialized = false;
    }
}
