
using GK;
using System;
using System.Collections.Generic;
using UnityEditor.U2D.Path;
using UnityEngine;


public class PlanetPolygonGenerator : MonoBehaviour
{

    [Serializable]
    public class PlanetShapeInfo
    {
        public int vertexCount;
        public NoiseData[] noiseData;
    }


    // --- Cache / Internal ---
    private PolygonCollider2D _outsidePolygon;
    private PlanetShapeInfo _shapeInfo;

    // --- Output ---
    private Vector2[] _points;
    public Vector2[] points => _points;


    #region Generation Pipeline

    public void Generate(PolygonCollider2D outsidePolygon, PlanetShapeInfo shapeInfo)
    {
        // Clear and cache
        ClearInternal();
        ClearOutput();
        _outsidePolygon = outsidePolygon;
        _shapeInfo = shapeInfo;

        // Generate points in a circle
        _points = new Vector2[_shapeInfo.vertexCount];
        for (int i = 0; i < _points.Length; i++)
        {
            // Add each noise to value
            float pct = (float)i / _points.Length;
            float value = 0;
            foreach (NoiseData noiseData in _shapeInfo.noiseData) value += noiseData.GetCyclicNoise(pct);

            // Create and add point
            _points[i] = value * new Vector2(Mathf.Cos(pct * Mathf.PI * 2), Mathf.Sin(pct * Mathf.PI * 2));
        }

        // Assign points to the polygon
        _outsidePolygon.SetPath(0, _points);
    }

    public void ClearInternal()
    {
        // Clear internal variables
        _outsidePolygon = null;
        _shapeInfo = null;
    }

    public void ClearOutput()
    {
        // Clear output variables
        _points = null;
    }

    #endregion
}
