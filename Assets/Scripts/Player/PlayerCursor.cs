using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCursor : MonoBehaviour
{
    public void InitIndicators(int count)
    {
        indicators = new Indicator[count];
        for (int i = 0; i < count; i++)
        {
            // Create object and struct
            GameObject indicatorObj = Instantiate(indicatorPfb);
            indicatorObj.name = "Cursor Indicator " + i;
            indicatorObj.transform.SetParent(transform);
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
        targetBoundsGap = 0;
        targetPos = pos;
        snapCornerPos = snap;
        if (snapCornerPos) LerpPosition(0);
    }

    public void SetTargetObject(CompositeObject obj, float gap, bool snap = false)
    {
        targetType = TargetType.Object;
        targetObject = obj;
        targetBoundsGap = gap;
        snapCornerPos = snap;
        UpdateTargetBounds();
        if (snapCornerPos) LerpObject(0);
    }

    public void SetUpwards(Vector2 up)
    {
        cursorContainer.up = up;
        if (targetType == TargetType.Object) UpdateTargetBounds();
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
    [SerializeField] private Transform promptOrganiser;
    [SerializeField] private GameObject indicatorPfb;

    [Header("Cursor Config")]
    [SerializeField] private float positionMoveSpeed = 50.0f;
    [SerializeField] private float boundsMoveSpeed = 20.0f;
    [SerializeField] private float idleCornerGap = 0.75f;
    [SerializeField] private float promptOrganiserOffset = 0.5f;
    [SerializeField] private float colorLerpSpeed = 25.0f;

    private Indicator[] indicators = new Indicator[0];
    private TargetType targetType;
    private Vector2 targetPos;
    private CompositeObject targetObject;
    private Vector2[] targetAlignedCorners;
    private Vector2 targetAlignedCentre;
    private float targetBoundsGap;
    private bool snapCornerPos;
    private Color cornerColor;

    private enum TargetType
    { Position, Object }

    private void Awake()
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
        if (targetType == TargetType.Position) LerpPosition(Time.deltaTime);
        if (targetType == TargetType.Object) LerpObject(Time.deltaTime);
        LerpColours(Time.deltaTime);
        UpdateIndicators();
    }

    private void LerpPosition(float dt)
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

    private void LerpObject(float dt)
    {
        targetAlignedCentre = (targetAlignedCorners[0] + targetAlignedCorners[1] + targetAlignedCorners[2] + targetAlignedCorners[3]) / 4;
        float rightExtent = Vector2.Distance(targetAlignedCorners[0], targetAlignedCorners[1]) / 2;

        // Lerp corners to their targets
        for (int i = 0; i < 4; i++)
        {
            Vector2 target = targetAlignedCorners[i];
            target += CORNER_OFFSETS[i] * idleCornerGap;
            if (snapCornerPos) cursorCorners[i].transform.position = target;
            else cursorCorners[i].transform.position = Vector2.Lerp(cursorCorners[i].transform.position, target, dt * boundsMoveSpeed);
        }

        // Set prompt organiser position
        promptOrganiser.position = targetAlignedCentre + (Vector2)cursorContainer.right * (rightExtent + targetBoundsGap + promptOrganiserOffset);
    }

    private void LerpColours(float dt)
    {
        // Lerp each corners colour
        for (int i = 0; i < cursorCorners.Length; i++)
        {
            cursorCorners[i].color = Color.Lerp(cursorCorners[i].color, cornerColor, dt * colorLerpSpeed);
        }
    }

    private void UpdateTargetBounds()
    {
        targetAlignedCorners = targetObject.GetAlignedBoundCorners(cursorContainer.up);
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

    private void OnDrawGizmos()
    {
        if (targetType == TargetType.Position)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.1f);
        }
        if (targetType == TargetType.Object)
        {
            for (int i = 0; i < 4; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetAlignedCorners[i], 0.1f);
            }
        }
    }

    private struct Indicator
    {
        public SpriteRenderer sr;
        public bool show;
        public Vector2 pos;
        public Color color;
    }
}
