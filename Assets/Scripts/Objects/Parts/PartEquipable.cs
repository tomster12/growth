using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;

public class PartEquipable : Part
{
    public bool IsEquipped { get; private set; }
    public bool CanEquip => canEquip;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartPhysical>();
    }

    public void StartEquipping()
    {
        if (!CanEquip) throw new System.Exception("Cannot equip part");
        IsEquipped = true;
        GameLayers.SetLayer(Composable.transform, GameLayer.Tools);
        Physical.SetEnabled(false);
    }

    public void StopEquipping()
    {
        if (!IsEquipped) throw new System.Exception("Cannot unequip part");
        IsEquipped = false;
        GameLayers.SetLayer(Composable.transform, GameLayer.Foreground);
        Physical.SetEnabled(true);

        // Add force to physical with grip direction
        Vector2 unequipForceDir = unequipDir.normalized * unequipForce;
        Physical.RB.AddForce(unequipForceDir, ForceMode2D.Impulse);
    }

    public void SetGrip(Vector2 handPos, Vector2 handDir)
    {
        if (!IsEquipped) throw new System.Exception("Cannot set grip on unequipped part");

        // Set rotation first
        float handAngle = Vector2.SignedAngle(Vector2.up, handDir);
        Composable.transform.rotation = Quaternion.Euler(0.0f, 0.0f, handAngle);
        unequipDir = handDir;

        // Set grip position considering angle transform
        Vector2 gripPos = handPos + (Vector2)Composable.transform.TransformDirection(gripOffset);
        Composable.transform.position = new Vector3(gripPos.x, gripPos.y, GameLayers.LAYER_MAPPINGS[GameLayer.Tools].posZ);
    }

    public void SetCanEquip(bool canEquip) => this.canEquip = canEquip;

    public void SetGripOffset(Vector2 gripOffset) => this.gripOffset = gripOffset;

    [Header("Config")]
    [SerializeField] private Vector2 gripOffset = Vector2.zero;
    [SerializeField] private float unequipForce = 2.0f;

    private bool canEquip = true;
    private Vector2 unequipDir;

    private PartPhysical Physical => Composable.GetPart<PartPhysical>();
}
