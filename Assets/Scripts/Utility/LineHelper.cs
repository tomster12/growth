
using UnityEngine;


public enum LineFill { NONE, SOLID, DOTTED }

public class LineHelper : MonoBehaviour
{
    private static int circleVertexCount = 25;

    [SerializeField] private Material materialDottedPfb;
    [SerializeField] private Material materialSolidPfb;

    public LineFill CurrentLineFill { get; private set; } = LineFill.NONE;
    public float repeatMult = 0.8f;

    private Material materialDotted;
    private Material materialSolid;
    private LineRenderer lineRenderer;


    public void DrawCircle(Vector3 centre, float radius, Color color, float width = 0.1f, LineFill lineFill = LineFill.SOLID)
    {
        SetFill(lineFill);

        // Set variables
        lineRenderer.positionCount = circleVertexCount + 1;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Set positions
        for (int i = 0; i <= circleVertexCount; i++)
        {
            float angle = Mathf.PI * 2 * i / circleVertexCount;
            Vector3 pos = new Vector3(centre.x + radius * Mathf.Cos(angle), centre.y + radius * Mathf.Sin(angle), centre.z);
            lineRenderer.SetPosition(i, pos);
        }

        // Set repeats
        if (CurrentLineFill == LineFill.DOTTED)
        {
            float length = 2 * radius * Mathf.PI;
            materialDotted.SetFloat("_Rep", length * repeatMult);
        }
    }

    public void DrawLine(Vector3 from, Vector3 to, Color color, float width = 0.1f, LineFill lineFill = LineFill.SOLID)
    {
        SetFill(lineFill);

        // Set variables
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Set positions
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);

        // Set repeats
        if (CurrentLineFill == LineFill.DOTTED)
        {
            float length = (to - from).magnitude;
            materialDotted.SetFloat("_Rep", length * repeatMult);
        }
    }

    public void DrawCurve(Vector3 from, Vector3 to, Vector3 control, Color color, float width = 0.1f, int segmentCount = 20, LineFill lineFill = LineFill.SOLID)
    {
        SetFill(lineFill);

        // Set variables
        lineRenderer.positionCount = segmentCount + 1;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Calculate points along the curve
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 p = Utility.CalculateBezierPoint(from, control, to, t);
            lineRenderer.SetPosition(i, p);
        }
        
        // Set repeats
        if (CurrentLineFill == LineFill.DOTTED)
        {
            float length = Utility.CalculateBezierLength(from, control, to, segmentCount);
            materialDotted.SetFloat("_Rep", length * repeatMult);
        }
    }

    public void SetActive(bool isActive) => lineRenderer.enabled = isActive;


    private void Start()
    {
        // Initialize variables
        materialDotted = Instantiate(materialDottedPfb);
        materialSolid = Instantiate(materialSolidPfb);
        lineRenderer = gameObject.AddComponent<LineRenderer>();
    }

    private void SetFill(LineFill lineFill)
    {
        if (CurrentLineFill == lineFill) return;
        CurrentLineFill = lineFill;

        // Mode specific initialization
        if (CurrentLineFill == LineFill.SOLID)
        {
            lineRenderer.material = materialSolid;
        }
        else if (CurrentLineFill == LineFill.DOTTED)
        {
            lineRenderer.material = materialDotted;
        }
    }
}
