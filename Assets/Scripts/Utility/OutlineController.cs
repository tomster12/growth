using System.Linq;
using UnityEngine;

public class OutlineController : MonoBehaviour
{
    public enum Mode
    { OutlineFull, Disabled }

    private Renderer[] renderers;
    private Material outlineMask, outlineFill;
    private bool isInitialized;
    private bool needsUpdate;

    private void Awake() => InitializeController();

    private void OnEnable() => AddMaterials();

    private void OnValidate()
    {
        needsUpdate = true;
    }

    private void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            UpdateMaterials();
        }
    }

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
        needsUpdate = true;
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

    [ContextMenu("Update Materials")]
    private void UpdateMaterials()
    {
        if (!isInitialized) return;

        // TODO: Why is this commented out

        //if (spriteRenderer == null || materials == null) return;

        //// Instantiate materials
        //localMaterials = new Material[materials.Length + 1];
        //localMaterials[0] = spriteRenderer.materials[0];
        //for (int i = 0; i < materials.Length; i++)
        //{
        //    localMaterials[i + 1] = Instantiate(materials[i]);
        //    localMaterials[i + 1].name = localMaterials[i + 1].name + " (Instance)";
        //}

        //// Set materials
        //spriteRenderer.materials = localMaterials;
    }
}
