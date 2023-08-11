
using System;
using System.Collections.Generic;
using UnityEngine;


public class SpriteSet : MonoBehaviour
{
    public static SpriteSet instance;

    [SerializeField] private Sprite[] spriteList;

    private bool isInitialized = false;
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();


    private void Awake()
    {
        instance = this;
    }

    private void Initialize()
    {
        foreach (Sprite sprite in spriteList) sprites[sprite.name] = sprite;
    }


    public Sprite GetSprite(string name)
    {
        if (!isInitialized) Initialize();
        return sprites[name];
    }
}
