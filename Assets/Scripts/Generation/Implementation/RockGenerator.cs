using UnityEngine;
using static VoronoiMeshGenerator;

public class RockGenerator : Generator
{
    public override string Name => "Rock Composite";
    public override Generator[] ComposedGenerators => new Generator[] { planetPolygonGenerator, voronoiMeshGenerator };

    public override void Generate()
    {
        Clear();
        outsidePolygon.enabled = true;
        planetPolygonGenerator.Generate();
        voronoiMeshGenerator.Generate();
        outsidePolygon.enabled = !disablePolygon;
        StepColorMesh();
        IsGenerated = true;
    }

    public override void Clear()
    {
        planetPolygonGenerator.Clear();
        voronoiMeshGenerator.Clear();
        IsGenerated = false;
    }

    [Header("Parameters")]
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator voronoiMeshGenerator;
    [SerializeField] private Color[] colorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    [SerializeField] private NoiseData colorNoise = new NoiseData(new float[] { 0, 1 });
    [SerializeField] private bool disablePolygon;

    private void StepColorMesh()
    {
        // Get mesh and mesh sites
        Mesh mesh = voronoiMeshGenerator.Mesh;
        MeshSite[] meshSites = voronoiMeshGenerator.MeshSites;
        Color[] colors = new Color[mesh.vertices.Length];

        // Calculate colors for each site
        foreach (MeshSite meshSite in meshSites)
        {
            Vector2 centre = mesh.vertices[meshSite.meshCentroidI];
            float r = colorNoise.GetNoise(centre.x, centre.y);
            Color col = Color.Lerp(colorRange[0], colorRange[1], r);

            // Update mesh colors
            colors[meshSite.meshCentroidI] = col;
            foreach (int i in meshSite.meshVerticesI) colors[i] = col;
        }

        // Update mesh colours
        mesh.colors = colors;
    }
}
