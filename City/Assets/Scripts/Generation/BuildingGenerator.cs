// BuildingGenerator.cs
// Places buildings inside generated blocks, following the sector's building_rules
// and the global buildings archetypes. Uses plot-based subdivision per block.
// Optimized using MeshBatcher: All buildings and rooftop penthouses are batched by material
// into unified meshes, taking draw calls and GameObject count down from 20,000+ to under 10.

using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _buildingsRoot;

    // Rooftop detail material
    private Material _pentMat;

    public BuildingGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        List<GeneratedBlock>      blocks,
        BuildingsData             buildingsData,
        List<SectorData>          sectors,
        CityData                  city,
        int                       seed)
    {
        _buildingsRoot = new GameObject("Buildings").transform;
        _buildingsRoot.SetParent(_parent, false);

        CreateSharedMaterials();

        if (blocks == null || blocks.Count == 0)
        {
            Debug.LogWarning("[Buildings] No blocks to place buildings in.");
            return;
        }

        // Build archetype lookup
        var archetypes = new Dictionary<string, BuildingArchetypeData>();
        if (buildingsData?.archetypes != null)
            foreach (var a in buildingsData.archetypes)
                archetypes[a.id] = a;

        // Build sector lookup
        var sectorMap = new Dictionary<string, SectorData>();
        if (sectors != null)
            foreach (var s in sectors)
                sectorMap[s.id] = s;

        BuildingSpacingData globalSpacing = buildingsData?.spacing ?? new BuildingSpacingData();

        // High-performance material batchers per district identity
        MeshBatcher bResidential      = new MeshBatcher();
        MeshBatcher bResidentialTower = new MeshBatcher();
        MeshBatcher bCommercial       = new MeshBatcher();
        MeshBatcher bMixedUse         = new MeshBatcher();
        MeshBatcher bIndustrial       = new MeshBatcher();
        MeshBatcher bCivic            = new MeshBatcher();
        MeshBatcher bDefault          = new MeshBatcher();
        MeshBatcher bPenthouse        = new MeshBatcher();
        MeshBatcher bSolarRoof        = new MeshBatcher();

        var rng = new System.Random(seed);
        int totalBuilt = 0;
        int solarRoofCount = 0;

        // Limit buildings per block to maintain aesthetic density without runaway geometry
        const int maxPerBlock = 6;

        foreach (var block in blocks)
        {
            if (IsNonDevelopable(block.sectorType)) continue;

            sectorMap.TryGetValue(block.sectorId, out SectorData sector);
            BuildingRulesData rules = sector?.building_rules ?? DefaultRules();

            List<BuildingArchetypeData> allowed = ResolveAllowedArchetypes(
                rules.allowed_archetypes, archetypes, block.sectorType);
            if (allowed.Count == 0) continue;

            float frontSetback = Mathf.Max(rules.spacing?.front_setback ?? 3f, globalSpacing.minimum_front_setback);
            float sideSetback  = Mathf.Max(rules.spacing?.side_setback  ?? 2f, globalSpacing.minimum_side_setback);
            float rearSetback  = Mathf.Max(rules.spacing?.rear_setback  ?? 3f, globalSpacing.minimum_rear_setback);
            float gap          = Mathf.Max(rules.spacing?.building_gap  ?? 5f, globalSpacing.minimum_building_gap);

            var archetype = allowed[rng.Next(allowed.Count)];
            float plotW = Lerp(archetype.footprint.minimum_width, archetype.footprint.maximum_width, 0.5f);
            float plotD = Lerp(archetype.footprint.minimum_depth, archetype.footprint.maximum_depth, 0.5f);
            plotW = Mathf.Clamp(plotW, 14f, block.width  - sideSetback * 2f);
            plotD = Mathf.Clamp(plotD, 14f, block.depth  - frontSetback - rearSetback);

            float innerW = block.width  - sideSetback * 2f;
            float innerD = block.depth  - frontSetback - rearSetback;
            if (innerW <= 0 || innerD <= 0) continue;

            int colCount = Mathf.Max(1, Mathf.FloorToInt(innerW / (plotW + gap)));
            int rowCount = Mathf.Max(1, Mathf.FloorToInt(innerD / (plotD + gap)));
            int buildingsInBlock = 0;

            for (int row = 0; row < rowCount && buildingsInBlock < maxPerBlock; row++)
            {
                for (int col = 0; col < colCount && buildingsInBlock < maxPerBlock; col++)
                {
                    var arch = allowed[rng.Next(allowed.Count)];

                    float bw = Mathf.Clamp(
                        Lerp(arch.footprint.minimum_width, arch.footprint.maximum_width, (float)rng.NextDouble()),
                        10f, plotW);
                    float bd = Mathf.Clamp(
                        Lerp(arch.footprint.minimum_depth, arch.footprint.maximum_depth, (float)rng.NextDouble()),
                        10f, plotD);

                    int floors = rng.Next(
                        Mathf.Max(1, rules.height.minimum_floors),
                        Mathf.Max(rules.height.minimum_floors + 1, rules.height.maximum_floors + 1));
                    float floorH = arch.height.floor_height > 0 ? arch.height.floor_height : 3.5f;
                    float height = floors * floorH;

                    float px = block.x + sideSetback  + col * (plotW + gap) + bw * 0.5f;
                    float pz = block.z + frontSetback + row * (plotD + gap) + bd * 0.5f;

                    float terrainHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;
                    float baseSurfaceY = terrainHeight + BlockGenerator.BlockTopY;
                    float centerY = baseSurfaceY + height * 0.5f;
                    float roofY   = baseSurfaceY + height;

                    // Choose batcher based on district identity and archetype
                    MeshBatcher targetBatcher = bDefault;
                    if (block.sectorType == "industrial" || arch.type is "industrial" or "warehouse")
                    {
                        targetBatcher = bIndustrial;
                    }
                    else if (block.sectorType == "civic" || arch.type is "government" or "community" or "school" or "hospital")
                    {
                        targetBatcher = bCivic;
                    }
                    else if (block.sectorType == "commercial" || arch.type is "retail" or "office")
                    {
                        targetBatcher = bCommercial;
                    }
                    else if (block.sectorType == "mixed_use" || arch.type is "mixed_use")
                    {
                        targetBatcher = bMixedUse;
                    }
                    else if (arch.type == "tower" || floors >= 10)
                    {
                        targetBatcher = bResidentialTower;
                    }
                    else if (block.sectorType == "residential" || arch.type is "house" or "apartment")
                    {
                        targetBatcher = bResidential;
                    }

                    targetBatcher.AddBox(new Vector3(px, centerY, pz), new Vector3(bw, height, bd));

                    // Rooftop detail on 4+ floor buildings
                    if (floors >= 4)
                    {
                        float pentW = bw * 0.5f;
                        float pentD = bd * 0.5f;
                        float pentH = Mathf.Clamp(height * 0.10f, 1.5f, 3.5f);
                        bPenthouse.AddBox(new Vector3(px, roofY + pentH * 0.5f, pz), new Vector3(pentW, pentH, pentD));
                    }

                    // Rooftop solar installations connected to sector/city sustainability targets
                    bool hasRooftopSolar = sector?.energy?.rooftop_solar ?? (buildingsData?.sustainability?.rooftop_solar_allowed ?? true);
                    if (hasRooftopSolar && floors <= 12 && (rng.NextDouble() < (sector?.energy?.solar_coverage_target ?? 0.4f)))
                    {
                        float solarCoverage = Mathf.Clamp(sector?.energy?.solar_coverage_target ?? 0.35f, 0.2f, 0.6f);
                        float sWidth = bw * 0.75f;
                        float sDepth = bd * solarCoverage;
                        float sY     = roofY + (floors >= 4 ? 0.05f : 0.05f);

                        // Mount solar array on top of roof
                        bSolarRoof.AddBox(new Vector3(px, sY + 0.1f, pz), new Vector3(sWidth, 0.12f, sDepth));
                        solarRoofCount++;
                    }

                    buildingsInBlock++;
                    totalBuilt++;
                }
            }
        }

        // Build batched GameObjects with district-specific material identities
        bResidential.BuildGameObject("Batched_Buildings_Residential",           _mats.Residential,      _buildingsRoot);
        bResidentialTower.BuildGameObject("Batched_Buildings_ResidentialTowers", _mats.ResidentialTower, _buildingsRoot);
        bCommercial.BuildGameObject("Batched_Buildings_Commercial",             _mats.Commercial,       _buildingsRoot);
        bMixedUse.BuildGameObject("Batched_Buildings_MixedUse",                 _mats.MixedUse,         _buildingsRoot);
        bIndustrial.BuildGameObject("Batched_Buildings_Industrial",             _mats.Industrial,       _buildingsRoot);
        bCivic.BuildGameObject("Batched_Buildings_Civic",                       _mats.Civic,            _buildingsRoot);
        bDefault.BuildGameObject("Batched_Buildings_Default",                   _mats.Building,         _buildingsRoot);
        bPenthouse.BuildGameObject("Batched_Buildings_Penthouses",               _pentMat,               _buildingsRoot);
        bSolarRoof.BuildGameObject("Batched_Buildings_RooftopSolar",             _mats.SolarRoof,        _buildingsRoot);

        Debug.Log($"[Buildings] Generated {totalBuilt} buildings and {solarRoofCount} rooftop solar arrays batched across distinct district identities.");
    }

    private void CreateSharedMaterials()
    {
        _pentMat = MakeFlat("Penthouse", new Color(0.38f, 0.40f, 0.45f));
    }

    private static Material MakeFlat(string name, Color col)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = name };
        m.enableInstancing = true;
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color"))      m.SetColor("_Color", col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
        return m;
    }

    private static bool IsNonDevelopable(string type) => type is
        "park" or "forest" or "wetland" or "water" or "agriculture" or "energy";

    private static List<BuildingArchetypeData> ResolveAllowedArchetypes(
        List<string> allowedIds,
        Dictionary<string, BuildingArchetypeData> archetypes,
        string sectorType)
    {
        var result = new List<BuildingArchetypeData>();

        if (allowedIds != null)
            foreach (var id in allowedIds)
                if (archetypes.TryGetValue(id, out var arch))
                    result.Add(arch);

        if (result.Count == 0)
            foreach (var kv in archetypes)
                if (ArchetypeMatchesSector(kv.Value.type, sectorType))
                    result.Add(kv.Value);

        if (result.Count == 0)
            result.AddRange(archetypes.Values);

        return result;
    }

    private static bool ArchetypeMatchesSector(string archType, string sectorType) =>
        sectorType switch
        {
            "residential"               => archType is "house" or "apartment",
            "commercial" or "mixed_use" => archType is "retail" or "office" or "mixed_use" or "apartment",
            "office"                    => archType is "office" or "tower",
            "industrial" or "logistics" => archType is "industrial" or "warehouse",
            "civic"                     => archType is "government" or "community",
            "education"                 => archType is "school",
            "healthcare"                => archType is "hospital",
            _                           => true,
        };

    private static BuildingRulesData DefaultRules() => new BuildingRulesData
    {
        height    = new BuildingHeightRulesData    { minimum_floors = 1, maximum_floors = 4 },
        footprint = new BuildingFootprintRulesData { minimum_width = 10f, maximum_width = 24f, minimum_depth = 10f, maximum_depth = 20f },
        spacing   = new BuildingSpacingRulesData   { building_gap = 5f, front_setback = 3f, side_setback = 3f, rear_setback = 3f },
        coverage  = new BuildingCoverageRulesData  { maximum = 0.5f },
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
