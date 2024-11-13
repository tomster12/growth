using UnityEngine;

[CreateAssetMenu(fileName = "WorldFeatureType", menuName = "World Feature/Type")]
public class WorldFeatureType : ScriptableObject
{
    public GameObject prefab;
    public GameLayer gameLayer;
};
