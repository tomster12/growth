using System;

[Serializable]
public class MeshSiteEdge
{
    public int siteIndex, siteFromVertexIdx = -1, siteToVertexIdx = -1;
    public bool isOutside = false;
    public int neighbouringSiteIdx = -1;
    public int neighbouringEdgeIdx = -1;

    public MeshSiteEdge(int siteIndex, int siteFromVertexIdx, int siteToVertexIdx)
    {
        this.siteIndex = siteIndex;
        this.siteFromVertexIdx = siteFromVertexIdx;
        this.siteToVertexIdx = siteToVertexIdx;
    }
}
