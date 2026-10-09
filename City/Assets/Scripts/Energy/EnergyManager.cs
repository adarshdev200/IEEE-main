using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central manager for energy infrastructure selection, telemetry synchronization, and UI console state.
/// </summary>
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    public EnergyType? SelectedType { get; private set; } = null;
    public bool IsConsoleOpen => SelectedType.HasValue;

    public WindTelemetryData WindData { get; } = new WindTelemetryData();
    public SolarTelemetryData SolarData { get; } = new SolarTelemetryData();
    public StorageTelemetryData StorageData { get; } = new StorageTelemetryData();
    public GridTelemetryData GridData { get; } = new GridTelemetryData();
    public EvTelemetryData EvData { get; } = new EvTelemetryData();

    private readonly Dictionary<EnergyType, List<EnergySource>> _sourcesByType =
        new Dictionary<EnergyType, List<EnergySource>>
        {
            { EnergyType.Wind, new List<EnergySource>() },
            { EnergyType.Solar, new List<EnergySource>() },
            { EnergyType.Storage, new List<EnergySource>() },
            { EnergyType.Grid, new List<EnergySource>() },
            { EnergyType.EV, new List<EnergySource>() }
        };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GetComponent<EnergyConsoleUI>() == null)
        {
            gameObject.AddComponent<EnergyConsoleUI>();
        }
    }

    public static EnergyManager EnsureExists()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("EnergySystemManager");
        return go.AddComponent<EnergyManager>();
    }

    public void RegisterSource(EnergySource source)
    {
        if (source == null) return;
        List<EnergySource> list = _sourcesByType[source.sourceType];
        if (!list.Contains(source))
        {
            list.Add(source);
            UpdateCounts();

            // If this type is currently selected, highlight newly added source immediately
            if (SelectedType.HasValue && SelectedType.Value == source.sourceType)
            {
                source.SetSelected(true, GetHighlightColor(source.sourceType));
            }
        }
    }

    public void UnregisterSource(EnergySource source)
    {
        if (source == null) return;
        List<EnergySource> list = _sourcesByType[source.sourceType];
        list.Remove(source);
        UpdateCounts();
    }

    public IReadOnlyList<EnergySource> GetSources(EnergyType type) => _sourcesByType[type];

    private void UpdateCounts()
    {
        WindData.activeCount = _sourcesByType[EnergyType.Wind].Count;
        SolarData.activeCount = _sourcesByType[EnergyType.Solar].Count;
        StorageData.activeCount = _sourcesByType[EnergyType.Storage].Count;
        GridData.activeCount = Mathf.Max(1, _sourcesByType[EnergyType.Grid].Count);

        int evCount = _sourcesByType[EnergyType.EV].Count;
        if (evCount > 0)
        {
            EvData.activeCount = evCount;
            EvData.ratedCapacityMw = evCount * EvData.portsPerStation * 0.05f; // ~50 kW per port
            EvData.defaultRatedCapacity = EvData.ratedCapacityMw;
        }
    }

    public void InitializeData(
        EnergyZonesData energyZones,
        ClimateData climate,
        int solarCount,
        int turbineCount)
    {
        if (energyZones != null)
        {
            if (energyZones.generation?.wind != null)
            {
                float cap = energyZones.generation.wind.minimum_capacity_mw;
                if (cap > 0f)
                {
                    WindData.ratedCapacityMw = cap;
                    WindData.defaultRatedCapacity = cap;
                }
            }

            if (energyZones.generation?.solar != null)
            {
                float cap = energyZones.generation.solar.minimum_capacity_mw;
                if (cap > 0f)
                {
                    SolarData.ratedCapacityMw = cap;
                    SolarData.defaultRatedCapacity = cap;
                }
            }

            if (energyZones.storage != null)
            {
                float cap = energyZones.storage.minimum_capacity_mwh;
                if (cap > 0f)
                {
                    StorageData.storageCapacityMwh = cap;
                    StorageData.defaultStorageCapacity = cap;
                }
            }

            if (energyZones.distribution != null)
            {
                float loss = energyZones.distribution.maximum_transmission_loss * 100f;
                if (loss > 0f)
                {
                    GridData.transmissionLossPercent = loss;
                    GridData.defaultLoss = loss;
                }
                GridData.isGridConnected = energyZones.distribution.grid_connected;
            }
        }

        if (climate != null)
        {
            if (climate.wind != null && climate.wind.average_speed > 0f)
            {
                WindData.windSpeedMs = climate.wind.average_speed;
                WindData.defaultWindSpeed = climate.wind.average_speed;
            }
            if (climate.solar != null && climate.solar.preferred_panel_orientation > 0f)
            {
                SolarData.tiltDegrees = 15f;
            }
        }

        if (solarCount > 0) SolarData.activeCount = solarCount;
        if (turbineCount > 0) WindData.activeCount = turbineCount;

        UpdateWindTurbineSpeeds();
    }

    public void SelectEnergySource(EnergyType type)
    {
        if (SelectedType.HasValue && SelectedType.Value != type)
        {
            // Clear previous highlight
            HighlightAll(SelectedType.Value, false);
        }

        SelectedType = type;
        HighlightAll(type, true);

        // Unlock mouse cursor so the user can easily interact with sliders & buttons in the console
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Deselect()
    {
        if (SelectedType.HasValue)
        {
            HighlightAll(SelectedType.Value, false);
            SelectedType = null;
        }
    }

    private void HighlightAll(EnergyType type, bool highlight)
    {
        Color col = GetHighlightColor(type);
        List<EnergySource> list = _sourcesByType[type];
        foreach (EnergySource s in list)
        {
            if (s != null)
            {
                s.SetSelected(highlight, col);
            }
        }
    }

    public Color GetHighlightColor(EnergyType type)
    {
        return type switch
        {
            EnergyType.Wind    => new Color(0.18f, 0.95f, 0.78f) * 2.4f, // Fresh Breeze Mint / Turquoise
            EnergyType.Solar   => new Color(1.0f, 0.85f, 0.18f) * 2.4f,  // Radiant Solar Gold
            EnergyType.Storage => new Color(0.15f, 0.82f, 0.70f) * 2.4f, // Clean Botanical Teal
            EnergyType.Grid    => new Color(0.20f, 0.98f, 0.45f) * 2.4f, // Vivid Lush Emerald
            EnergyType.EV      => new Color(0.15f, 0.70f, 1.0f) * 2.4f,  // Electric Charge Blue
            _                  => Color.white
        };
    }

    public void UpdateWindTurbineSpeeds()
    {
        float speed = (WindData.operatingStatus == "OPERATIONAL" && WindData.windSpeedMs >= 3f && WindData.windSpeedMs <= 25f)
            ? Mathf.Clamp(WindData.windSpeedMs * 14f, 25f, 240f)
            : 0f;

        foreach (WindTurbineSpinner spinner in FindObjectsByType<WindTurbineSpinner>(FindObjectsSortMode.None))
        {
            if (spinner != null)
            {
                float variation = 0; //(spinner.GetInstanceID() % 9) - 4f;
                spinner.RotationSpeed = Mathf.Max(0f, speed + (speed > 0f ? variation : 0f));
            }
        }
    }

    private void Update()
    {
        if (Mouse.current == null || Camera.main == null) return;

        // Escape closes console & deselects
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && IsConsoleOpen)
        {
            Deselect();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Ignore click if clicking inside the OnGUI console window
            EnergyConsoleUI ui = GetComponent<EnergyConsoleUI>();
            if (IsConsoleOpen && ui != null && ui.IsPointInsideConsole(mousePos))
            {
                return;
            }

            Ray ray = Cursor.lockState == CursorLockMode.Locked
                ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
            {
                EnergySource source = hit.collider.GetComponentInParent<EnergySource>();
                if (source != null)
                {
                    SelectEnergySource(source.sourceType);
                    return;
                }
            }

            // Clicked empty space or ground while console is open -> deselect and close console
            if (IsConsoleOpen)
            {
                Deselect();
            }
        }
    }
}
