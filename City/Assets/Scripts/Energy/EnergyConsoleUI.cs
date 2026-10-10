using System;
using UnityEngine;

/// <summary>
/// Renders an interactive glassmorphic telemetry console for the municipal energy grid in OnGUI.
/// Features a translucent frosted glass aesthetic with low visual noise, clean typography,
/// real-time environmental metrics, and operating parameter tuning for Wind, Solar, Storage, and Smart Grid.
/// </summary>
public class EnergyConsoleUI : MonoBehaviour
{
    public static EnergyConsoleUI Instance { get; private set; }

    private Rect _consoleRect;
    private Vector2 _scrollPos = Vector2.zero;

    // Procedural Glassmorphic Textures
    private Texture2D _bgTexture;
    private Texture2D _cardTexture;
    private Texture2D _cardSubtleTexture;
    private Texture2D _btnNormalTex;
    private Texture2D _btnHoverTex;
    private Texture2D _btnActiveTex;
    private Texture2D _btnDangerTex;
    private Texture2D _btnDangerHoverTex;
    private Texture2D _badgeTex;
    private Texture2D _sliderTrackTex;
    private Texture2D _sliderThumbTex;
    private Texture2D _progressTrackTex;
    private Texture2D _progressFillTex;

    // Clean Glassmorphic GUIStyles
    private GUIStyle _windowStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _sectionTitleStyle;
    private GUIStyle _ecoBannerStyle;
    private GUIStyle _cardStyle;
    private GUIStyle _cardValStyle;
    private GUIStyle _cardLabelStyle;
    private GUIStyle _sliderLabelStyle;
    private GUIStyle _sliderTrackStyle;
    private GUIStyle _sliderThumbStyle;
    private GUIStyle _ecoMetricRowStyle;
    private GUIStyle _badgeStyle;
    private GUIStyle _tabStyle;
    private GUIStyle _activeTabStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _activeButtonStyle;
    private GUIStyle _dangerButtonStyle;
    private GUIStyle _tipStyle;

    private bool _stylesInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        DestroyTexture(ref _bgTexture);
        DestroyTexture(ref _cardTexture);
        DestroyTexture(ref _cardSubtleTexture);
        DestroyTexture(ref _btnNormalTex);
        DestroyTexture(ref _btnHoverTex);
        DestroyTexture(ref _btnActiveTex);
        DestroyTexture(ref _btnDangerTex);
        DestroyTexture(ref _btnDangerHoverTex);
        DestroyTexture(ref _badgeTex);
        DestroyTexture(ref _sliderTrackTex);
        DestroyTexture(ref _sliderThumbTex);
        DestroyTexture(ref _progressTrackTex);
        DestroyTexture(ref _progressFillTex);
    }

    private static void DestroyTexture(ref Texture2D tex)
    {
        if (tex != null)
        {
            Destroy(tex);
            tex = null;
        }
    }

    public bool IsPointInsideConsole(Vector2 mouseScreenPos)
    {
        if (EnergyManager.Instance == null || !EnergyManager.Instance.IsConsoleOpen)
            return false;

        float guiY = Screen.height - mouseScreenPos.y;
        return _consoleRect.Contains(new Vector2(mouseScreenPos.x, guiY));
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        // ─── Glassmorphism Textures ──────────────────────────────────────────
        // Translucent dark glass window background with delicate rim highlight
        _bgTexture = MakeRoundedGlassTex(32, 32, 10f,
            new Color(0.06f, 0.08f, 0.11f, 0.68f),
            new Color(1.0f, 1.0f, 1.0f, 0.14f), 1f);

        // Subtle frosted glass card for metrics
        _cardTexture = MakeRoundedGlassTex(20, 20, 6f,
            new Color(1.0f, 1.0f, 1.0f, 0.045f),
            new Color(1.0f, 1.0f, 1.0f, 0.09f), 1f);

        // Soft glass banner for environmental impact
        _cardSubtleTexture = MakeRoundedGlassTex(20, 20, 6f,
            new Color(1.0f, 1.0f, 1.0f, 0.03f),
            new Color(1.0f, 1.0f, 1.0f, 0.07f), 1f);

        // Glass buttons
        _btnNormalTex = MakeRoundedGlassTex(16, 16, 5f,
            new Color(1.0f, 1.0f, 1.0f, 0.06f),
            new Color(1.0f, 1.0f, 1.0f, 0.12f), 1f);

        _btnHoverTex = MakeRoundedGlassTex(16, 16, 5f,
            new Color(1.0f, 1.0f, 1.0f, 0.13f),
            new Color(1.0f, 1.0f, 1.0f, 0.24f), 1f);

        _btnActiveTex = MakeRoundedGlassTex(16, 16, 5f,
            new Color(1.0f, 1.0f, 1.0f, 0.20f),
            new Color(1.0f, 1.0f, 1.0f, 0.38f), 1f);

        _btnDangerTex = MakeRoundedGlassTex(16, 16, 5f,
            new Color(0.85f, 0.25f, 0.25f, 0.12f),
            new Color(1.0f, 0.35f, 0.35f, 0.22f), 1f);

        _btnDangerHoverTex = MakeRoundedGlassTex(16, 16, 5f,
            new Color(0.95f, 0.30f, 0.30f, 0.20f),
            new Color(1.0f, 0.45f, 0.45f, 0.35f), 1f);

        // Floating 3D world HUD badge pill
        _badgeTex = MakeRoundedGlassTex(16, 16, 8f,
            new Color(0.06f, 0.08f, 0.12f, 0.65f),
            new Color(1.0f, 1.0f, 1.0f, 0.20f), 1f);

        // Slider track and thumb
        _sliderTrackTex = MakeRoundedGlassTex(8, 8, 3f,
            new Color(1.0f, 1.0f, 1.0f, 0.10f),
            new Color(1.0f, 1.0f, 1.0f, 0.16f), 1f);

        _sliderThumbTex = MakeRoundedGlassTex(14, 14, 7f,
            new Color(0.92f, 0.95f, 0.98f, 0.92f),
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 1f);

        // Clean progress bar textures
        _progressTrackTex = MakeRoundedGlassTex(8, 8, 3f,
            new Color(1.0f, 1.0f, 1.0f, 0.08f),
            new Color(1.0f, 1.0f, 1.0f, 0.12f), 1f);

        _progressFillTex = MakeRoundedGlassTex(8, 8, 3f,
            new Color(0.40f, 0.82f, 0.72f, 0.85f),
            new Color(0.55f, 0.95f, 0.85f, 0.90f), 1f);

        // ─── Glassmorphism Typography & Layout Styles ─────────────────────────
        _windowStyle = new GUIStyle
        {
            normal = { background = _bgTexture },
            border = new RectOffset(10, 10, 10, 10),
            padding = new RectOffset(16, 16, 16, 16)
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.96f, 0.98f, 1.0f) }
        };

        _subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.60f, 0.68f, 0.78f) }
        };

        _sectionTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.52f, 0.60f, 0.70f) },
            margin = new RectOffset(0, 0, 8, 3)
        };

        _ecoBannerStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _cardSubtleTexture },
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(2, 2, 4, 8)
        };

        _cardStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _cardTexture },
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(3, 3, 3, 3)
        };

        _cardValStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.96f, 0.98f, 1.0f) }
        };

        _cardLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.58f, 0.65f, 0.74f) }
        };

        _sliderLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.82f, 0.86f, 0.92f) }
        };

        _sliderTrackStyle = new GUIStyle(GUI.skin.horizontalSlider)
        {
            normal = { background = _sliderTrackTex },
            border = new RectOffset(3, 3, 3, 3),
            fixedHeight = 4,
            margin = new RectOffset(0, 0, 8, 8)
        };

        _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            normal = { background = _sliderThumbTex },
            hover = { background = _sliderThumbTex },
            active = { background = _sliderThumbTex },
            border = new RectOffset(6, 6, 6, 6),
            fixedWidth = 12,
            fixedHeight = 12
        };

        _ecoMetricRowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.85f, 0.89f, 0.94f) }
        };

        _badgeStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _badgeTex, textColor = new Color(0.92f, 0.95f, 0.98f) },
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(4, 4, 2, 2)
        };

        _tabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnNormalTex, textColor = new Color(0.65f, 0.72f, 0.80f) },
            hover = { background = _btnHoverTex, textColor = Color.white },
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(8, 8, 6, 6)
        };

        _activeTabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnActiveTex, textColor = Color.white },
            hover = { background = _btnActiveTex, textColor = Color.white },
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(8, 8, 6, 6)
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnNormalTex, textColor = new Color(0.82f, 0.86f, 0.92f) },
            hover = { background = _btnHoverTex, textColor = Color.white },
            active = { background = _btnActiveTex, textColor = Color.white },
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(8, 8, 6, 6)
        };

        _activeButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnActiveTex, textColor = Color.white },
            hover = { background = _btnActiveTex, textColor = Color.white },
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(8, 8, 6, 6)
        };

        _dangerButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnDangerTex, textColor = new Color(0.95f, 0.65f, 0.65f) },
            hover = { background = _btnDangerHoverTex, textColor = new Color(1f, 0.85f, 0.85f) },
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(6, 6, 5, 5)
        };

        _tipStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.50f, 0.55f, 0.62f) }
        };

        _stylesInitialized = true;
    }

    /// <summary>
    /// Generates a procedural rounded glass texture with anti-aliasing and specular glass sheen.
    /// Perfectly suited for 9-slice scaling in Unity IMGUI.
    /// </summary>
    private static Texture2D MakeRoundedGlassTex(int width, int height, float radius, Color fillColor, Color borderColor, float borderWidth = 1f)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float innerX = Mathf.Clamp(px, radius, width - radius);
                float innerY = Mathf.Clamp(py, radius, height - radius);
                float d = Vector2.Distance(new Vector2(px, py), new Vector2(innerX, innerY));

                if (d > radius)
                {
                    // Outside rounded corner - anti-aliased edge
                    float edgeAlpha = Mathf.Clamp01(1f - (d - radius));
                    if (edgeAlpha > 0f)
                    {
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * edgeAlpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
                else if (d > radius - borderWidth)
                {
                    // Border rim
                    float t = Mathf.Clamp01((d - (radius - borderWidth)) / borderWidth);
                    Color c = Color.Lerp(fillColor, borderColor, t);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    // Glass fill with subtle vertical specular reflection gradient
                    float sheen = (float)y / height * 0.04f;
                    Color col = new Color(
                        Mathf.Clamp01(fillColor.r + sheen),
                        Mathf.Clamp01(fillColor.g + sheen),
                        Mathf.Clamp01(fillColor.b + sheen),
                        fillColor.a
                    );
                    tex.SetPixel(x, y, col);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        if (EnergyManager.Instance == null || !EnergyManager.Instance.SelectedType.HasValue) return;
        EnergyType currentType = EnergyManager.Instance.SelectedType.Value;

        InitStyles();

        // 1. Draw floating HUD tags above selected assets in the 3D scene
        if (Camera.main != null)
        {
            DrawWorldBadges(currentType);
        }

        // 2. Draw Main Glassmorphic Console
        float width = 450f;
        float height = Mathf.Min(Screen.height - 40f, 720f);
        _consoleRect = new Rect(Screen.width - width - 20f, 20f, width, height);

        GUI.Box(_consoleRect, GUIContent.none, _windowStyle);

        Rect innerRect = new Rect(_consoleRect.x + 14f, _consoleRect.y + 14f, _consoleRect.width - 28f, _consoleRect.height - 28f);
        GUILayout.BeginArea(innerRect);

        DrawHeader(currentType);
        DrawTabs(currentType);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

        // Environmental Impact Telemetry Panel
        DrawSustainabilityImpactBanner(currentType);

        switch (currentType)
        {
            case EnergyType.Wind:
                DrawWindConsole(EnergyManager.Instance.WindData);
                break;
            case EnergyType.Solar:
                DrawSolarConsole(EnergyManager.Instance.SolarData);
                break;
            case EnergyType.Storage:
                DrawStorageConsole(EnergyManager.Instance.StorageData);
                break;
            case EnergyType.Grid:
                DrawGridConsole(EnergyManager.Instance.GridData);
                break;
            case EnergyType.EV:
                DrawEvConsole(EnergyManager.Instance.EvData);
                break;
        }

        // Municipal Decarbonization Ratio
        DrawCityDecarbonizationGauge();

        GUILayout.EndScrollView();

        DrawFooter(currentType);

        GUILayout.EndArea();
    }

    private void DrawHeader(EnergyType type)
    {
        GUILayout.BeginHorizontal();

        var (title, subtitle) = type switch
        {
            EnergyType.Wind    => ("Wind Kinetic Park", "Zero-Emission Turbine Array"),
            EnergyType.Solar   => ("Photovoltaic Solar Farm", "High-Yield Solar Matrix"),
            EnergyType.Storage => ("BESS Storage Reserve", "Grid-Buffering Battery System"),
            EnergyType.Grid    => ("Smart Municipal Microgrid", "Distribution & Substation Hub"),
            EnergyType.EV      => ("EV Charging Network", "Smart Clean-Mobility Hubs"),
            _                  => ("Renewable Energy Asset", "Dispatch Control System")
        };

        GUILayout.BeginVertical();
        GUILayout.Label(title, _headerStyle);
        GUILayout.Label(subtitle, _subHeaderStyle);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Close", _dangerButtonStyle, GUILayout.Width(64), GUILayout.Height(24)))
        {
            EnergyManager.Instance.Deselect();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void DrawTabs(EnergyType currentType)
    {
        GUILayout.BeginHorizontal();
        if (DrawTabButton("Wind", currentType == EnergyType.Wind))
            EnergyManager.Instance.SelectEnergySource(EnergyType.Wind);
        if (DrawTabButton("Solar", currentType == EnergyType.Solar))
            EnergyManager.Instance.SelectEnergySource(EnergyType.Solar);
        if (DrawTabButton("Storage", currentType == EnergyType.Storage))
            EnergyManager.Instance.SelectEnergySource(EnergyType.Storage);
        if (DrawTabButton("Grid", currentType == EnergyType.Grid))
            EnergyManager.Instance.SelectEnergySource(EnergyType.Grid);
        if (DrawTabButton("EV", currentType == EnergyType.EV))
            EnergyManager.Instance.SelectEnergySource(EnergyType.EV);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private bool DrawTabButton(string label, bool isActive)
    {
        return GUILayout.Button(label, isActive ? _activeTabStyle : _tabStyle, GUILayout.Height(25));
    }

    // ─── Environmental Impact Panel ───────────────────────────────────────────

    private void DrawSustainabilityImpactBanner(EnergyType type)
    {
        float currentOutput = type switch
        {
            EnergyType.Wind    => EnergyManager.Instance.WindData.CurrentOutputMw,
            EnergyType.Solar   => EnergyManager.Instance.SolarData.CurrentOutputMw,
            EnergyType.Storage => Mathf.Max(0f, EnergyManager.Instance.StorageData.powerFlowMw),
            EnergyType.Grid    => EnergyManager.Instance.WindData.CurrentOutputMw + EnergyManager.Instance.SolarData.CurrentOutputMw,
            EnergyType.EV      => EnergyManager.Instance.EvData.CurrentOutputMw * (EnergyManager.Instance.EvData.renewableSharePercent / 100f),
            _                  => 0f
        };

        // Real, dataset-backed impact estimates (see SustainabilityDatasets.cs):
        //  • Grid emission factor .... CEA CO2 Baseline Database v21.0 (India).
        //  • Lifecycle intensity ..... IPCC AR5 WG3 Annex III medians.
        //  • Homes / trees ........... India household survey + EPA urban forestry.
        float gridIntensityG = SustainabilityDatasets.GridIntensityGPerKwh;           // gCO2/kWh the grid would emit
        float sourceIntensityG = SustainabilityDatasets.LifecycleIntensityGPerKwh(type); // gCO2eq/kWh of this clean source
        float netAvoidedTPerMwh = Mathf.Max(0f, (gridIntensityG - sourceIntensityG) / 1000f); // t/MWh avoided vs grid

        float co2AvoidedHourly = currentOutput * netAvoidedTPerMwh;                   // t/h (output_MW * t/MWh)
        int homes = (int)(currentOutput * 1000f / (SustainabilityDatasets.HouseholdAnnualKwh / 8760f));
        int trees = (int)(co2AvoidedHourly * 24f * SustainabilityDatasets.TreesPerTonnePerYear);

        GUILayout.BeginVertical(_ecoBannerStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("ENVIRONMENTAL IMPACT TELEMETRY", _subHeaderStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("<color=#6EE7B7>●</color> Active Clean Dispatch", _subHeaderStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"CO₂ Avoided: <b>{co2AvoidedHourly:F1} t/h</b> ({co2AvoidedHourly * 24f:F0} t/d)", _ecoMetricRowStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Clean Homes: <b>{homes:N0}</b>", _ecoMetricRowStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(2);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Forest Equiv: <b>{trees:N0} trees</b>", _ecoMetricRowStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Carbon Intensity: <b>{sourceIntensityG:F0} gCO₂e/kWh</b> <color=#9FB4A8>vs grid {gridIntensityG:F0}</color>", _ecoMetricRowStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        GUILayout.Label($"<color=#8AA093><size=9>{SustainabilityDatasets.AttributionLine}</size></color>", _ecoMetricRowStyle);

        GUILayout.EndVertical();
        GUILayout.Space(6);
    }

    private void DrawKpiGrid(string v1, string l1, string v2, string l2, string v3, string l3, string v4, string l4)
    {
        GUILayout.BeginHorizontal();
        DrawKpiCard(v1, l1);
        DrawKpiCard(v2, l2);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawKpiCard(v3, l3);
        DrawKpiCard(v4, l4);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void DrawKpiCard(string value, string label)
    {
        GUILayout.BeginVertical(_cardStyle, GUILayout.ExpandWidth(true));
        GUILayout.Label(value, _cardValStyle);
        GUILayout.Label(label, _cardLabelStyle);
        GUILayout.EndVertical();
    }

    private float DrawSlider(float val, float min, float max)
    {
        return GUILayout.HorizontalSlider(val, min, max, _sliderTrackStyle, _sliderThumbStyle);
    }

    // ─── Wind Console ─────────────────────────────────────────────────────────

    private void DrawWindConsole(WindTelemetryData d)
    {
        DrawKpiGrid(
            $"{d.CurrentOutputMw:F1} MW", "OUTPUT POWER",
            $"{d.CapacityFactor:F0}%", "CAPACITY FACTOR",
            $"{d.ratedCapacityMw:F1} MW", $"RATED CAP ({d.activeCount} UNITS)",
            $"{d.DailyYieldMwh:F0} MWh", "EST. 24H YIELD"
        );

        GUILayout.Label("OPERATING PARAMETERS", _sectionTitleStyle);

        // Wind Speed Slider
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Wind Speed: <b>{d.windSpeedMs:F1} m/s</b>", _sliderLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(GetWindRating(d.windSpeedMs), _subHeaderStyle);
        GUILayout.EndHorizontal();

        float oldSpeed = d.windSpeedMs;
        d.windSpeedMs = DrawSlider(d.windSpeedMs, 0.0f, 25.0f);
        if (Mathf.Abs(oldSpeed - d.windSpeedMs) > 0.05f)
        {
            EnergyManager.Instance.UpdateWindTurbineSpeeds();
        }
        GUILayout.Space(4);

        // Efficiency Slider
        GUILayout.Label($"Aerodynamic Efficiency: <b>{d.efficiencyPercent:F1}%</b>", _sliderLabelStyle);
        d.efficiencyPercent = DrawSlider(d.efficiencyPercent, 15.0f, 55.0f);
        GUILayout.Space(4);

        // Capacity Slider
        GUILayout.Label($"Turbine Array Rated Capacity: <b>{d.ratedCapacityMw:F1} MW</b>", _sliderLabelStyle);
        d.ratedCapacityMw = DrawSlider(d.ratedCapacityMw, 5.0f, 100.0f);
        GUILayout.Space(8);

        // Operating Mode
        GUILayout.Label("Dispatch Mode", _sectionTitleStyle);
        GUILayout.BeginHorizontal();
        if (DrawModeButton("Full Yield", d.operatingStatus == "OPERATIONAL"))
        {
            d.operatingStatus = "OPERATIONAL";
            EnergyManager.Instance.UpdateWindTurbineSpeeds();
        }
        if (DrawModeButton("Curtailment", d.operatingStatus == "CURTAILED"))
        {
            d.operatingStatus = "CURTAILED";
            EnergyManager.Instance.UpdateWindTurbineSpeeds();
        }
        if (DrawModeButton("Maintenance", d.operatingStatus == "MAINTENANCE"))
        {
            d.operatingStatus = "MAINTENANCE";
            EnergyManager.Instance.UpdateWindTurbineSpeeds();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
    }

    private string GetWindRating(float v) => v switch
    {
        < 3f  => "Calm (< Cut-in)",
        < 8f  => "Moderate Breeze",
        < 15f => "Optimal Flow",
        < 25f => "High Velocity",
        _     => "Storm Cut-out"
    };

    // ─── Solar Console ────────────────────────────────────────────────────────

    private void DrawSolarConsole(SolarTelemetryData d)
    {
        DrawKpiGrid(
            $"{d.CurrentOutputMw:F1} MW", "GENERATION",
            $"{d.CapacityFactor:F0}%", "CAPACITY FACTOR",
            $"{d.ratedCapacityMw:F1} MW", $"PV CAP ({d.activeCount} PANELS)",
            $"{d.DailyYieldMwh:F0} MWh", "DAILY YIELD"
        );

        GUILayout.Label("OPERATING PARAMETERS", _sectionTitleStyle);

        // Solar Irradiance
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Solar Irradiance: <b>{d.solarIrradianceWm2:F0} W/m²</b>", _sliderLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(GetSunRating(d.solarIrradianceWm2), _subHeaderStyle);
        GUILayout.EndHorizontal();
        d.solarIrradianceWm2 = DrawSlider(d.solarIrradianceWm2, 0.0f, 1200.0f);
        GUILayout.Space(4);

        // Panel Efficiency
        GUILayout.Label($"Photovoltaic Efficiency: <b>{d.panelEfficiencyPercent:F1}%</b>", _sliderLabelStyle);
        d.panelEfficiencyPercent = DrawSlider(d.panelEfficiencyPercent, 12.0f, 30.0f);
        GUILayout.Space(4);

        // Panel Tilt
        GUILayout.Label($"Mount Tilt Angle: <b>{d.tiltDegrees:F0}°</b>", _sliderLabelStyle);
        d.tiltDegrees = DrawSlider(d.tiltDegrees, 0.0f, 60.0f);
        GUILayout.Space(4);

        // Ambient Temp
        GUILayout.Label($"Ambient Temperature: <b>{d.temperatureC:F1} °C</b>", _sliderLabelStyle);
        d.temperatureC = DrawSlider(d.temperatureC, 10.0f, 50.0f);
        GUILayout.Space(8);

        // Mode
        GUILayout.Label("Dispatch Mode", _sectionTitleStyle);
        GUILayout.BeginHorizontal();
        if (DrawModeButton("Full Yield", d.operatingStatus == "OPERATIONAL")) d.operatingStatus = "OPERATIONAL";
        if (DrawModeButton("Diffused", d.operatingStatus == "REDUCED")) d.operatingStatus = "REDUCED";
        if (DrawModeButton("Offline", d.operatingStatus == "OFFLINE")) d.operatingStatus = "OFFLINE";
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
    }

    private string GetSunRating(float irr) => irr switch
    {
        < 100f  => "Overcast / Dusk",
        < 500f  => "Diffused Light",
        < 950f  => "Direct Sunlight",
        _       => "Peak Irradiance"
    };

    // ─── Storage Console ──────────────────────────────────────────────────────

    private void DrawStorageConsole(StorageTelemetryData d)
    {
        string flowStr = d.powerFlowMw >= 0
            ? $"+{d.powerFlowMw:F1} MW"
            : $"{d.powerFlowMw:F1} MW";
        string timeStr = d.EstimatedHoursRemaining >= 90f ? "Standby" : $"{d.EstimatedHoursRemaining:F1} hrs";

        DrawKpiGrid(
            $"{d.stateOfChargePercent:F0}%", "STATE OF CHARGE",
            $"{d.StoredEnergyMwh:F1} MWh", $"RESERVE ({d.activeCount} UNITS)",
            flowStr, d.powerFlowMw >= 0 ? "DISCHARGING" : "CHARGING",
            timeStr, "BUFFER RUNTIME"
        );

        GUILayout.Label("OPERATING PARAMETERS", _sectionTitleStyle);

        // State of Charge
        GUILayout.Label($"Storage Reserve: <b>{d.stateOfChargePercent:F0}%</b>", _sliderLabelStyle);
        d.stateOfChargePercent = DrawSlider(d.stateOfChargePercent, 0.0f, 100.0f);
        GUILayout.Space(4);

        // Power Flow
        GUILayout.Label($"Power Flow: <b>{d.powerFlowMw:F1} MW</b>", _sliderLabelStyle);
        d.powerFlowMw = DrawSlider(d.powerFlowMw, -d.maxPowerMw, d.maxPowerMw);
        GUILayout.Space(4);

        // Storage Capacity
        GUILayout.Label($"Storage Capacity: <b>{d.storageCapacityMwh:F0} MWh</b>", _sliderLabelStyle);
        d.storageCapacityMwh = DrawSlider(d.storageCapacityMwh, 20.0f, 300.0f);
        GUILayout.Space(4);

        // Round-trip Efficiency
        GUILayout.Label($"Round-Trip Efficiency: <b>{d.roundTripEfficiencyPercent:F1}%</b>", _sliderLabelStyle);
        d.roundTripEfficiencyPercent = DrawSlider(d.roundTripEfficiencyPercent, 70.0f, 98.0f);
        GUILayout.Space(8);

        // Mode
        GUILayout.Label("Operational Mode", _sectionTitleStyle);
        GUILayout.BeginHorizontal();
        if (DrawModeButton("Peak Shaving", d.operatingMode == "PEAK_SHAVING")) d.operatingMode = "PEAK_SHAVING";
        if (DrawModeButton("Fast Frequency", d.operatingMode == "FAST_FREQ")) d.operatingMode = "FAST_FREQ";
        if (DrawModeButton("Standby", d.operatingMode == "STANDBY")) { d.operatingMode = "STANDBY"; d.powerFlowMw = 0f; }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
    }

    // ─── Grid Console ─────────────────────────────────────────────────────────

    private void DrawGridConsole(GridTelemetryData d)
    {
        float totalClean = EnergyManager.Instance.WindData.CurrentOutputMw +
                           EnergyManager.Instance.SolarData.CurrentOutputMw +
                           Mathf.Max(0f, EnergyManager.Instance.StorageData.powerFlowMw);

        float netExchange = d.cityDemandMw - totalClean;
        string netStr = netExchange >= 0 ? $"Import {netExchange:F1} MW" : $"Export {Mathf.Abs(netExchange):F1} MW";

        DrawKpiGrid(
            $"{d.cityDemandMw:F1} MW", "MUNICIPAL DEMAND",
            $"{totalClean:F1} MW", "RENEWABLE SUPPLY",
            netStr, "NET EXCHANGE",
            $"{d.gridFrequencyHz:F2} Hz", $"FREQUENCY ({d.gridVoltageKv:F0} kV)"
        );

        GUILayout.Label("OPERATING PARAMETERS", _sectionTitleStyle);

        // City Demand
        GUILayout.Label($"Total City Demand: <b>{d.cityDemandMw:F1} MW</b>", _sliderLabelStyle);
        d.cityDemandMw = DrawSlider(d.cityDemandMw, 20.0f, 200.0f);
        GUILayout.Space(4);

        // Transmission loss
        GUILayout.Label($"Transmission Loss Target: <b>{d.transmissionLossPercent:F1}%</b>", _sliderLabelStyle);
        d.transmissionLossPercent = DrawSlider(d.transmissionLossPercent, 1.0f, 10.0f);
        GUILayout.Space(4);

        // Voltage
        GUILayout.Label($"Substation Voltage: <b>{d.gridVoltageKv:F1} kV</b>", _sliderLabelStyle);
        d.gridVoltageKv = DrawSlider(d.gridVoltageKv, 90.0f, 130.0f);
        GUILayout.Space(4);

        // Frequency
        GUILayout.Label($"Grid Frequency: <b>{d.gridFrequencyHz:F2} Hz</b>", _sliderLabelStyle);
        d.gridFrequencyHz = DrawSlider(d.gridFrequencyHz, 48.5f, 51.5f);
        GUILayout.Space(4);

        // Power factor
        GUILayout.Label($"Power Factor: <b>{d.powerFactor:F2}</b>", _sliderLabelStyle);
        d.powerFactor = DrawSlider(d.powerFactor, 0.85f, 1.0f);
        GUILayout.Space(8);
    }

    // ─── EV Charging Console ──────────────────────────────────────────────────

    private void DrawEvConsole(EvTelemetryData d)
    {
        DrawKpiGrid(
            $"{d.CurrentOutputMw:F1} MW", "CHARGING LOAD",
            $"{d.CapacityFactor:F0}%", "PORT UTILIZATION",
            $"{d.ratedCapacityMw:F1} MW", $"RATED CAP ({d.activeCount} HUBS)",
            $"{d.ActiveSessions}", "ACTIVE SESSIONS"
        );

        GUILayout.Label("OPERATING PARAMETERS", _sectionTitleStyle);

        // Port utilization
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Port Utilization: <b>{d.utilizationPercent:F0}%</b>", _sliderLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{d.TotalPorts} ports", _subHeaderStyle);
        GUILayout.EndHorizontal();
        d.utilizationPercent = DrawSlider(d.utilizationPercent, 0.0f, 100.0f);
        GUILayout.Space(4);

        // Renewable share
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Renewable-Powered Share: <b>{d.renewableSharePercent:F0}%</b>", _sliderLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(GetEvCleanRating(d.renewableSharePercent), _subHeaderStyle);
        GUILayout.EndHorizontal();
        d.renewableSharePercent = DrawSlider(d.renewableSharePercent, 0.0f, 100.0f);
        GUILayout.Space(4);

        // Charger capacity
        GUILayout.Label($"Installed Charging Capacity: <b>{d.ratedCapacityMw:F1} MW</b>", _sliderLabelStyle);
        d.ratedCapacityMw = DrawSlider(d.ratedCapacityMw, 0.5f, 20.0f);
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Vehicles Served / Day: <b>{d.VehiclesServedDaily:N0}</b>", _ecoMetricRowStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Daily Energy: <b>{d.DailyEnergyMwh:F0} MWh</b>", _ecoMetricRowStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        // Charging mode
        GUILayout.Label("Charging Mode", _sectionTitleStyle);
        GUILayout.BeginHorizontal();
        if (DrawModeButton("Smart (Clean)", d.operatingStatus == "SMART_CHARGING")) d.operatingStatus = "SMART_CHARGING";
        if (DrawModeButton("Full Power", d.operatingStatus == "FULL_POWER")) d.operatingStatus = "FULL_POWER";
        if (DrawModeButton("Offline", d.operatingStatus == "OFFLINE")) d.operatingStatus = "OFFLINE";
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
    }

    private string GetEvCleanRating(float share) => share switch
    {
        < 40f => "Grid-Dependent",
        < 70f => "Mostly Clean",
        < 95f => "High Renewable",
        _     => "100% Clean"
    };

    private bool DrawModeButton(string label, bool isSelected)
    {
        return GUILayout.Button(label, isSelected ? _activeButtonStyle : _buttonStyle, GUILayout.ExpandWidth(true));
    }

    // ─── Municipal Decarbonization Gauge ──────────────────────────────────────

    private void DrawCityDecarbonizationGauge()
    {
        GUILayout.Space(4);

        float windOut = EnergyManager.Instance.WindData.CurrentOutputMw;
        float solarOut = EnergyManager.Instance.SolarData.CurrentOutputMw;
        float storageOut = EnergyManager.Instance.StorageData.powerFlowMw;
        float totalClean = windOut + solarOut + Mathf.Max(0f, storageOut);
        float demand = EnergyManager.Instance.GridData.cityDemandMw;
        float selfSufficiency = demand > 0f ? (totalClean / demand) * 100f : 0f;

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("CLEAN ENERGY SELF-SUFFICIENCY", _subHeaderStyle);
        GUILayout.FlexibleSpace();
        if (selfSufficiency >= 100f)
            GUILayout.Label("<color=#6EE7B7>100% (Net Exporter)</color>", _subHeaderStyle);
        else
            GUILayout.Label($"{selfSufficiency:F0}% Renewable", _subHeaderStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Renewable: <b>{totalClean:F1} MW</b>  /  Demand: <b>{demand:F1} MW</b>", _sliderLabelStyle);
        GUILayout.FlexibleSpace();
        // CEA India grid emission factor (tCO2/MWh) — see SustainabilityDatasets.cs.
        float avoidedCo2 = totalClean * SustainabilityDatasets.GridEmissionFactorTPerMwh;
        GUILayout.Label($"Avoided: <b>{avoidedCo2:F1} t/h</b>", _subHeaderStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        // Translucent glass progress bar track
        Rect trackRect = GUILayoutUtility.GetRect(100, 6);
        GUI.DrawTexture(trackRect, _progressTrackTex);
        float fillWidth = trackRect.width * Mathf.Clamp01(selfSufficiency / 100f);
        if (fillWidth > 0f)
        {
            Rect fillRect = new Rect(trackRect.x, trackRect.y, fillWidth, trackRect.height);
            GUI.DrawTexture(fillRect, _progressFillTex);
        }

        GUILayout.EndVertical();
        GUILayout.Space(6);
    }

    private void DrawFooter(EnergyType currentType)
    {
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Optimize", _activeButtonStyle, GUILayout.Height(26)))
        {
            switch (currentType)
            {
                case EnergyType.Wind:
                    EnergyManager.Instance.WindData.SetEcoOptimal();
                    EnergyManager.Instance.UpdateWindTurbineSpeeds();
                    break;
                case EnergyType.Solar:
                    EnergyManager.Instance.SolarData.SetEcoOptimal();
                    break;
                case EnergyType.Storage:
                    EnergyManager.Instance.StorageData.SetEcoOptimal();
                    break;
                case EnergyType.Grid:
                    EnergyManager.Instance.GridData.SetEcoOptimal();
                    break;
                case EnergyType.EV:
                    EnergyManager.Instance.EvData.SetEcoOptimal();
                    break;
            }
        }

        if (GUILayout.Button("Reset Defaults", _buttonStyle, GUILayout.Height(26)))
        {
            switch (currentType)
            {
                case EnergyType.Wind:
                    EnergyManager.Instance.WindData.ResetDefaults();
                    EnergyManager.Instance.UpdateWindTurbineSpeeds();
                    break;
                case EnergyType.Solar:
                    EnergyManager.Instance.SolarData.ResetDefaults();
                    break;
                case EnergyType.Storage:
                    EnergyManager.Instance.StorageData.ResetDefaults();
                    break;
                case EnergyType.Grid:
                    EnergyManager.Instance.GridData.ResetDefaults();
                    break;
                case EnergyType.EV:
                    EnergyManager.Instance.EvData.ResetDefaults();
                    break;
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Deselect", _dangerButtonStyle, GUILayout.Width(84), GUILayout.Height(26)))
        {
            EnergyManager.Instance.Deselect();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(4);
        GUILayout.Label("Press ESC or click open space to close console", _tipStyle);
    }

    // ─── Floating 3D HUD Badges ───────────────────────────────────────────────

    private void DrawWorldBadges(EnergyType type)
    {
        var sources = EnergyManager.Instance.GetSources(type);
        if (sources == null || sources.Count == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        int limit = Mathf.Min(sources.Count, 24);
        for (int i = 0; i < limit; i++)
        {
            EnergySource src = sources[i];
            if (src == null) continue;

            Vector3 worldPos = src.transform.position + Vector3.up * 8f;
            Vector3 camForward = cam.transform.forward;
            Vector3 toObj = worldPos - cam.transform.position;

            if (Vector3.Dot(camForward, toObj) <= 0.4f) continue;
            float dist = toObj.magnitude;
            if (dist > 400f) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            float x = screenPos.x;
            float y = Screen.height - screenPos.y;

            string badge = type switch
            {
                EnergyType.Wind    => $"Wind {(i + 1):D2}",
                EnergyType.Solar   => $"Solar {(i + 1):D2}",
                EnergyType.Storage => $"Storage {(i + 1):D2}",
                EnergyType.Grid    => "Substation",
                EnergyType.EV      => $"EV Hub {(i + 1):D2}",
                _                  => "Energy Unit"
            };

            float badgeW = 68f;
            float badgeH = 18f;
            GUI.Label(new Rect(x - badgeW * 0.5f, y - badgeH * 0.5f, badgeW, badgeH), badge, _badgeStyle);
        }
    }
}
