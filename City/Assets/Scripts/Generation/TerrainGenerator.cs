using UnityEngine;

public class TerrainGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;

    public Terrain UnityTerrain { get; private set; } = null;

    public float MinHeight => 0f;
    public float MaxHeight => 0f;

    private GameObject _floorObject;
    private Texture2D _gridTexture;

    public TerrainGenerator(Transform parent, CityMaterials mats)
    {
        _parent = parent;
        _mats   = mats;
    }

    public void Generate(TerrainData terrainData, CityData city)
    {
        UnityTerrain = null;

        float width = city?.dimensions?.width > 0f ? city.dimensions.width : 6000f;
        float depth = city?.dimensions?.depth > 0f ? city.dimensions.depth : 6000f;

        // Generous margin around the city perimeter
        float margin = 1000f;
        float totalWidth = width + margin * 2f;
        float totalDepth = depth + margin * 2f;
        float centerX = width * 0.5f;
        float centerZ = depth * 0.5f;

        // Create the floor object
        _floorObject = new GameObject("CityGridFloor");
        _floorObject.transform.SetParent(_parent, false);

        // Position well below 0 so all road/sidewalk/block/park overlays (Y >= 0.05)
        // cleanly render above it with zero z-fighting or flickering artifacts.
        const float floorTopY = -0.50f;
        const float floorThickness = 2.0f;
        _floorObject.transform.position = new Vector3(centerX, floorTopY - floorThickness * 0.5f, centerZ);
        _floorObject.transform.localScale = new Vector3(totalWidth, floorThickness, totalDepth);

        // Add BoxCollider for physical collision and camera raycast/flying containment
        BoxCollider collider = _floorObject.AddComponent<BoxCollider>();
        collider.size = Vector3.one;

        // Visual Mesh
        MeshFilter mf = _floorObject.AddComponent<MeshFilter>();
        MeshRenderer mr = _floorObject.AddComponent<MeshRenderer>();

        // Shared procedural unit cube mesh
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tempCube);

        // Create clean grid texture & material
        Material floorMat = CreateGridMaterial(totalWidth, totalDepth);
        mr.sharedMaterial = floorMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        Debug.Log($"[Terrain] Created collidable grid floor: {totalWidth:F0}m x {totalDepth:F0}m at Y={floorTopY:F2}m.");
    }

    private Material CreateGridMaterial(float totalWidth, float totalDepth)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = "CityFloor_Grid" };
        mat.enableInstancing = true;

        // Procedural 128x128 crisp anti-aliased grid texture
        const int texSize = 128;
        _gridTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
        _gridTexture.wrapMode = TextureWrapMode.Repeat;
        _gridTexture.filterMode = FilterMode.Trilinear;

        Color bg = new Color(0.14f, 0.16f, 0.18f, 1.0f);       // Modern dark slate concrete
        Color line = new Color(0.24f, 0.28f, 0.32f, 1.0f);     // Subtle crisp grid lines

        Color[] pixels = new Color[texSize * texSize];
        const int lineWidth = 3;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                bool isLine = (x < lineWidth || x >= texSize - lineWidth ||
                               y < lineWidth || y >= texSize - lineWidth);
                pixels[y * texSize + x] = isLine ? line : bg;
            }
        }

        _gridTexture.SetPixels(pixels);
        _gridTexture.Apply(true);

        // Tile every 50 meters
        const float gridSize = 50f;
        float tileX = totalWidth / gridSize;
        float tileY = totalDepth / gridSize;

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", _gridTexture);
            mat.SetTextureScale("_BaseMap", new Vector2(tileX, tileY));
        }
        else if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", _gridTexture);
            mat.SetTextureScale("_MainTex", new Vector2(tileX, tileY));
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

        return mat;
    }

    public float SampleHeight(float worldX, float worldZ)
    {
        return 0f;
    }
}
