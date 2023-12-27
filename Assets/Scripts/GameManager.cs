
using System;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool IsMouseLocked => Cursor.visible;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject menu;

    private event Action<bool> OnIsPausedChange = delegate { };

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Awake()
    {
        Instance = this;
        SetPaused(false);
    }

    private void Update()
    {
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

    private void LockMouse() => Cursor.visible = false;

    private void UnlockMouse() => Cursor.visible = true;
};
