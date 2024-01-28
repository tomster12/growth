using System;
using System.Collections.Generic;

[Serializable]
public class MeshSite
{
    public MeshSiteVertex[] vertices;
    public MeshSiteEdge[] edges;
    public int[] meshVerticesIdx;
    public int meshCentroidIdx = -1;
    public int siteIdx = -1;
    public bool isOutside = false;
    public HashSet<int> neighbouringSitesIdx;
}
