
using UnityEngine;


public class LineHelper : MonoBehaviour
{
    public enum LineMode { NONE, LINE, CIRCLE }
    
    public enum LineFill { NONE, SOLID, DOTTED }


    private static int circleVertexCount = 25;

    [SerializeField] private Material materialDottedPfb;
    [SerializeField] private Material materialSolidPfb;

    public LineMode CurrentLineMode { get; private set; } = LineMode.NONE;
    public LineFill CurrentLineFill { get; private set; } = LineFill.NONE;
    public float repeatMult = 0.8f;

    private Material materialDotted;
    private Material materialSolid;
    private LineRenderer lineRenderer;



    public void DrawCircle(Vector3 centre, float radius, Color color, LineFill lineFill = LineFill.SOLID)
    {
        SetMode(LineMode.CIRCLE);
        SetFill(lineFill);

        // Set color
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
        if (this.CurrentLineFill == LineFill.DOTTED)
        {
            float length = 2 * radius * Mathf.PI;
            materialDotted.SetFloat("_Rep", length * repeatMult);
        }
    }

    public void DrawLine(Vector3 from, Vector3 to, Color color, LineFill lineFill = LineFill.SOLID)
    {
        SetMode(LineMode.LINE);
        SetFill(lineFill);

        // Set color
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Set positions
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);

        // Set repeats
        if (this.CurrentLineFill == LineFill.DOTTED)
        {
            float length = (to - from).magnitude;
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

    private void SetMode(LineMode lineMode, float width=0.1f)
    {
        if (this.CurrentLineMode == lineMode) return;
        this.CurrentLineMode = lineMode;

        // Mode specific initialization
        if (this.CurrentLineMode == LineMode.CIRCLE)
        {
            lineRenderer.positionCount = circleVertexCount + 1;
        }
        else if (this.CurrentLineMode == LineMode.LINE)
        {
            lineRenderer.positionCount = 2;
        }

        // Global line renderer setup
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    private void SetFill(LineFill lineFill)
    {
        if (this.CurrentLineFill == lineFill) return;
        this.CurrentLineFill = lineFill;

        // Mode specific initialization
        if (this.CurrentLineFill == LineFill.SOLID)
        {
            lineRenderer.material = materialSolid;
        }
        else if (this.CurrentLineFill == LineFill.DOTTED)
        {
            lineRenderer.material = materialDotted;
        }
    }
}
