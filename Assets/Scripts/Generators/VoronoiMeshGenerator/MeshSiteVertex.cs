using System;
using static GK.VoronoiClipper;

[Serializable]
public class MeshSiteVertex
{
    public VertexType type;
    public int vertexUID = -1;
    public int intersectionFromUID = -1, intersectionToUID = -1;

    public MeshSiteVertex(ClippedVertex clippedVertex)
    {
        this.type = clippedVertex.type;
        this.vertexUID = clippedVertex.vertexUID;
        this.intersectionFromUID = clippedVertex.intersectionFromUID;
        this.intersectionToUID = clippedVertex.intersectionToUID;
    }
}
