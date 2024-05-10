public class PartEquipable : Part
{
    public PartControllable Controllable => Composable.GetPart<PartControllable>();

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartControllable>();
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }
}
