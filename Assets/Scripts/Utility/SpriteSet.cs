
using System;
using System.Collections.Generic;
using UnityEngine;


public class SpriteSet : MonoBehaviour
{
    public static SpriteSet Instance { get; private set; }

    [SerializeField] private Sprite[] spriteList;

    private bool isInitialized = false;
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();


    public Sprite GetSprite(string name)
    {
        if (!isInitialized) Initialize();
        return sprites[name];
    }


    private void Awake()
    {
        Instance = this;
    }

    private void Initialize()
    {
        foreach (Sprite sprite in spriteList) sprites[sprite.name] = sprite;
    }
}
