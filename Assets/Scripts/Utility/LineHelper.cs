
using UnityEditor.SceneManagement;
using UnityEngine;


public class LineHelper : MonoBehaviour
{
    public enum LineMode { NONE, LINE, CIRCLE }
    public enum LineFill { NONE, SOLID, DOTTED }

    public LineMode lineMode { get; private set; } = LineMode.NONE;
    public LineFill lineFill { get; private set; } = LineFill.NONE;

    private LineRenderer lineRenderer;


    private void Awake()
    {
        // Initialize line renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
    }


    public void DrawCircle(Vector2 centre, float radius, Color color)
    {
        if (lineMode != LineMode.CIRCLE) SetMode(LineMode.CIRCLE);
    }

    public void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        if (lineMode != LineMode.LINE) SetMode(LineMode.LINE);
    }


    private void SetMode(LineMode lineMode)
    {
        if (this.lineMode == lineMode) return;
        this.lineMode = lineMode;
    }

    public void SetActive(bool isActive)
    {
        if (gameObject.activeSelf == isActive) return;
        gameObject.SetActive(isActive);
        lineRenderer.enabled = isActive;
    }
}
