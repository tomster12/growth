using UnityEngine;

// TODO: Document why this is used
public class AddMaterials : MonoBehaviour
{
    [ContextMenu("Add Materials")]
    public void Add()
    {
        if (spriteRenderer == null || materials == null) return;

        // Instantiate materials
        localMaterials = new Material[materials.Length + 1];
        localMaterials[0] = spriteRenderer.materials[0];
        for (int i = 0; i < materials.Length; i++)
        {
            localMaterials[i + 1] = Instantiate(materials[i]);
            localMaterials[i + 1].name = localMaterials[i + 1].name + " (Instance)";
        }

        // Set materials
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
