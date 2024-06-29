using System.Linq;
using UnityEngine;

public class OutlineController : MonoBehaviour
{
    public enum Mode
    { OutlineFull, Disabled }

    public Color OutlineColor
    {
        get => outlineFill.GetColor("_OutlineColor");
        set
        {
            outlineMask.SetColor("_OutlineColor", value);
            outlineFill.SetColor("_OutlineColor", value);
        }
    }

    private Renderer[] renderers;
    private Material outlineMask, outlineFill;
    private bool isInitialized;

    private void Awake() => InitializeController();

    private void OnEnable() => AddMaterials();

    private void OnDisable() => RemoveMaterials();

    private void OnDestroy() => DeinitializeController();

    [ContextMenu("Initialize Controller")]
    private void InitializeController()
    {
        if (isInitialized) return;

        renderers = GetComponentsInChildren<Renderer>();
        outlineMask = Instantiate((Material)Resources.Load("Materials/OutlineMask", typeof(Material)));
        outlineFill = Instantiate((Material)Resources.Load("Materials/OutlineFill", typeof(Material)));
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
