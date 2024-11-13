using System;

// Counter-clockwise edge
[Serializable]
public class MeshSiteEdge
{
    public int siteIndex, fromVertexIndex = -1, toVertexIndex = -1;
    public bool isOutside = false;
    public int neighbouringSiteIndex = -1;
    public int neighbouringEdgeIndex = -1;

    public MeshSiteEdge(int siteIndex, int siteFromVertexIdx, int siteToVertexIdx)
    {
        this.siteIndex = siteIndex;
        this.fromVertexIndex = siteFromVertexIdx;
        this.toVertexIndex = siteToVertexIdx;
    }
}
