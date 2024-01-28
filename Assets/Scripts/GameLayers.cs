using System.Collections.Generic;
using UnityEngine;

public enum GameLayer
{ FRONT_DECOR, TERRAIN, PLAYER, TOOLS, FOREGROUND, BACKGROUND, BACK_DECOR };

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
        [GameLayer.FRONT_DECOR] = new GameLayerInfo { posZ = -1.0f, sceneLayer = LayerMask.NameToLayer("Terrain") },
        [GameLayer.TERRAIN] = new GameLayerInfo { posZ = 0.1f, sceneLayer = LayerMask.NameToLayer("Terrain") },
        [GameLayer.PLAYER] = new GameLayerInfo { posZ = 1.0f, sceneLayer = LayerMask.NameToLayer("Player") },
        [GameLayer.TOOLS] = new GameLayerInfo { posZ = 2.0f, sceneLayer = LayerMask.NameToLayer("Player") },
        [GameLayer.FOREGROUND] = new GameLayerInfo { posZ = 3.0f, sceneLayer = LayerMask.NameToLayer("Foreground") },
        [GameLayer.BACKGROUND] = new GameLayerInfo { posZ = 4.0f, sceneLayer = LayerMask.NameToLayer("Background") },
        [GameLayer.BACK_DECOR] = new GameLayerInfo { posZ = 5.0f, sceneLayer = LayerMask.NameToLayer("Terrain") }
    };

    public static void SetLayer(Transform transform, GameLayer layer)
    {
        GameLayerInfo info = LAYER_MAPPINGS[layer];
        transform.position = new Vector3(transform.position.x, transform.position.y, info.posZ);
        Utility.SetLayer(transform, info.sceneLayer);
    }
};
