
using System.Collections.Generic;
using UnityEngine;


public enum Layer { FRONT_DECOR, TERRAIN, PLAYER, TOOLS, FOREGROUND, BACKGROUND, BACK_DECOR };

public class Layers
{

    public class LayerInfo
    {
        public float posZ;
        public int sceneLayer;
    };

    public static Dictionary<Layer, LayerInfo> LAYER_MAPPINGS { get; private set; } = new Dictionary<Layer, LayerInfo>()
    {
        [Layer.FRONT_DECOR] = new LayerInfo { posZ=-1.0f, sceneLayer=LayerMask.NameToLayer("Terrain") },
        [Layer.TERRAIN] = new LayerInfo { posZ=0.1f, sceneLayer=LayerMask.NameToLayer("Terrain") },
        [Layer.PLAYER] = new LayerInfo { posZ=1.0f, sceneLayer=LayerMask.NameToLayer("Player") },
        [Layer.TOOLS] = new LayerInfo { posZ=2.0f, sceneLayer=LayerMask.NameToLayer("Player") },
        [Layer.FOREGROUND] = new LayerInfo { posZ=3.0f, sceneLayer=LayerMask.NameToLayer("Foreground") },
        [Layer.BACKGROUND] = new LayerInfo { posZ=4.0f, sceneLayer=LayerMask.NameToLayer("Background") },
        [Layer.BACK_DECOR] = new LayerInfo { posZ=5.0f, sceneLayer=LayerMask.NameToLayer("Terrain") },
    };

    public static void SetLayer(Transform transform, Layer layer)
    {
        LayerInfo info = LAYER_MAPPINGS[layer];
        transform.position = new Vector3(transform.position.x, transform.position.y, info.posZ);
        Utility.SetLayer(transform, info.sceneLayer);
    }
};
