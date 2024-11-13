public class WorldFeatureInstance
{
    public WorldFeatureType Type { get; private set; }
    public IWorldFeature Feature { get; private set; }

    public WorldFeatureInstance(WorldFeatureType type, IWorldFeature feature)
    {
        Type = type;
        Feature = feature;
    }
}
