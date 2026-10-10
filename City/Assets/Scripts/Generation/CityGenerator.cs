// CityGenerator.cs  – Main orchestrator
// Reads TestCity.json, validates it, then drives each subsystem generator in order.
// Inspector context-menu actions: Generate City, Clear City, Regenerate City.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("City/City Generator")]
public class CityGenerator : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("City Plan")]
    [SerializeField] private TextAsset cityPlan;
    [SerializeField] private TextAsset cityPlanAsset;

    [SerializeField] private Material cityBaseMaterial;
    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;

    // ─── State ────────────────────────────────────────────────────────────────

    private CityPlan   _plan;
    private Transform  _cityRoot;

    // Materials cache shared by sub-generators
    internal CityMaterials Materials { get; private set; }

    // Debug visualization flags
    private bool _showSectors = false;
    private bool _showRoads   = false;
    private bool _showBlocks  = false;
    private bool _showPlots   = false;

    // ─── Sub-generators ───────────────────────────────────────────────────────

    private TerrainGenerator     _terrainGen;
    private SectorGenerator      _sectorGen;
    private RoadGenerator        _roadGen;
    private BlockGenerator       _blockGen;
    private BuildingGenerator    _buildingGen;
    private WaterGenerator       _waterGen;
    private VegetationGenerator  _vegetationGen;
    private EnergyGenerator      _energyGen;
    private TransportGenerator   _transportGen;
    private EnvironmentGenerator _environmentGen;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        if (generateOnStart)
            GenerateCity();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Debug visualization toggle keys
        if (keyboard.f1Key.wasPressedThisFrame) _showSectors = !_showSectors;
        if (keyboard.f2Key.wasPressedThisFrame) _showRoads   = !_showRoads;
        if (keyboard.f3Key.wasPressedThisFrame) _showBlocks  = !_showBlocks;
        if (keyboard.f4Key.wasPressedThisFrame) _showPlots   = !_showPlots;

        // Generation controls
        if (keyboard.rKey.wasPressedThisFrame) RegenerateCity();
        if (keyboard.cKey.wasPressedThisFrame) ClearCity();
    }

    private void OnDrawGizmos()
    {
        if (_plan == null) return;

        if (_showSectors)   DrawSectorGizmos();
        if (_showRoads)     DrawRoadGizmos();
        if (_showBlocks)    DrawBlockGizmos();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        try
        {
            _plan = LoadAndValidatePlan();
        }
        catch (Exception ex)
        {
            Debug.LogError("[City] Generation aborted due to plan error:\n" + ex.Message);
            return;
        }

        ClearCity();
        CreateRoot();
        InitMaterials();
        InitSubGenerators();

        int seed = _plan.generation.seed;

        try
        {
            _terrainGen.Generate(_plan.terrain, _plan.city);
            _sectorGen.Generate(_plan.sectors, _plan.city);
            _roadGen.GenerateWithSectors(_plan.road_graph, _plan.sectors, _plan.city);
            _blockGen.Generate(_plan.sectors, _plan.road_graph, _plan.blocks, _plan.city, seed);
            _buildingGen.Generate(
                _blockGen.GeneratedBlocks, _plan.buildings,
                _plan.sectors, _plan.city, seed);
            _waterGen.Generate(_plan.water, _plan.sectors, _plan.city);
            _vegetationGen.Generate(_plan.vegetation, _plan.sectors, _plan.city, seed);
            _energyGen.Generate(_plan.energy_zones, _plan.sectors, _plan.city, _plan.climate);
            _transportGen.Generate(_plan.transport, _plan.road_graph, _plan.city);
            _environmentGen.Generate(_plan.environment, _plan.city);

            LogDiagnostics();
        }
        catch (Exception ex)
        {
            Debug.LogError("[City] Generation subsystem error:\n" + ex.Message + "\n" + ex.StackTrace);
        }

        // Register clickable clusters so sectors and energy assets can be labelled on click.
        CityClusterLabels.EnsureExists().RegisterClusters(_plan.sectors);

        FrameCityView();

        Debug.Log("[City] Generation complete.");
    }

    private void FrameCityView()
    {
        Camera cam = Camera.main;
        if (cam == null || _plan?.city?.dimensions == null) return;

        float width = _plan.city.dimensions.width;
        float depth = _plan.city.dimensions.depth;
        float centerX = width * 0.5f;
        float centerZ = depth * 0.5f;

        cam.farClipPlane = Mathf.Max(cam.farClipPlane, Mathf.Max(width, depth) * 3f);
        cam.transform.position = new Vector3(centerX, Mathf.Max(width, depth) * 0.45f, -Mathf.Max(width, depth) * 0.25f);
        cam.transform.LookAt(new Vector3(centerX, 0f, centerZ));

        FreeFlyCamera ffc = cam.GetComponent<FreeFlyCamera>();
        if (ffc != null)
        {
            ffc.ResetOrientation(cam.transform.eulerAngles);
        }
    }

    private void LogDiagnostics()
    {
        if (_plan == null || _terrainGen == null) return;

        float width = _plan.city.dimensions.width;
        float depth = _plan.city.dimensions.depth;

        float centerHeight = _terrainGen.SampleHeight(width * 0.5f, depth * 0.5f);
        Debug.Log($"[City Diagnostics] Terrain bounds: {width}x{depth}m. Min height: {_terrainGen.MinHeight:F2}m, Max height: {_terrainGen.MaxHeight:F2}m. Center ({width*0.5f}, {depth*0.5f}) height: {centerHeight:F2}m.");

        // Sample first building
        Transform buildings = _cityRoot.Find("Buildings");
        if (buildings != null && buildings.childCount > 0)
        {
            Transform firstB = buildings.GetChild(0);
            float baseY = firstB.position.y - firstB.localScale.y * 0.5f;
            Debug.Log($"[City Diagnostics] First building '{firstB.name}': Pos={firstB.position}, Scale={firstB.localScale}, BaseY={baseY:F2}m, TerrainY={_terrainGen.SampleHeight(firstB.position.x, firstB.position.z):F2}m.");
        }

        // Sample first tree
        Transform veg = _cityRoot.Find("Vegetation");
        if (veg != null && veg.childCount > 0)
        {
            Transform firstTree = veg.GetChild(0);
            Debug.Log($"[City Diagnostics] First tree object '{firstTree.name}': Pos={firstTree.position}, TerrainY={_terrainGen.SampleHeight(firstTree.position.x, firstTree.position.z):F2}m.");
        }

        // Sample first road
        Transform roads = _cityRoot.Find("Roads");
        if (roads != null)
        {
            Transform firstRoad = roads.GetComponentInChildren<MeshRenderer>()?.transform;
            if (firstRoad != null)
            {
                Debug.Log($"[City Diagnostics] First road segment '{firstRoad.name}': Pos={firstRoad.position}, TerrainY={_terrainGen.SampleHeight(firstRoad.position.x, firstRoad.position.z):F2}m.");
            }
        }
    }

    [ContextMenu("Clear City")]
    public void ClearCity()
    {
        Transform existing = transform.Find("GeneratedCity");
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        _cityRoot    = null;
        _terrainGen  = null;
        _sectorGen   = null;
        _roadGen     = null;
        _blockGen    = null;
        _buildingGen = null;
        _waterGen    = null;
        _vegetationGen = null;
        _energyGen   = null;
        _transportGen = null;
        _environmentGen = null;
    }

    [ContextMenu("Regenerate City")]
    public void RegenerateCity()
    {
        GenerateCity();
    }

    // ─── Internal helpers ─────────────────────────────────────────────────────

    private CityPlan LoadAndValidatePlan()
    {
        TextAsset asset = cityPlan != null ? cityPlan : cityPlanAsset;
        CityPlan plan = CityPlanLoader.LoadFromTextAsset(asset);

        CityPlanValidator.ValidationResult result = CityPlanValidator.Validate(plan);
        if (!result.isValid)
        {
            throw new InvalidOperationException(
                "[CityPlan] Validation failed:\n" + result.errors);
        }

        Debug.Log($"[CityPlan] '{plan.city.name}' — population target: {plan.city.population.target}," +
                  $" dimensions: {plan.city.dimensions.width}×{plan.city.dimensions.depth}m," +
                  $" seed: {plan.generation.seed}");
        return plan;
    }

    private void CreateRoot()
    {
        _cityRoot = new GameObject("GeneratedCity").transform;
        _cityRoot.SetParent(transform);
    }

    private void InitMaterials()
    {
        if (cityBaseMaterial == null || cityBaseMaterial.shader == null)
            throw new InvalidOperationException(
                "CityGenerator: Assign CityBaseMaterial in the Inspector.");

        Materials = new CityMaterials(cityBaseMaterial.shader);
    }

    private void InitSubGenerators()
    {
        _terrainGen     = new TerrainGenerator(_cityRoot, Materials);
        _sectorGen      = new SectorGenerator(_cityRoot, Materials, _terrainGen);
        _roadGen        = new RoadGenerator(_cityRoot, Materials, _terrainGen);
        _blockGen       = new BlockGenerator(_cityRoot, Materials, _terrainGen);
        _buildingGen    = new BuildingGenerator(_cityRoot, Materials, _terrainGen);
        _waterGen       = new WaterGenerator(_cityRoot, Materials, _terrainGen);
        _vegetationGen  = new VegetationGenerator(_cityRoot, Materials, _terrainGen);
        _energyGen      = new EnergyGenerator(_cityRoot, Materials, _terrainGen);
        _transportGen   = new TransportGenerator(_cityRoot, Materials, _terrainGen);
        _environmentGen = new EnvironmentGenerator(_cityRoot, Materials);
    }

    // ─── Gizmo helpers ────────────────────────────────────────────────────────

    private void DrawSectorGizmos()
    {
        if (_plan.sectors == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        foreach (var sector in _plan.sectors)
        {
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;
            float x = sector.geometry.bounds[0];
            float z = sector.geometry.bounds[1];
            float w = sector.geometry.bounds[2];
            float d = sector.geometry.bounds[3];
            Vector3 center = new Vector3(x + w * 0.5f, 1f, z + d * 0.5f);
            Vector3 size   = new Vector3(w, 0.5f, d);
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void DrawRoadGizmos()
    {
        if (_plan.road_graph == null) return;
        var nodes = new Dictionary<string, Vector3>();
        Gizmos.color = Color.yellow;
        foreach (var node in _plan.road_graph.nodes)
        {
            Vector3 pos = new Vector3(node.position.x, 2f, node.position.z);
            nodes[node.id] = pos;
            Gizmos.DrawSphere(pos, 5f);
        }
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.7f);
        foreach (var edge in _plan.road_graph.edges)
        {
            if (nodes.TryGetValue(edge.from, out var a) && nodes.TryGetValue(edge.to, out var b))
                Gizmos.DrawLine(a, b);
        }
    }

    private void DrawBlockGizmos()
    {
        if (_blockGen == null) return;
        Gizmos.color = new Color(0.8f, 0.4f, 0.1f, 0.4f);
        foreach (var block in _blockGen.GeneratedBlocks)
        {
            Vector3 center = new Vector3(block.x + block.width * 0.5f, 2f, block.z + block.depth * 0.5f);
            Vector3 size   = new Vector3(block.width, 0.5f, block.depth);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

/// <summary>Shared material cache used by all sub-generators.</summary>
public class CityMaterials
{
    public readonly Material Terrain;
    public readonly Material Road;
    public readonly Material Sidewalk;
    public readonly Material Water;
    public readonly Material Grass;
    public readonly Material Building;
    public readonly Material Roof;
    public readonly Material Solar;
    public readonly Material Industrial;
    public readonly Material Vegetation;
    public readonly Material Commercial;
    public readonly Material Residential;
    public readonly Material ResidentialTower;
    public readonly Material MixedUse;
    public readonly Material Civic;
    public readonly Material SolarRoof;
    public readonly Material Energy;
    public readonly Material Transport;

    private readonly Shader _shader;

    public CityMaterials(Shader shader)
    {
        if (shader == null)
            throw new ArgumentNullException(nameof(shader));

        _shader = shader;
        Terrain          = Make(new Color(0.18f, 0.32f, 0.18f), 0.1f);
        Road             = Make(new Color(0.15f, 0.16f, 0.18f), 0.2f);
        Sidewalk         = Make(new Color(0.72f, 0.70f, 0.65f), 0.1f);
        Water            = Make(new Color(0.08f, 0.38f, 0.65f), 0.85f);
        Grass            = Make(new Color(0.22f, 0.48f, 0.22f), 0.1f);
        Building         = Make(new Color(0.75f, 0.75f, 0.78f), 0.4f);
        Roof             = Make(new Color(0.38f, 0.36f, 0.35f), 0.2f);
        Solar            = Make(new Color(0.04f, 0.12f, 0.32f), 0.8f);
        Industrial       = Make(new Color(0.44f, 0.41f, 0.38f), 0.25f); // Rugged slate-bronze warehouse tone
        Vegetation       = Make(new Color(0.12f, 0.42f, 0.14f), 0.15f);
        Commercial       = Make(new Color(0.24f, 0.48f, 0.68f), 0.65f); // Sleek modern blue reflective glass
        MixedUse         = Make(new Color(0.42f, 0.52f, 0.58f), 0.50f); // Polished contemporary urban composite
        Residential      = Make(new Color(0.82f, 0.76f, 0.68f), 0.25f); // Warm sandstone / stucco suburban tone
        ResidentialTower = Make(new Color(0.72f, 0.74f, 0.78f), 0.45f); // Clean light-grey architectural concrete
        Civic            = Make(new Color(0.88f, 0.86f, 0.82f), 0.35f); // Majestic limestone/marble public monument
        Energy           = Make(new Color(0.08f, 0.24f, 0.45f), 0.5f);
        Transport        = Make(new Color(0.32f, 0.34f, 0.38f), 0.3f);
        SolarRoof        = MakeEmissive("Building_SolarRoof", new Color(0.03f, 0.10f, 0.28f), new Color(0.0f, 0.18f, 0.75f));
    }

    private Material Make(Color color, float smoothness = 0.3f)
    {
        var mat = new Material(_shader);
        mat.enableInstancing = true;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        return mat;
    }
    private Material MakeEmissive(string name, Color color, Color emit)
    {
        var mat = Make(color, 0.6f);
        mat.name = name;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emit);
        }
        return mat;
    }

    /// <summary>Pick a building material based on sector type string.</summary>
    public Material ForSectorType(string sectorType) => sectorType switch
    {
        "commercial" or "office"                 => Commercial,
        "mixed_use"                              => MixedUse,
        "residential"                            => Residential,
        "civic" or "education" or "healthcare"   => Civic,
        "industrial" or "logistics"              => Industrial,
        "energy"                                 => Energy,
        "transport"                              => Transport,
        "park" or "forest" or "wetland"
            or "recreation" or "agriculture"     => Grass,
        _                                        => Building,
    };
}
