
using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Sprite Set", menuName = "Sprite Set")]
public class SpriteSet : ScriptableObject
{
    [SerializeField] private Sprite[] inputSpriteList;
    [SerializeField] private Sprite[] symbolSpriteList;

    private bool isInitialized = false;
    private Dictionary<string, Sprite> inputSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> symbolSprites = new Dictionary<string, Sprite>();


    private void Initialize()
    {
        foreach (Sprite sprite in inputSpriteList) inputSprites[sprite.name] = sprite;
        foreach (Sprite sprite in symbolSpriteList) symbolSprites[sprite.name] = sprite;
    }


    public Sprite GetInputSprite(string name, string suffix)
    {
        if (!isInitialized) Initialize();
        return inputSprites["input_" + name + "_" + suffix];
    }

    public Sprite GetSymbolSprite(string name)
    {
        if (!isInitialized) Initialize();
        return symbolSprites[name];
    }
}
