using System;
using System.Collections.Generic;

[Serializable]
public class MeshSite
{
    public int siteIndex = -1;
    public HashSet<int> neighbouringSiteIndices;
    public MeshSiteVertex[] vertices;
    public MeshSiteEdge[] edges;
    public int centroidMeshIndex = -1;
    public int[] verticesMeshIndices;
    public bool isOutside = false;
}
