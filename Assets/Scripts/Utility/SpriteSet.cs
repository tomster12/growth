using System.Collections.Generic;
using UnityEngine;

public class SpriteSet : MonoBehaviour
{
    public static Sprite GetSprite(string name) => spriteMap[name];

    [SerializeField] private Sprite[] sprites;

    private static Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();

    private void OnValidate()
    {
        if (spriteMap == null) spriteMap = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in sprites) spriteMap[sprite.name] = sprite;
    }
}
