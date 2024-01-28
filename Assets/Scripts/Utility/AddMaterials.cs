using UnityEngine;

// Used for adding a list of materials to an object.
// In unity you can have 1 from the inspector.
// Similar to functionality in OutlineController
public class AddMaterials : MonoBehaviour
{
    [ContextMenu("Add Materials")]
    public void Add()
    {
        if (spriteRenderer == null || materials == null) return;

        // Move single current material into list
        localMaterials = new Material[materials.Length + 1];
        localMaterials[0] = spriteRenderer.materials[0];

        // Instantiate added materials into list
        for (int i = 0; i < materials.Length; i++)
        {
            localMaterials[i + 1] = Instantiate(materials[i]);
            localMaterials[i + 1].name = localMaterials[i + 1].name + " (Instance)";
        }

        // Set materials to list
        spriteRenderer.materials = localMaterials;
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material[] materials;

    private Material[] localMaterials;

    private void Awake()
    {
        Add();
    }
}
