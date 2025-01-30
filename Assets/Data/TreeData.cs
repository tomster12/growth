using UnityEngine;

[CreateAssetMenu(fileName = "Tree Data", menuName = "Tree Data", order = 0)]
public class TreeData : ScriptableObject
{
    public SampleRange Count = new SampleRange(8, 10, SampleTypes.UNIFORM);

    public SampleRange WidthInitial = new SampleRange(0.5f, 0.7f, SampleTypes.GAUSSIAN);
    public SampleRange WidthAdd = new SampleRange(-0.05f, 0.05f, SampleTypes.UNIFORM);
    public float WidthDecay = 0.95f;

    public SampleRange LengthInitial = new SampleRange(0.5f, 0.7f, SampleTypes.UNIFORM);
    public SampleRange LengthAdd = new SampleRange(-0.05f, 0.05f, SampleTypes.UNIFORM);
    public float LengthDecay = 0.99f;

    public SampleRange AngleInitial = new SampleRange(-5, 5, SampleTypes.UNIFORM);
    public SampleRange AngleAdd = new SampleRange(-1, 1, SampleTypes.UNIFORM);
    public float AngleDecay = 1.02f;

    public float BranchChance = 0.1f;
    public SampleRange BranchAngleAdd = new SampleRange(10, 15, SampleTypes.UNIFORM);

    public Color ColorMin = new(0.2f, 0.1f, 0.0f);
    public Color ColorMax = new(0.4f, 0.2f, 0.1f);

    public float WindStrength = 3.0f;
    public float BranchResistance = 0.5f;
}
