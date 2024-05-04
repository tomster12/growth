using System.Runtime.CompilerServices;
using UnityEngine;
using static PlayerInteractor;
using static UnityEngine.GraphicsBuffer;

public class PlayerCursor : MonoBehaviour
{
    public void SetPosition(Vector2 position) => this.targetPos = position;

    public void SetSurround(Bounds bounds, float gap, bool snap = false)
    {
        this.surroundBounds = bounds;
        this.surroundGap = gap;
        this.surroundSnap = snap;
    }

    public void SetShowCentreIndicator(bool enabled) => showCentreIndicator = enabled;

    public void SetLimitedIndicator(Vector2 pos) => limitedIndicatorPos = pos;

    public void SetColor(Color color) => cursorColor = color;

    [Header("References")]
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer[] cursorCorners;
    [SerializeField] private SpriteRenderer centreIndicator;
    [SerializeField] private SpriteRenderer limitedCentreIndicator;

    [Header("Cursor Config")]
    [SerializeField] private float idleMoveSpeed = 50.0f;
    [SerializeField] private float surroundMoveSpeed = 20.0f;
    [SerializeField] private float cursorIdleGap = 0.75f;
    [SerializeField] private float cursorColorLerpSpeed = 25.0f;

    private Vector2[] cornerOffsets;

    private Vector2 targetPos;
    private Bounds surroundBounds;
    private float surroundGap;
    private bool surroundSnap;
    private bool showCentreIndicator;
    private Vector2 limitedIndicatorPos;
    private Color cursorColor;

    private bool ToSurroundTarget => surroundBounds != null;
    private bool ShowLimitedIndicator => limitedIndicatorPos != Vector2.zero;

    private void Awake()
    {
        cornerOffsets = new Vector2[4];
        cornerOffsets[0] = new Vector2(-1, 1);
        cornerOffsets[1] = new Vector2(1, 1);
        cornerOffsets[2] = new Vector2(-1, -1);
        cornerOffsets[3] = new Vector2(1, -1);
    }

    private void Start()
    {
        // Hide indicators
        centreIndicator.gameObject.SetActive(false);
        limitedCentreIndicator.gameObject.SetActive(false);

        // Subscribe pause event
        GameManager.onIsPausedChange += OnGameManagerIsPausedChange;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        UpdateIndicators();
        LerpCursorPosIdle(Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (GameManager.IsPaused) return;
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        /*
        // Move prompt organiser based on cursor
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
        */
    }

    private void UpdateIndicators()
    {
        // Update centre indicator
        centreIndicator.gameObject.SetActive(showCentreIndicator);
        if (showCentreIndicator) centreIndicator.transform.position = targetPos;

        // Update limited centre indicator
        limitedCentreIndicator.gameObject.SetActive(ShowLimitedIndicator);
        if (ShowLimitedIndicator) limitedCentreIndicator.transform.position = limitedIndicatorPos;
    }

    private void LerpCursorSurround(float dt)
    {
        for (int i = 0; i < 4; i++)
        {
            // Target based on bounds and gap
            Vector2 target = new Vector2(
                surroundBounds.center.x + (surroundBounds.extents.x + surroundGap) * cornerOffsets[i].x,
                surroundBounds.center.y + (surroundBounds.extents.y + surroundGap) * cornerOffsets[i].y
            );

            // Snap or lerp to target
            cursorCorners[i].transform.position = surroundSnap ? target : Vector2.Lerp(cursorCorners[i].transform.position, target, dt * surroundMoveSpeed);
        }
    }

    private void LerpCursorPosIdle(float dt)
    {
        // Set container to position
        cursorContainer.position = new Vector2(Mathf.Round(targetPos.x * 12) / 12, Mathf.Round(targetPos.y * 12) / 12);

        // Lerp corners to idle offsets
        for (int i = 0; i < cursorCorners.Length; i++)
        {
            cursorCorners[i].transform.localPosition = Vector2.Lerp(cursorCorners[i].transform.localPosition, cornerOffsets[i] * cursorIdleGap, dt * idleMoveSpeed);
        }
    }

    private void LerpCursorColour(float dt)
    {
        // Lerp each corners colour
        for (int i = 0; i < cursorCorners.Length; i++)
        {
            cursorCorners[i].color = Color.Lerp(cursorCorners[i].color, cursorColor, dt * cursorColorLerpSpeed);
        }
    }

    private void OnGameManagerIsPausedChange(bool isPaused)
    {
        cursorContainer.gameObject.SetActive(!isPaused);
        if (!isPaused)
        {
            FixedUpdateCursor();
            LateUpdateCursor();
        }
    }
}
