using UnityEngine;

public enum ToolType
{ None, Cutter };

public class PartEquipable : Part
{
    public ToolType ToolType => toolType;
    public bool IsEquipped { get; private set; }
    public bool CanEquip => !IsEquipped && canEquip;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartPhysical>();
    }

    public bool StartEquipping()
    {
        if (!CanEquip)
        {
            Debug.LogWarning("Trying to start equipping equipped part");
            return false;
        }

        // Set variables
        IsEquipped = true;
        GameLayers.SetLayer(Composable.Transform, GameLayer.Tools);
        Physical.SetEnabled(false);

        return true;
    }

    public bool StopEquipping()
    {
        if (!IsEquipped)
        {
            Debug.LogWarning("Trying to stop equipping unequipped part");
            return false;
        }

        // Reset variables
        IsEquipped = false;
        GameLayers.SetLayer(Composable.Transform, GameLayer.Foreground);
        Physical.SetEnabled(true);

        // Add force to physical with grip direction
        Vector2 unequipForceDir = unequipDir * unequipForce;
        unequipForceDir = Vector2.ClampMagnitude(unequipForceDir, unequipForceMax);
        Physical.RB.AddForce(unequipForceDir, ForceMode2D.Impulse);

        return true;
    }

    public void SetGrip(Vector2 handPos, Vector2 handDir)
    {
        if (!IsEquipped) throw new System.Exception("Cannot set grip on unequipped part");

        // Set rotation first
        float handAngle = Vector2.SignedAngle(Vector2.up, handDir);
        Composable.Transform.rotation = Quaternion.Euler(0.0f, 0.0f, handAngle);

        // Set grip position considering angle transform
        Vector2 gripPos = handPos + (Vector2)Composable.Transform.TransformDirection(gripOffset);
        unequipDir = gripPos - oldGripPos;
        oldGripPos = gripPos;
        Composable.Transform.position = new Vector3(gripPos.x, gripPos.y, GameLayers.LAYER_MAPPINGS[GameLayer.Tools].posZ);
    }

    public void SetCanEquip(bool canEquip) => this.canEquip = canEquip;

    public void SetGripOffset(Vector2 gripOffset) => this.gripOffset = gripOffset;

    [Header("Config")]
    [SerializeField] private Vector2 gripOffset = Vector2.zero;
    [SerializeField] private float unequipForce = 2.0f;
    [SerializeField] private float unequipForceMax = 8.0f;
    [SerializeField] private ToolType toolType = ToolType.None;
    private bool canEquip = true;
    private Vector2 oldGripPos;
    private Vector2 unequipDir;

    private PartPhysical Physical => Composable.GetPart<PartPhysical>();
}
