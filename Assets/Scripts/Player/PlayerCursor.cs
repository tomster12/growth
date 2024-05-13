using UnityEngine;

public class PlayerCursor : MonoBehaviour
{
    public void InitIndicators(int count)
    {
        indicators = new Indicator[count];
        for (int i = 0; i < count; i++)
        {
            // Create object and struct
            GameObject indicatorObj = Instantiate(indicatorPfb);
            indicators[i] = new Indicator
            {
                sr = indicatorObj.GetComponent<SpriteRenderer>(),
                show = false,
                pos = Vector2.zero,
                color = Color.white
            };

            // Set to default configuration
            indicators[i].sr.gameObject.SetActive(indicators[i].show);
            indicators[i].sr.transform.position = indicators[i].pos;
            indicators[i].sr.color = indicators[i].color;
        }
    }

    public void SetTargetPosition(Vector2 pos, bool snap = false)
    {
        targetType = TargetType.Position;
        targetPos = pos;
        snapCornerPos = snap;
        if (snapCornerPos) LerpCornersPosition(0);
    }

    public void SetTargetBounds(Bounds bounds, float gap, bool snap = false)
    {
        targetType = TargetType.Bounds;
        targetBounds = bounds;
        targetBoundsGap = gap;
        snapCornerPos = snap;
        if (snapCornerPos) LerpCornersBounds(0);
    }

    public void SetIndicator(int index, bool show, Vector2 pos = default)
    {
        indicators[index].show = show;
        indicators[index].pos = pos;
    }

    public void SetCornerColor(Color color) => cornerColor = color;

    public void SetIndicatorColor(int index, Color color) => indicators[index].color = color;

    private static Vector2[] CORNER_OFFSETS = new Vector2[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };

    [Header("References")]
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer[] cursorCorners;
    [SerializeField] private GameObject indicatorPfb;

    [Header("Cursor Config")]
    [SerializeField] private float positionMoveSpeed = 50.0f;
    [SerializeField] private float boundsMoveSpeed = 20.0f;
    [SerializeField] private float idleCornerGap = 0.75f;
    [SerializeField] private float colorLerpSpeed = 25.0f;

    private Indicator[] indicators;
    private TargetType targetType;
    private Vector2 targetPos;
    private Bounds targetBounds;
    private float targetBoundsGap;
    private bool snapCornerPos;
    private Color cornerColor;

    private enum TargetType
    { Position, Bounds }

    private void Start()
    {
        // Set corner colors
        cornerColor = Color.white;
        for (int i = 0; i < cursorCorners.Length; i++) cursorCorners[i].color = cornerColor;

        // Subscribe pause event
        GameManager.OnIsPausedChange += OnGameManagerIsPausedChange;
    }

    private void LateUpdate()
    {
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        if (GameManager.IsPaused) return;
        if (targetType == TargetType.Bounds) LerpCornersBounds(Time.deltaTime);
        if (targetType == TargetType.Position) LerpCornersPosition(Time.deltaTime);
        LerpCornersColors(Time.deltaTime);
        UpdateIndicators();
    }

    private void LerpCornersPosition(float dt)
    {
        // Set container to position
        cursorContainer.position = new Vector2(Mathf.Round(targetPos.x * 12) / 12, Mathf.Round(targetPos.y * 12) / 12);

        // Lerp corners to idle offsets
        for (int i = 0; i < cursorCorners.Length; i++)
        {
            if (snapCornerPos) cursorCorners[i].transform.localPosition = CORNER_OFFSETS[i] * idleCornerGap;
            else cursorCorners[i].transform.localPosition = Vector2.Lerp(cursorCorners[i].transform.localPosition, CORNER_OFFSETS[i] * idleCornerGap, dt * positionMoveSpeed);
        }
    }

    private void LerpCornersBounds(float dt)
    {
        // Set container to centre
        cursorContainer.position = new Vector2(Mathf.Round(targetBounds.center.x * 12) / 12, Mathf.Round(targetBounds.center.y * 12) / 12);

        for (int i = 0; i < 4; i++)
        {
            // Target based on bounds and gap
            Vector2 target = new Vector2(
                targetBounds.center.x + (targetBounds.extents.x + targetBoundsGap) * CORNER_OFFSETS[i].x,
                targetBounds.center.y + (targetBounds.extents.y + targetBoundsGap) * CORNER_OFFSETS[i].y
            );

            // Lerp corners to their targets
            if (snapCornerPos) cursorCorners[i].transform.position = target;
            else cursorCorners[i].transform.position = Vector2.Lerp(cursorCorners[i].transform.position, target, dt * boundsMoveSpeed);
        }
    }

    private void LerpCornersColors(float dt)
    {
        // Lerp each corners colour
        for (int i = 0; i < cursorCorners.Length; i++)
        {
            cursorCorners[i].color = Color.Lerp(cursorCorners[i].color, cornerColor, dt * colorLerpSpeed);
        }
    }

    private void UpdateIndicators()
    {
        // Update all indicators
        for (int i = 0; i < indicators.Length; i++)
        {
            indicators[i].sr.gameObject.SetActive(indicators[i].show);

            // Set position and color
            if (indicators[i].show)
            {
                indicators[i].sr.transform.position = indicators[i].pos;
                indicators[i].sr.color = indicators[i].color;
            }
        }
    }

    private void OnGameManagerIsPausedChange(bool isPaused)
    {
        cursorContainer.gameObject.SetActive(!isPaused);
        if (!isPaused) LateUpdateCursor();
    }

    private struct Indicator
    {
        public SpriteRenderer sr;
        public bool show;
        public Vector2 pos;
        public Color color;
    }
}
