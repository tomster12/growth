using System;
using System.Linq;
using UnityEngine;

public class OutlineController : MonoBehaviour
{
    [Serializable]
    public enum Mode
    {
        Scale,
        Pixel
    }

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

    public Mode OutlineMode
    {
        get => outlineMode;
        set
        {
            if (value == outlineMode) return;
            UpdateProperties();
        }
    }

    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.3f;
    [SerializeField] private Mode outlineMode = Mode.Scale;

    private bool isInitialized;
    private Renderer[] renderers;
    private Material scaleOutlineMask, scaleOutlineFill;
    private Material pixelOutline;

    private void Awake() => InitializeController();

    private void OnEnable() => AddMaterials();

    private void OnDisable() => RemoveMaterials();

    private void OnDestroy() => DeinitializeController();

    private void OnValidate() => UpdateProperties();

    [ContextMenu("Initialize Controller")]
    private void InitializeController()
    {
        if (isInitialized) return;

        renderers = GetComponentsInChildren<Renderer>();

        if (outlineMode == Mode.Pixel)
        {
            pixelOutline = Instantiate(AssetManager.GetMaterial("PixelOutline"));
            pixelOutline.name = "PixelOutline (Instance)";
        }
        else if (outlineMode == Mode.Scale)
        {
            scaleOutlineMask = Instantiate(AssetManager.GetMaterial("ScaleOutlineMask"));
            scaleOutlineFill = Instantiate(AssetManager.GetMaterial("ScaleOutlineFill"));
            scaleOutlineMask.name = "ScaleOutlineMask (Instance)";
            scaleOutlineFill.name = "ScaleOutlineFill (Instance)";
        }

        isInitialized = true;
    }

    [ContextMenu("Add Materials")]
    private void AddMaterials()
    {
        if (!isInitialized) InitializeController();

        if (outlineMode == Mode.Pixel)
        {
            foreach (var renderer in renderers)
            {
                // TODO: See if need to be added or replaced
                var materials = renderer.sharedMaterials.ToList();
                materials.Add(pixelOutline);
                renderer.materials = materials.ToArray();
            }
        }
        else if (outlineMode == Mode.Scale)
        {
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials.ToList();
                materials.Add(scaleOutlineMask);
                materials.Add(scaleOutlineFill);
                renderer.materials = materials.ToArray();
            }
        }

        UpdateProperties();
    }

    private void UpdateProperties()
    {
        if (!isInitialized) return;

        bool toUpdate = false;
        if (outlineMode == Mode.Pixel && pixelOutline == null) toUpdate = true;
        if (outlineMode == Mode.Scale && (scaleOutlineMask == null || scaleOutlineFill == null)) toUpdate = true;
        if (toUpdate)
        {
            DeinitializeController();
            InitializeController();
            AddMaterials();
        }

        if (outlineMode == Mode.Pixel)
        {
            pixelOutline.SetColor("_OutlineColor", OutlineColor);
            pixelOutline.SetFloat("_OutlineThickness", outlineWidth);
        }
        else if (outlineMode == Mode.Scale)
        {
            scaleOutlineMask.SetColor("_OutlineColor", OutlineColor);
            scaleOutlineFill.SetColor("_OutlineColor", OutlineColor);
            scaleOutlineFill.SetFloat("_OutlineWidth", outlineWidth);
        }
    }

    [ContextMenu("Remove Materials")]
    private void RemoveMaterials()
    {
        if (!isInitialized) return;

        if (outlineMode == Mode.Pixel)
        {
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials.ToList();
                materials.Remove(pixelOutline);
                renderer.materials = materials.ToArray();
            }
        }
        else
        {
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials.ToList();
                materials.Remove(scaleOutlineMask);
                materials.Remove(scaleOutlineFill);
                renderer.materials = materials.ToArray();
            }
        }
    }

    [ContextMenu("Deinitialize Controller")]
    private void DeinitializeController()
    {
        if (!isInitialized) return;

        RemoveMaterials();

        if (scaleOutlineMask != null) DestroyImmediate(scaleOutlineMask);
        if (scaleOutlineFill != null) DestroyImmediate(scaleOutlineFill);
        if (pixelOutline != null) DestroyImmediate(pixelOutline);

        isInitialized = false;
    }
}
