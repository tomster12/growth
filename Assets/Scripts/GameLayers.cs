using System.Collections.Generic;
using UnityEngine;

public enum GameLayer
{ FrontDecor, Terrain, Player, Tools, Foreground, Background, BackDecor };

// Single source of truth for Z position and scene layer for different world things
public class GameLayers
{
    public class GameLayerInfo
    {
        public float posZ;
        public int sceneLayer;
    };

    public static Dictionary<GameLayer, GameLayerInfo> LAYER_MAPPINGS { get; private set; } = new Dictionary<GameLayer, GameLayerInfo>()
    {
        [GameLayer.FrontDecor] = new GameLayerInfo { posZ = -1.0f, sceneLayer = LayerMask.NameToLayer("Terrain") },
        [GameLayer.Terrain] = new GameLayerInfo { posZ = 0.1f, sceneLayer = LayerMask.NameToLayer("Terrain") },
        [GameLayer.Player] = new GameLayerInfo { posZ = 1.0f, sceneLayer = LayerMask.NameToLayer("Player") },
        [GameLayer.Tools] = new GameLayerInfo { posZ = 2.0f, sceneLayer = LayerMask.NameToLayer("Player") },
        [GameLayer.Foreground] = new GameLayerInfo { posZ = 3.0f, sceneLayer = LayerMask.NameToLayer("Foreground") },
        [GameLayer.Background] = new GameLayerInfo { posZ = 4.0f, sceneLayer = LayerMask.NameToLayer("Background") },
        [GameLayer.BackDecor] = new GameLayerInfo { posZ = 5.0f, sceneLayer = LayerMask.NameToLayer("Terrain") }
    };

    public static void SetLayer(Transform transform, GameLayer layer)
    {
        GameLayerInfo info = LAYER_MAPPINGS[layer];
        transform.position = Utility.WithZ(transform.position, info.posZ);
        Utility.SetLayer(transform, info.sceneLayer);
    }

    public static Vector3 OnLayer(Vector2 pos, GameLayer layer)
    {
        GameLayerInfo info = LAYER_MAPPINGS[layer];
        return Utility.WithZ(pos, info.posZ);
    }
};
