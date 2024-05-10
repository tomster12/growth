using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsMouseLocked => Cursor.visible;
    public static bool IsPaused { get; private set; }
    public static Action<bool> OnIsPausedChange = delegate { };

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject menu;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SetPaused(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!IsPaused);
    }

    private void LockMouse() => Cursor.visible = false;

    private void UnlockMouse() => Cursor.visible = true;

    private void SetPaused(bool isPaused)
    {
        GameManager.IsPaused = isPaused;
        Time.timeScale = isPaused ? 0.0f : 1.0f;
        if (isPaused) UnlockMouse();
        else LockMouse();
        menu.SetActive(isPaused);
        GameManager.OnIsPausedChange?.Invoke(isPaused);
    }
};
