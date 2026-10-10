
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class EnergyGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;
    private readonly TerrainGenerator _terrainGen;

    private Transform _energyRoot;

    private Material _panelMaterial;
    private Material _panelFrameMaterial;
    private Material _towerMaterial;
    private Material _bladeMaterial;
    private Material _batteryMaterial;
    private Material _equipmentMaterial;

    // EV charging + parking
    private Material _asphaltMaterial;
    private Material _stallLineMaterial;
    private Material _evChargerMaterial;
    private Material _evCanopyMaterial;
    private Material[] _carMaterials;

    // Imported 3D models (glb via glTFast) keyed by energy type; null/absent → primitive fallback.
    private readonly Dictionary<EnergyType, GameObject> _models = new Dictionary<EnergyType, GameObject>();

    // ── Geometry constants ────────────────────────────────────────────────────
    private const float PanelTilt   = 15f;
    private const float PanelWidth  = 1.6f;
    private const float PanelDepth  = 1.0f;
    private const float PanelGap    = 0.25f;

    private const float TurbineHeight      = 14f;
    private const float TurbineBladeLength = 4.0f;

    // ── RAM & Object limits: strictly bounded to prevent memory explosion ────
    private const int MaxPanelsPerFarm   = 80;
    private const int MaxTurbinesPerFarm = 16;

    // ─────────────────────────────────────────────────────────────────────────

    public EnergyGenerator(
        Transform parent,
        CityMaterials mats,
        TerrainGenerator terrainGen)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        EnergyZonesData  energyZones,
        List<SectorData> sectors,
        CityData         city,
        ClimateData      climate = null)
    {
        Debug.Log($"[Energy] Generate called. Sectors: {sectors?.Count ?? -1}");
        CreateMaterials();
        LoadModels();

        GameObject rootObject = new GameObject("Energy");
        rootObject.transform.SetParent(_parent, false);
        _energyRoot = rootObject.transform;

        EnergyManager energyMgr = EnergyManager.EnsureExists();

        if (sectors == null || sectors.Count == 0)
        {
            Debug.LogWarning("[Energy] No sectors supplied.");
            return;
        }

        // 1. Read generation sources and renewable targets from AI Planner
        bool solarEnabled   = ReadBool(energyZones, "generation", "solar", "preferred", true);
        bool windEnabled    = ReadBool(energyZones, "generation", "wind",  "preferred", false);
        bool storageEnabled = ReadBool(energyZones, "storage", "enabled", "", true);

        bool hasWindSource  = ContainsString(ReadValue(energyZones, "generation", "sources"), "wind");
        bool hasSolarSource = ContainsString(ReadValue(energyZones, "generation", "sources"), "solar");

        if (hasWindSource)  windEnabled  = true;
        if (hasSolarSource) solarEnabled = true;

        float renewableTarget = 0.85f;
        object targetVal = ReadValue(energyZones, "generation", "renewable_target");
        if (targetVal != null)
        {
            try { renewableTarget = Convert.ToSingle(targetVal); } catch { }
        }

        float solarCapacityMw = 100f;
        object solCap = ReadValue(energyZones, "generation", "solar", "minimum_capacity_mw");
        if (solCap != null)
        {
            try { solarCapacityMw = Convert.ToSingle(solCap); } catch { }
        }

        float windCapacityMw = 30f;
        object windCap = ReadValue(energyZones, "generation", "wind", "minimum_capacity_mw");
        if (windCap != null)
        {
            try { windCapacityMw = Convert.ToSingle(windCap); } catch { }
        }

        // 2. Read Climate conditions from AI Planner
        // Solar orientation: default south (180 deg) in Northern Hemisphere
        float panelAzimuth = 180f;
        if (climate?.solar != null && climate.solar.preferred_panel_orientation > 0f)
            panelAzimuth = climate.solar.preferred_panel_orientation;

        // Wind turbine alignment: face into dominant wind direction
        float dominantWindDir = 270f;
        if (climate?.wind != null && climate.wind.dominant_direction > 0f)
            dominantWindDir = climate.wind.dominant_direction;

        float windSpeed = 6f;
        if (climate?.wind != null && climate.wind.average_speed > 0f)
            windSpeed = climate.wind.average_speed;

        // Climate severity adjustment: higher wind speeds or capacity scale turbine count and spacing
        float targetSolarCount = Mathf.Clamp(solarCapacityMw * 0.8f, 30f, MaxPanelsPerFarm);
        float targetTurbineCount = Mathf.Clamp(windCapacityMw * 0.35f, 4f, MaxTurbinesPerFarm);

        int solarFarmCount = 0;
        int turbineCount   = 0;
        int evHubCount = 0;

        foreach (SectorData sector in sectors)
        {
            if (sector == null) continue;

            string type = Convert.ToString(ReadValue(sector, "type"));
            if (!string.Equals(type, "energy", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryGetSectorBounds(sector,
                    out float centerX, out float centerZ,
                    out float width,   out float depth))
            {
                Debug.LogWarning("[Energy] Could not read bounds for an energy sector.");
                continue;
            }

            if (width <= 1f || depth <= 1f) continue;

            float usableWidth = Mathf.Max(1f, width  - 4f);
            float usableDepth = Mathf.Max(1f, depth  - 4f);

            float baseY = _terrainGen != null
                ? _terrainGen.SampleHeight(centerX, centerZ)
                : 0f;

            // Divide sector logically according to renewable planner:
            // West/South zone: High-yield ground mounted solar arrays oriented towards optimal solar angle
            // East/High-elevation zone: Wind park oriented towards dominant wind corridor
            // Central perimeter: Grid battery storage & high-voltage transmission substation
            if (sector.id != null && sector.id.StartsWith("sub_"))
            {
                CreateSubstation(centerX, centerZ, baseY);
                continue;
            }

            bool sectorIsSolar = sector.id != null && sector.id.Contains("solar");
            bool sectorIsWind  = sector.id != null && sector.id.Contains("wind");

            if (solarEnabled && (sectorIsSolar || !sectorIsWind) && usableWidth >= 60f && usableDepth >= 60f)
            {
                float solarW = usableWidth * 0.52f;
                float solarD = usableDepth * 0.75f;
                float startX = centerX - usableWidth * 0.5f;
                float startZ = centerZ - usableDepth * 0.35f;

                CreateSolarFarm(startX, startZ, solarW, solarD, baseY, panelAzimuth, (int)targetSolarCount);
                solarFarmCount++;
            }

            if (windEnabled && (sectorIsWind || !sectorIsSolar) && usableWidth >= 60f && usableDepth >= 60f)
            {
                float windW  = usableWidth * 0.44f;
                float windD  = usableDepth * 0.75f;
                float startX = centerX + usableWidth * 0.05f;
                float startZ = centerZ - usableDepth * 0.35f;

                turbineCount += CreateWindFarm(startX, startZ, windW, windD, baseY, dominantWindDir, (int)targetTurbineCount, windSpeed);
            }

            if (storageEnabled)
            {
                CreateStorageFacility(
                    centerX - usableWidth * 0.2f,
                    centerZ - usableDepth * 0.42f,
                    baseY);
            }

            CreateSubstation(
                centerX + usableWidth * 0.2f,
                centerZ - usableDepth * 0.42f,
                baseY);

            // Clean EV charging hub inside the dedicated energy sector (no building overlap)
            if (usableWidth >= 80f && usableDepth >= 80f)
            {
                CreateEvChargingHub(centerX, centerZ + usableDepth * 0.35f, baseY, new System.Random(sector.id.GetHashCode()));
                evHubCount++;
            }
        }

        energyMgr.InitializeData(energyZones, climate, solarFarmCount, turbineCount);

        Debug.Log($"[Energy] Visuals synchronized with AI Planner: Generated {solarFarmCount} solar arrays ({panelAzimuth}° azimuth), {turbineCount} wind turbines ({dominantWindDir}° wind heading), {evHubCount} EV charging hubs, {renewableTarget:P0} target.");
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        _panelMaterial = MakeMaterial("Energy_NeonBlue_Panels",
            new Color(0.015f, 0.055f, 0.22f),
            new Color(0.0f,   0.25f,  1.1f));
        _panelMaterial.enableInstancing = true;

        _panelFrameMaterial = MakeMaterial("Energy_Cyan_Frames",
            new Color(0.0f, 0.48f, 0.9f),
            new Color(0.0f, 0.85f, 2.0f));
        _panelFrameMaterial.enableInstancing = true;

        _towerMaterial = MakeMaterial("Energy_Blue_Turbines",
            new Color(0.85f, 0.88f, 0.92f),
            Color.black);
        _towerMaterial.enableInstancing = true;

        _bladeMaterial = MakeMaterial("Energy_Cyan_Blades",
            new Color(0.9f, 0.95f, 1.0f),
            new Color(0.0f, 0.5f, 1.5f));
        _bladeMaterial.enableInstancing = true;

        _batteryMaterial = MakeMaterial("Energy_Blue_Batteries",
            new Color(0.015f, 0.08f, 0.35f),
            new Color(0.0f,   0.4f,  1.5f));
        _batteryMaterial.enableInstancing = true;

        _equipmentMaterial = MakeMaterial("Energy_Blue_Substations",
            new Color(0.25f, 0.3f, 0.35f),
            new Color(0.0f,  0.45f, 1.6f));
        _equipmentMaterial.enableInstancing = true;

        _asphaltMaterial = MakeMaterial("EV_Asphalt",
            new Color(0.12f, 0.13f, 0.15f), Color.black);
        _asphaltMaterial.enableInstancing = true;

        _stallLineMaterial = MakeMaterial("EV_StallLines",
            new Color(0.85f, 0.87f, 0.9f), Color.black);
        _stallLineMaterial.enableInstancing = true;

        _evChargerMaterial = MakeMaterial("EV_Charger",
            new Color(0.03f, 0.18f, 0.32f),
            new Color(0.1f,  0.55f, 1.3f));
        _evChargerMaterial.enableInstancing = true;

        _evCanopyMaterial = MakeMaterial("EV_SolarCanopy",
            new Color(0.02f, 0.06f, 0.2f),
            new Color(0.0f,  0.22f, 0.9f));
        _evCanopyMaterial.enableInstancing = true;

        _carMaterials = new[]
        {
            MakeMaterial("EV_Car_1", new Color(0.80f, 0.82f, 0.85f), Color.black),
            MakeMaterial("EV_Car_2", new Color(0.20f, 0.35f, 0.55f), Color.black),
            MakeMaterial("EV_Car_3", new Color(0.55f, 0.20f, 0.22f), Color.black),
            MakeMaterial("EV_Car_4", new Color(0.18f, 0.20f, 0.24f), Color.black),
        };
        foreach (Material m in _carMaterials) m.enableInstancing = true;
    }

    private static Material MakeMaterial(string name, Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat  = new Material(shader) { name = name };

        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color"))      mat.SetColor("_Color",     baseColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);

        if (emissionColor != Color.black && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }

        return mat;
    }

    // ── Solar farm ────────────────────────────────────────────────────────────

    private void CreateSolarFarm(
        float startX, float startZ,
        float width,  float depth,
        float baseY,
        float panelAzimuth = 180f,
        int   maxPanels = 80)
    {
        Transform farm = CreateGroup("SolarFarm");

        float rowSpacing = PanelDepth + PanelGap;
        float colSpacing = PanelWidth + PanelGap;

        int columns = Mathf.Max(1, Mathf.FloorToInt((width - PanelGap) / colSpacing));
        int rows    = Mathf.Max(1, Mathf.FloorToInt((depth - PanelGap) / rowSpacing));

        int cap = Mathf.Clamp(maxPanels, 20, MaxPanelsPerFarm);
        int totalPanels = columns * rows;
        if (totalPanels > cap)
        {
            float ratio = Mathf.Sqrt((float)cap / totalPanels);
            columns = Mathf.Max(1, Mathf.FloorToInt(columns * ratio));
            rows    = Mathf.Max(1, Mathf.FloorToInt(rows    * ratio));
        }

        float totalW  = columns * colSpacing - PanelGap;
        float totalD  = rows    * rowSpacing - PanelGap;
        float offsetX = (width  - totalW) * 0.5f;
        float offsetZ = (depth  - totalD) * 0.5f;

        float tiltRad = PanelTilt * Mathf.Deg2Rad;
        // Rotate solar panel according to preferred panel azimuth (from AI climate planner)
        // 180 deg = faces South. Euler angles: pitch by -PanelTilt, then yaw by (panelAzimuth - 180)
        Quaternion tiltRot = Quaternion.Euler(-PanelTilt, panelAzimuth - 180f, 0f);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float x = startX + offsetX + col * colSpacing + PanelWidth * 0.5f;
                float z = startZ + offsetZ + row * rowSpacing + PanelDepth * 0.5f;
                float panelY = baseY + 0.8f + PanelDepth * 0.5f * Mathf.Sin(tiltRad);

                // Imported solar model (clickable holder) when available; else primitive panel.
                GameObject solarModel = PlaceModel(EnergyType.Solar, new Vector3(x, baseY, z), colSpacing, panelAzimuth - 180f, farm, true);
                if (solarModel != null)
                {
                    // Restore console-driven tilt on the model's visual (same behaviour as the primitive panel).
                    Transform visual = solarModel.transform.childCount > 0 ? solarModel.transform.GetChild(0) : solarModel.transform;
                    SolarPanelTilter modelTilter = visual.gameObject.AddComponent<SolarPanelTilter>();
                    modelTilter.Initialize(panelAzimuth, PanelTilt);
                    continue;
                }

                // Panel body
                GameObject panel = CreateCube("SolarPanel",
                    new Vector3(x, panelY, z),
                    new Vector3(PanelWidth, 0.06f, PanelDepth),
                    _panelMaterial, farm, keepCollider: true);
                panel.transform.rotation = tiltRot;

                EnergySource src = panel.AddComponent<EnergySource>();
                src.sourceType = EnergyType.Solar;

                // Attach live tilt driver — listens to SolarTelemetryData.tiltDegrees each frame
                SolarPanelTilter tilter = panel.AddComponent<SolarPanelTilter>();
                tilter.Initialize(panelAzimuth, PanelTilt);

                // Support leg
                CreateCylinder("SolarSupport",
                    new Vector3(x, baseY + 0.35f, z),
                    new Vector3(0.06f, 0.35f, 0.06f),
                    _panelFrameMaterial, farm);
            }
        }
    }

    // ── Wind farm ─────────────────────────────────────────────────────────────

    private int CreateWindFarm(
        float startX, float startZ,
        float width,  float depth,
        float baseY,
        float windDirection = 270f,
        int   maxTurbines = 16,
        float windSpeed = 6f)
    {
        int cap = Mathf.Clamp(maxTurbines, 2, MaxTurbinesPerFarm);
        int columns = Mathf.Clamp(Mathf.FloorToInt(width / 35f), 1, 4);
        int rows    = Mathf.Clamp(Mathf.FloorToInt(depth / 35f), 1, 4);

        int count = 0;
        for (int row = 0; row < rows && count < cap; row++)
        {
            for (int col = 0; col < columns && count < cap; col++)
            {
                float x = startX + (col + 0.5f) * (width / columns);
                float z = startZ + (row + 0.5f) * (depth / rows);

                if ((row & 1) != 0)
                    x += (width / columns) * 0.25f;

                CreateWindTurbine(x, z, baseY, windDirection, windSpeed);
                count++;
            }
        }

        return count;
    }

    private void CreateWindTurbine(float x, float z, float baseY, float windDirection = 270f, float windSpeed = 6f)
    {
        Transform turbine = CreateGroup("WindTurbine");

        float towerH = TurbineHeight;

        // Add capsule collider encompassing turbine so raycasting anywhere near it hits
        CapsuleCollider col = turbine.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(x, baseY + towerH * 0.5f, z);
        col.radius = TurbineBladeLength + 1.5f;
        col.height = towerH + TurbineBladeLength * 2f;

        EnergySource src = turbine.gameObject.AddComponent<EnergySource>();
        src.sourceType = EnergyType.Wind;

        // Imported wind-turbine model (visual only; group above provides collider + EnergySource).
        // Fit by height so the turbine reads as a tall tower rather than a stub.
        GameObject windModel = PlaceModel(EnergyType.Wind, new Vector3(x, baseY, z), towerH + TurbineBladeLength * 2f, windDirection, turbine, false, fitByHeight: true);
        if (windModel != null)
        {
            // Restore console-driven spin on the model's rotor node.
            // EnergyManager.UpdateWindTurbineSpeeds() drives every WindTurbineSpinner from the console.
            Transform rotor = FindChildByName(windModel.transform, "rotor");
            if (rotor != null)
            {
                WindTurbineSpinner modelSpinner = rotor.gameObject.AddComponent<WindTurbineSpinner>();
                modelSpinner.RotationSpeed = Mathf.Clamp(windSpeed * 14f, 60f, 120f) + UnityEngine.Random.Range(-10f, 10f);
                modelSpinner.RotationAxis = Vector3.forward;
            }
            return;
        }

        // Tower
        CreateCylinder("TurbineTower",
            new Vector3(x, baseY + towerH * 0.5f, z),
            new Vector3(0.35f, towerH * 0.5f, 0.35f),
            _towerMaterial, turbine);

        // Nacelle & Hub (aligned directly into dominant wind direction)
        Quaternion yawRot = Quaternion.Euler(0f, windDirection, 0f);
        Vector3 forward = yawRot * Vector3.forward;

        Vector3 hubBase = new Vector3(x, baseY + towerH, z);
        Vector3 nacellePos = hubBase + forward * 0.3f + new Vector3(0f, 0.05f, 0f);
        GameObject nacelle = CreateCube("TurbineNacelle",
            nacellePos,
            new Vector3(0.6f, 0.6f, 1.3f),
            _equipmentMaterial, turbine);
        nacelle.transform.rotation = yawRot;

        Vector3 hubPos = hubBase + forward * 0.95f + new Vector3(0f, 0.05f, 0f);

        // Rotating rotor assembly (hub + 3 blades)
        GameObject rotorObj = new GameObject("TurbineRotor");
        rotorObj.transform.SetParent(turbine, false);
        rotorObj.transform.position = hubPos;
        rotorObj.transform.rotation = yawRot;

        // Hub nose cone
        GameObject hubObj = CreateSphere("TurbineHub",
            hubPos,
            new Vector3(0.45f, 0.45f, 0.45f),
            _bladeMaterial, rotorObj.transform);
        hubObj.transform.localPosition = Vector3.zero;
        hubObj.transform.localRotation = Quaternion.identity;

        // 3 Aerodynamic Blades
        for (int i = 0; i < 3; i++)
        {
            float angle   = 90f + i * 120f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 localRadial = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 localBladeCenter = new Vector3(0f, 0f, 0.05f) + localRadial * (TurbineBladeLength * 0.5f);

            GameObject blade = CreateCube("TurbineBlade",
                hubPos,
                new Vector3(0.2f, TurbineBladeLength, 0.08f),
                _bladeMaterial, rotorObj.transform);

            blade.transform.localPosition = localBladeCenter;
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // Add rotor spinner animation
        WindTurbineSpinner spinner = rotorObj.AddComponent<WindTurbineSpinner>();
        float baseSpeed = Mathf.Clamp(windSpeed * 14f, 60f, 120f);
        float variation = UnityEngine.Random.Range(-10f, 10f);
        spinner.RotationSpeed = baseSpeed + variation;
        spinner.RotationAxis = Vector3.forward;
    }

    // ── Storage & substations ─────────────────────────────────────────────────

    private void CreateStorageFacility(float x, float z, float baseY)
    {
        Transform storage = CreateGroup("EnergyStorage");
        float groundY = baseY + 0.7f;

        // Imported storage model (clickable holder) when available; else primitive battery units.
        if (PlaceModel(EnergyType.Storage, new Vector3(x, baseY, z), 9f, 0f, storage, true) != null)
            return;

        for (int i = 0; i < 3; i++)
        {
            float ox = (i - 1) * 3f;

            GameObject unit = CreateCube("BatteryUnit",
                new Vector3(x + ox, groundY, z),
                new Vector3(2.4f, 1.4f, 1.6f),
                _batteryMaterial, storage, keepCollider: true);

            EnergySource src = unit.AddComponent<EnergySource>();
            src.sourceType = EnergyType.Storage;

            // Live emission visualizer — pulses reflect SOC level and charge/discharge direction
            unit.AddComponent<StorageVisualizer>();

            CreateCube("BatteryIndicator",
                new Vector3(x + ox, groundY + 0.2f, z - 0.82f),
                new Vector3(1.2f, 0.18f, 0.05f),
                _panelFrameMaterial, storage);
        }
    }

    private void CreateSubstation(float x, float z, float baseY)
    {
        Transform sub = CreateGroup("Substation");

        // Add BoxCollider encompassing substation so clicking anywhere selects the grid substation
        BoxCollider col = sub.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(x, baseY + 1.5f, z);
        col.size = new Vector3(8f, 4f, 6f);

        EnergySource src = sub.gameObject.AddComponent<EnergySource>();
        src.sourceType = EnergyType.Grid;

        // Imported grid/substation model (visual only; group above provides collider + EnergySource).
        if (PlaceModel(EnergyType.Grid, new Vector3(x, baseY, z), 10f, 0f, sub, false) != null)
            return;

        CreateCube("SubstationBase",
            new Vector3(x, baseY + 0.15f, z),
            new Vector3(6f, 0.3f, 4f),
            _equipmentMaterial, sub);

        for (int i = 0; i < 3; i++)
        {
            float ox = (i - 1) * 1.8f;

            // Each transformer body drives a live emission visualizer reflecting grid state
            GameObject transformer = CreateCube("Transformer",
                new Vector3(x + ox, baseY + 0.9f, z),
                new Vector3(1.1f, 1.2f, 1.2f),
                _equipmentMaterial, sub);
            transformer.AddComponent<SubstationVisualizer>();

            CreateCylinder("TransformerInsulator",
                new Vector3(x + ox, baseY + 1.65f, z),
                new Vector3(0.1f, 0.3f, 0.1f),
                _panelFrameMaterial, sub);
        }

        // Gantry
        GameObject gantry = CreateCube("SubstationGantry",
            new Vector3(x, baseY + 2.1f, z + 1.2f),
            new Vector3(5f, 0.12f, 0.12f),
            _panelFrameMaterial, sub);
        gantry.AddComponent<SubstationVisualizer>();

        for (int i = 0; i < 3; i++)
        {
            float ox = (i - 1) * 2.2f;
            GameObject post = CreateCube("GantryPost",
                new Vector3(x + ox, baseY + 1.1f, z + 1.2f),
                new Vector3(0.1f, 1f, 0.1f),
                _panelFrameMaterial, sub);
            post.AddComponent<SubstationVisualizer>();
        }
    }

    // ── EV charging hubs + parking ────────────────────────────────────────────

    private int CreateEvHubsForSector(SectorData sector)
    {
        float[] b = sector.geometry.bounds;
        float sx = b[0], sz = b[1], w = Mathf.Abs(b[2]), d = Mathf.Abs(b[3]);
        if (w < 80f || d < 80f) return 0;

        int seed = (sector.id != null ? sector.id.GetHashCode() : 12345) ^ 0x5EED;
        var rng = new System.Random(seed);

        const float inset = 75f;
        const float spacing = 300f;
        int cols = Mathf.Clamp(Mathf.FloorToInt((w - inset * 2f) / spacing) + 1, 1, 3);
        int rows = Mathf.Clamp(Mathf.FloorToInt((d - inset * 2f) / spacing) + 1, 1, 3);
        float cellW = (w - inset * 2f) / cols;
        float cellD = (d - inset * 2f) / rows;

        int count = 0;
        const int cap = 6;
        for (int r = 0; r < rows && count < cap; r++)
        {
            for (int c = 0; c < cols && count < cap; c++)
            {
                if (rng.NextDouble() < 0.30) continue; // deterministic scatter
                float cx = sx + inset + (c + 0.5f) * cellW + (float)(rng.NextDouble() - 0.5) * cellW * 0.3f;
                float cz = sz + inset + (r + 0.5f) * cellD + (float)(rng.NextDouble() - 0.5) * cellD * 0.3f;
                float baseY = _terrainGen != null ? _terrainGen.SampleHeight(cx, cz) : 0f;
                CreateEvChargingHub(cx, cz, baseY, rng);
                count++;
            }
        }

        if (count == 0) // guarantee at least one hub per qualifying sector
        {
            float cx = sx + w * 0.5f, cz = sz + d * 0.5f;
            float baseY = _terrainGen != null ? _terrainGen.SampleHeight(cx, cz) : 0f;
            CreateEvChargingHub(cx, cz, baseY, rng);
            count = 1;
        }
        return count;
    }

    private void CreateEvChargingHub(float cx, float cz, float baseY, System.Random rng)
    {
        Transform hub = CreateGroup("EVChargingHub");

        const float lotW = 34f, lotD = 22f;
        float groundY = baseY + 0.06f;

        // Clickable hub: collider + EnergySource so it selects into the EV console
        BoxCollider col = hub.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(cx, baseY + 2f, cz);
        col.size = new Vector3(lotW, 6f, lotD);
        EnergySource src = hub.gameObject.AddComponent<EnergySource>();
        src.sourceType = EnergyType.EV;

        // Asphalt lot
        CreateCube("ParkingLot", new Vector3(cx, groundY, cz),
            new Vector3(lotW, 0.08f, lotD), _asphaltMaterial, hub);

        // Stall markings (two bays per side)
        const int bays = 8;
        float bayW = lotW / bays;
        for (int i = 0; i <= bays; i++)
        {
            float lx = cx - lotW * 0.5f + i * bayW;
            CreateCube("StallLine", new Vector3(lx, groundY + 0.05f, cz - lotD * 0.25f),
                new Vector3(0.18f, 0.02f, lotD * 0.42f), _stallLineMaterial, hub);
            CreateCube("StallLine", new Vector3(lx, groundY + 0.05f, cz + lotD * 0.25f),
                new Vector3(0.18f, 0.02f, lotD * 0.42f), _stallLineMaterial, hub);
        }

        // Solar carport canopy on posts
        GameObject canopy = CreateCube("EVCanopy", new Vector3(cx, baseY + 3.4f, cz),
            new Vector3(lotW * 0.92f, 0.18f, lotD * 0.5f), _evCanopyMaterial, hub);
        canopy.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                CreateCylinder("CanopyPost",
                    new Vector3(cx + i * lotW * 0.42f, baseY + 1.7f, cz + j * lotD * 0.22f),
                    new Vector3(0.2f, 1.7f, 0.2f), _equipmentMaterial, hub);

        // EV charger pedestals along the centre island
        const int chargers = 4;
        for (int i = 0; i < chargers; i++)
        {
            float t = (i + 0.5f) / chargers;
            float x = cx - lotW * 0.5f + 3f + t * (lotW - 6f);

            // Imported EV-charger model (visual only; hub group provides collider + EnergySource).
            if (PlaceModel(EnergyType.EV, new Vector3(x, baseY, cz), 1.6f, 0f, hub, false) != null)
                continue;

            CreateCube("EVCharger", new Vector3(x, baseY + 0.75f, cz),
                new Vector3(0.5f, 1.5f, 0.35f), _evChargerMaterial, hub);
            CreateCube("EVChargerHead", new Vector3(x, baseY + 1.55f, cz),
                new Vector3(0.6f, 0.35f, 0.45f), _panelFrameMaterial, hub);
        }

        // A few parked EVs in bays
        int cars = 3 + rng.Next(0, 3);
        for (int i = 0; i < cars; i++)
        {
            int bay = rng.Next(0, bays);
            float x = cx - lotW * 0.5f + (bay + 0.5f) * bayW;
            float zside = (rng.Next(0, 2) == 0 ? -1f : 1f) * lotD * 0.25f;
            Material cm = _carMaterials[rng.Next(0, _carMaterials.Length)];
            CreateCube("EVCar", new Vector3(x, baseY + 0.6f, cz + zside),
                new Vector3(bayW * 0.8f, 1.1f, lotD * 0.34f), cm, hub);
        }
    }

    // ── Imported model loading & placement ────────────────────────────────────

    // Resource paths are relative to any Assets/Resources folder (no extension).
    private void LoadModels()
    {
        _models.Clear();
        TryLoadModel(EnergyType.Wind,    "EnergyModels/Wind/model");
        TryLoadModel(EnergyType.Solar,   "EnergyModels/Solar/model");
        TryLoadModel(EnergyType.Storage, "EnergyModels/Storage/model");
        TryLoadModel(EnergyType.EV,      "EnergyModels/EV/model");   // supplied later; primitive fallback until then
        TryLoadModel(EnergyType.Grid,    "EnergyModels/Grid/model"); // supplied later; primitive fallback until then

        if (_models.Count > 0)
            Debug.Log($"[Energy] Loaded {_models.Count} imported energy model(s): {string.Join(", ", _models.Keys)}");
    }

    private void TryLoadModel(EnergyType type, string resourcePath)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null) _models[type] = prefab;
    }

    private bool HasModel(EnergyType type) => _models.TryGetValue(type, out GameObject p) && p != null;

    /// <summary>
    /// Instantiates the imported model for <paramref name="type"/> (if one is loaded),
    /// auto-scaled from its renderer bounds to <paramref name="targetFootprint"/> metres on its
    /// largest horizontal axis, grounded so its base sits at <paramref name="groundPos"/>, yawed,
    /// with import colliders stripped and shadows disabled (matching the primitive look).
    ///
    /// When <paramref name="addSourceCollider"/> is true the model is wrapped in a holder that
    /// carries its own BoxCollider + EnergySource (used where no parent group provides them, e.g.
    /// solar panels and storage). Otherwise the model is purely visual and clickability comes from
    /// the parent group's existing collider + EnergySource (wind, substation, EV hub).
    ///
    /// Returns the placed object, or null when no model is available (caller falls back to primitives).
    /// </summary>
    private GameObject PlaceModel(
        EnergyType type, Vector3 groundPos, float targetSize, float yawDeg,
        Transform parent, bool addSourceCollider, bool fitByHeight = false)
    {
        if (!HasModel(type)) return null;

        Transform host;
        if (addSourceCollider)
        {
            GameObject holder = new GameObject($"{type}Model");
            holder.transform.SetParent(parent, false);
            holder.transform.position = groundPos; // unit scale, no rotation → simple world/local mapping
            host = holder.transform;
        }
        else
        {
            host = parent;
        }

        GameObject inst = UnityEngine.Object.Instantiate(_models[type], host);
        inst.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

        Renderer[] rends = inst.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = GetBounds(rends);
            float measure = fitByHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (measure > 1e-4f && targetSize > 0f)
                inst.transform.localScale *= targetSize / measure;

            b = GetBounds(rends); // recompute after scaling
            inst.transform.position += groundPos - new Vector3(b.center.x, b.min.y, b.center.z);

            foreach (Renderer r in rends)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
        else
        {
            inst.transform.position = groundPos;
        }

        foreach (Collider c in inst.GetComponentsInChildren<Collider>())
            UnityEngine.Object.Destroy(c);

        if (!addSourceCollider) return inst;

        // Holder gets a clickable collider sized to the final world bounds (holder is unit-scale, unrotated).
        Bounds fb = rends.Length > 0 ? GetBounds(host.GetComponentsInChildren<Renderer>())
                                     : new Bounds(host.position, Vector3.one);
        BoxCollider bc = host.gameObject.AddComponent<BoxCollider>();
        bc.center = fb.center - host.position;
        bc.size   = fb.size;

        EnergySource src = host.gameObject.AddComponent<EnergySource>();
        src.sourceType = type;
        return host.gameObject;
    }

    private static Bounds GetBounds(Renderer[] rends)
    {
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    /// <summary>
    /// Depth-first search for a descendant transform matching <paramref name="target"/>:
    /// exact name first, then a group (has children) whose name contains it, then any match.
    /// </summary>
    private static Transform FindChildByName(Transform root, string target)
    {
        target = target.ToLowerInvariant();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in all)
            if (t != root && t.name.ToLowerInvariant() == target) return t;

        foreach (Transform t in all)
            if (t != root && t.childCount > 0 && t.name.ToLowerInvariant().Contains(target)) return t;

        foreach (Transform t in all)
            if (t != root && t.name.ToLowerInvariant().Contains(target)) return t;

        return null;
    }

    // ── Primitive helpers ─────────────────────────────────────────────────────

    private Transform CreateGroup(string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(_energyRoot, false);
        return g.transform;
    }

    private static GameObject CreateCube(
        string name, Vector3 position, Vector3 scale, Material mat, Transform parent, bool keepCollider = false)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position   = position;
        obj.transform.localScale = scale;
        ApplyMaterial(obj, mat);
        if (!keepCollider) RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateCylinder(
        string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position   = position;
        obj.transform.localScale = scale;
        ApplyMaterial(obj, mat);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateSphere(
        string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position   = position;
        obj.transform.localScale = scale;
        ApplyMaterial(obj, mat);
        RemoveCollider(obj);
        return obj;
    }

    private static void ApplyMaterial(GameObject obj, Material mat)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial    = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
        }
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider c = obj.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.Destroy(c);
    }

    // ── Data access helpers ───────────────────────────────────────────────────

    private static object ReadValue(object source, params string[] path)
    {
        object cur = source;
        foreach (string m in path)
        {
            if (cur == null) return null;
            cur = ReadMember(cur, m);
        }
        return cur;
    }

    private static object ReadMember(object source, string name)
    {
        if (source == null || string.IsNullOrEmpty(name)) return null;

        if (source is IDictionary dict)
        {
            foreach (DictionaryEntry e in dict)
                if (string.Equals(Convert.ToString(e.Key), name, StringComparison.OrdinalIgnoreCase))
                    return e.Value;
        }

        Type t = source.GetType();

        PropertyInfo prop = t.GetProperty(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(source);

        FieldInfo field = t.GetField(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null) return field.GetValue(source);

        return null;
    }

    private static bool ReadBool(
        object source, string first, string second, string third, bool defaultValue)
    {
        object v = ReadValue(source, first, second, third);
        if (v == null) return defaultValue;
        try { return Convert.ToBoolean(v); }
        catch { return defaultValue; }
    }

    private static bool ContainsString(object source, string expected)
    {
        if (source is IEnumerable e && !(source is string))
            foreach (object item in e)
                if (string.Equals(Convert.ToString(item), expected, StringComparison.OrdinalIgnoreCase))
                    return true;
        return false;
    }

    private static bool TryGetSectorBounds(
        SectorData sector,
        out float centerX, out float centerZ,
        out float width,   out float depth)
    {
        centerX = centerZ = width = depth = 0f;

        if (sector?.geometry == null) return false;

        float[] b = sector.geometry.bounds;
        if (b == null || b.Length < 4)
        {
            Debug.LogWarning($"[Energy] Sector '{sector.id}' has invalid bounds.");
            return false;
        }

        centerX = b[0];
        centerZ = b[1];
        width   = Mathf.Abs(b[2]);
        depth   = Mathf.Abs(b[3]);

        return width > 1f && depth > 1f;
    }
}
