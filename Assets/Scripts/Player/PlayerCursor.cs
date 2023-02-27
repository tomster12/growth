
using UnityEngine;


public class PlayerCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Transform container;
    [SerializeField] private SpriteRenderer cornerTL, cornerTR, cornerBL, cornerBR;

    [Header("Config")]
    [SerializeField] private Color idleColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private float idleDistance = 0.75f;
    [SerializeField] private float idleMovementSpeed = 50.0f;
    [SerializeField] private Color hoverColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private float hoverMovementSpeed = 20.0f;
    [SerializeField] private float hoverGap = 0.2f;
    [SerializeField] private float colorLerpSpeed = 3.0f;


    private void Start() => Focus();


    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) Focus();
    }

    private void FixedUpdate()
    {
        //Surround hover object
        if (interactor.hoverInfo.isHovering)
        {
            Bounds b = interactor.hoverInfo.hoveredIHoverable.GetBounds();

            // Move to centre
            Vector2 targetPos = b.center;
            container.position = Vector2.Lerp(container.position, targetPos, Time.deltaTime * hoverMovementSpeed);

            // Surround with corners
            cornerTL.transform.position = Vector2.Lerp(cornerTL.transform.position, new Vector2(b.center.x - b.extents.x - hoverGap, b.center.y + b.extents.y + hoverGap), Time.deltaTime * hoverMovementSpeed);
            cornerTR.transform.position = Vector2.Lerp(cornerTR.transform.position, new Vector2(b.center.x + b.extents.x + hoverGap, b.center.y + b.extents.y + hoverGap), Time.deltaTime * hoverMovementSpeed);
            cornerBL.transform.position = Vector2.Lerp(cornerBL.transform.position, new Vector2(b.center.x - b.extents.x - hoverGap, b.center.y - b.extents.y - hoverGap), Time.deltaTime * hoverMovementSpeed);
            cornerBR.transform.position = Vector2.Lerp(cornerBR.transform.position, new Vector2(b.center.x + b.extents.x + hoverGap, b.center.y - b.extents.y - hoverGap), Time.deltaTime * hoverMovementSpeed);

            // Set colours
            cornerTL.color = Color.Lerp(cornerTL.color, hoverColor, Time.deltaTime * colorLerpSpeed);
            cornerTR.color = Color.Lerp(cornerTR.color, hoverColor, Time.deltaTime * colorLerpSpeed);
            cornerBL.color = Color.Lerp(cornerBL.color, hoverColor, Time.deltaTime * colorLerpSpeed);
            cornerBR.color = Color.Lerp(cornerBR.color, hoverColor, Time.deltaTime * colorLerpSpeed);
        }

        // Is idling
        else
        {
            // Move to mouse
            Vector2 targetPos = interactor.hoverPos;
            //container.position = Vector2.Lerp(container.position, targetPos, Time.deltaTime * idleMovementSpeed);
            container.position = targetPos;

            // Spread out corners
            cornerTL.transform.localPosition = Vector2.Lerp(cornerTL.transform.localPosition, new Vector2(-idleDistance, idleDistance), Time.deltaTime * idleMovementSpeed);
            cornerTR.transform.localPosition = Vector2.Lerp(cornerTR.transform.localPosition, new Vector2(idleDistance, idleDistance), Time.deltaTime * idleMovementSpeed);
            cornerBL.transform.localPosition = Vector2.Lerp(cornerBL.transform.localPosition, new Vector2(-idleDistance, -idleDistance), Time.deltaTime * idleMovementSpeed);
            cornerBR.transform.localPosition = Vector2.Lerp(cornerBR.transform.localPosition, new Vector2(idleDistance, -idleDistance), Time.deltaTime * idleMovementSpeed);

            // Set colours
            cornerTL.color = Color.Lerp(cornerTL.color, idleColor, Time.deltaTime * colorLerpSpeed);
            cornerTR.color = Color.Lerp(cornerTR.color, idleColor, Time.deltaTime * colorLerpSpeed);
            cornerBL.color = Color.Lerp(cornerBL.color, idleColor, Time.deltaTime * colorLerpSpeed);
            cornerBR.color = Color.Lerp(cornerBR.color, idleColor, Time.deltaTime * colorLerpSpeed);
        }
    }


    void Focus()
    {
        Cursor.visible = false;
    }
}
