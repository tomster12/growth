using System.Collections.Generic;
using UnityEngine;

public enum GameLayer
{ Particles, FrontDecor, Terrain, Player, Foreground, Background, BackDecor };

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
        [GameLayer.Particles] = new GameLayerInfo { posZ = -2.0f, sceneLayer = LayerMask.NameToLayer("Decor") },
        [GameLayer.FrontDecor] = new GameLayerInfo { posZ = -1.0f, sceneLayer = LayerMask.NameToLayer("Decor") },
        [GameLayer.Terrain] = new GameLayerInfo { posZ = 0.1f, sceneLayer = LayerMask.NameToLayer("Terrain") },
        [GameLayer.Player] = new GameLayerInfo { posZ = 1.0f, sceneLayer = LayerMask.NameToLayer("World") },
        [GameLayer.Foreground] = new GameLayerInfo { posZ = 2.0f, sceneLayer = LayerMask.NameToLayer("World") },
        [GameLayer.Background] = new GameLayerInfo { posZ = 3.0f, sceneLayer = LayerMask.NameToLayer("World") },
        [GameLayer.BackDecor] = new GameLayerInfo { posZ = 4.0f, sceneLayer = LayerMask.NameToLayer("Decor") }
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
