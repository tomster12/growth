
using System;
using UnityEditor;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool IsMouseLocked => UnityEngine.Cursor.visible;
    
    [SerializeField] private PlayerController playerController;    
    [SerializeField] private GameObject menu;

    private Action<bool> OnIsPausedChange;


    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void SubscribeOnIsPausedChange(Action<bool> action) => OnIsPausedChange += action;
    
    public void UnsubscribeOnIsPausedChange(Action<bool> action) => OnIsPausedChange += action;


    private void Awake()
    {
        Instance = this;
        SetPaused(false);
    }

    private void Update()
    {
        // Pause on escape
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!IsPaused);
    }

    private void SetPaused(bool isPaused)
    {
        GameManager.IsPaused = isPaused;
        Time.timeScale = isPaused ? 0.0f : 1.0f;
        if (isPaused) UnlockMouse();
        else LockMouse();
        menu.SetActive(isPaused);
        OnIsPausedChange?.Invoke(isPaused);
    }

    private void LockMouse() => UnityEngine.Cursor.visible = false;

    private void UnlockMouse() => UnityEngine.Cursor.visible = true;
};
