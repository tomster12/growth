using System.Collections.Generic;
using UnityEngine;

public class SpriteSet : MonoBehaviour
{
    public static Sprite GetSprite(string name) => spriteMap[name];

    private static Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();

    [SerializeField] private Sprite[] sprites;

    private void OnValidate()
    {
        spriteMap ??= new Dictionary<string, Sprite>();
        foreach (Sprite sprite in sprites) spriteMap[sprite.name] = sprite;
    }
}
