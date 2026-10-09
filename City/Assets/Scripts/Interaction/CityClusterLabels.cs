using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Click-to-identify labels for the city.
///
/// Clicking anywhere on the ground inside a sector pops a floating 3D tag naming
/// that cluster ("Residential Area", "Commercial Area", …). Clicking an energy
/// asset (wind turbine, solar array, battery, substation, EV hub) pops a tag
/// naming the asset ("Wind Turbine", "EV Charging Station", "Power Grid", …) —
/// the existing EnergyConsoleUI still opens on the same click, independently.
///
/// Self-bootstrapping: CityGenerator calls EnsureExists().RegisterClusters(...)
/// after generation. The manager lives on its own persistent GameObject (like
/// EnergyManager), so it survives ClearCity()/regeneration.
/// </summary>
public sealed class CityClusterLabels : MonoBehaviour
{
    public static CityClusterLabels Instance { get; private set; }

    // ── Cluster registry ──────────────────────────────────────────────────────
    private struct Cluster
    {
        public float minX, maxX, minZ, maxZ;
        public float area;
        public string title;    // e.g. "Commercial Area"
        public string subtitle; // e.g. "Central Business District"
        public Color accent;
    }

    private readonly List<Cluster> _clusters = new List<Cluster>();

    // ── Current label state ───────────────────────────────────────────────────
    private bool _hasLabel;
    private Vector3 _anchorWorld;
    private string _labelTitle;
    private string _labelSubtitle;
    private Color _labelAccent = Color.white;

    private const float RayDistance = 10000f;

    // ── GUI ───────────────────────────────────────────────────────────────────
    private bool _styleReady;
    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private Texture2D _tagTex;
    private Texture2D _dotTex;

    // ─────────────────────────────────────────────────────────────────────────

    public static CityClusterLabels EnsureExists()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("CityClusterLabels");
        return go.AddComponent<CityClusterLabels>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_tagTex != null) Destroy(_tagTex);
        if (_dotTex != null) Destroy(_dotTex);
    }

    /// <summary>Rebuild the clickable-cluster lookup from the current city plan.</summary>
    public void RegisterClusters(List<SectorData> sectors)
    {
        _clusters.Clear();
        _hasLabel = false;

        if (sectors == null) return;

        foreach (SectorData sector in sectors)
        {
            float[] b = sector?.geometry?.bounds;
            if (b == null || b.Length < 4) continue;

            float w = Mathf.Abs(b[2]);
            float d = Mathf.Abs(b[3]);
            if (w < 1f || d < 1f) continue;

            bool isEnergy = string.Equals(sector.type, "energy", StringComparison.OrdinalIgnoreCase);

            // EnergyGenerator treats energy-sector bounds[0],[1] as the CENTRE of the
            // installation; every other generator treats them as the lower-left corner.
            float minX, minZ;
            if (isEnergy)
            {
                minX = b[0] - w * 0.5f;
                minZ = b[1] - d * 0.5f;
            }
            else
            {
                minX = b[0];
                minZ = b[1];
            }

            _clusters.Add(new Cluster
            {
                minX = minX,
                maxX = minX + w,
                minZ = minZ,
                maxZ = minZ + d,
                area = w * d,
                title = TitleForSector(sector.type),
                subtitle = !string.IsNullOrEmpty(sector.name) ? sector.name : sector.id,
                accent = AccentForSector(sector.type)
            });
        }
    }

    // ── Input ──────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _hasLabel = false;
            return;
        }

        if (Mouse.current == null || Camera.main == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Don't react to clicks inside the energy console window.
        if (EnergyConsoleUI.Instance != null && EnergyConsoleUI.Instance.IsPointInsideConsole(mousePos))
            return;

        Ray ray = Cursor.lockState == CursorLockMode.Locked
            ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, RayDistance))
        {
            _hasLabel = false;
            return;
        }

        // 1) Energy asset? (its own collider is hit before the ground)
        EnergySource source = hit.collider.GetComponentInParent<EnergySource>();
        if (source != null)
        {
            ShowEnergyLabel(source);
            return;
        }

        // 2) Otherwise resolve which sector/cluster the hit point lands in.
        ShowClusterLabel(hit.point);
    }

    private void ShowEnergyLabel(EnergySource source)
    {
        _anchorWorld = source.transform.position + Vector3.up * 6f;
        (_labelTitle, _labelSubtitle, _labelAccent) = LabelForEnergy(source.sourceType);
        _hasLabel = true;
    }

    private void ShowClusterLabel(Vector3 hitPoint)
    {
        int best = -1;
        float bestArea = float.MaxValue;
        for (int i = 0; i < _clusters.Count; i++)
        {
            Cluster c = _clusters[i];
            if (hitPoint.x >= c.minX && hitPoint.x <= c.maxX &&
                hitPoint.z >= c.minZ && hitPoint.z <= c.maxZ &&
                c.area < bestArea)
            {
                best = i;
                bestArea = c.area;
            }
        }

        if (best < 0)
        {
            _hasLabel = false;
            return;
        }

        Cluster cl = _clusters[best];
        _anchorWorld = new Vector3(hitPoint.x, hitPoint.y + 18f, hitPoint.z);
        _labelTitle = cl.title;
        _labelSubtitle = cl.subtitle;
        _labelAccent = cl.accent;
        _hasLabel = true;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!_hasLabel) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(_anchorWorld);
        if (screen.z <= 0f) return; // behind camera

        EnsureStyle();

        bool hasSub = !string.IsNullOrEmpty(_labelSubtitle);
        float titleW = _titleStyle.CalcSize(new GUIContent(_labelTitle)).x;
        float subW = hasSub ? _subtitleStyle.CalcSize(new GUIContent(_labelSubtitle)).x : 0f;

        const float padX = 14f;
        const float dot = 9f;
        float contentW = Mathf.Max(titleW, subW) + dot + 6f;
        float w = contentW + padX * 2f;
        float h = hasSub ? 42f : 28f;

        float x = screen.x - w * 0.5f;
        float y = (Screen.height - screen.y) - h - 6f; // float just above the anchor

        x = Mathf.Clamp(x, 4f, Screen.width - w - 4f);
        y = Mathf.Clamp(y, 4f, Screen.height - h - 4f);

        Rect box = new Rect(x, y, w, h);

        Color prev = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(box, _tagTex, ScaleMode.StretchToFill);

        // accent dot
        GUI.color = _labelAccent;
        GUI.DrawTexture(new Rect(x + padX, y + (hasSub ? 11f : h * 0.5f - dot * 0.5f), dot, dot), _dotTex, ScaleMode.StretchToFill);
        GUI.color = prev;

        float textX = x + padX + dot + 6f;
        float textW = w - (padX + dot + 6f) - padX;
        if (hasSub)
        {
            GUI.Label(new Rect(textX, y + 5f, textW, 16f), _labelTitle, _titleStyle);
            GUI.Label(new Rect(textX, y + 22f, textW, 14f), _labelSubtitle, _subtitleStyle);
        }
        else
        {
            GUI.Label(new Rect(textX, y + 6f, textW, 16f), _labelTitle, _titleStyle);
        }
    }

    private void EnsureStyle()
    {
        if (_styleReady) return;

        _tagTex = MakeRoundedTex(28, 28, 8f,
            new Color(0.06f, 0.08f, 0.11f, 0.86f),
            new Color(1f, 1f, 1f, 0.18f));
        _dotTex = MakeRoundedTex(12, 12, 6f, Color.white, Color.white);

        _titleStyle = new GUIStyle
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.96f, 0.98f, 1f) }
        };
        _subtitleStyle = new GUIStyle
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.62f, 0.70f, 0.80f) }
        };

        _styleReady = true;
    }

    // ── Mappings ────────────────────────────────────────────────────────────

    private static string TitleForSector(string type)
    {
        switch ((type ?? "").ToLowerInvariant())
        {
            case "residential": return "Residential Area";
            case "commercial":  return "Commercial Area";
            case "mixed_use":   return "Mixed-Use Area";
            case "industrial":  return "Industrial Area";
            case "civic":
            case "institutional": return "Civic Area";
            case "park":        return "Park";
            case "forest":
            case "green":       return "Green Space";
            case "energy":      return "Energy Zone";
            case "transport":   return "Transport Hub";
            case "water":       return "Water Body";
            default:
                if (string.IsNullOrEmpty(type)) return "District";
                return char.ToUpperInvariant(type[0]) + type.Substring(1).Replace('_', ' ') + " Area";
        }
    }

    private static Color AccentForSector(string type)
    {
        switch ((type ?? "").ToLowerInvariant())
        {
            case "residential": return new Color(0.98f, 0.78f, 0.36f); // warm amber
            case "commercial":  return new Color(0.36f, 0.70f, 1.0f);  // blue
            case "mixed_use":   return new Color(0.72f, 0.56f, 1.0f);  // violet
            case "industrial":  return new Color(0.80f, 0.52f, 0.42f); // terracotta
            case "civic":
            case "institutional": return new Color(0.60f, 0.85f, 0.95f); // pale cyan
            case "park":
            case "forest":
            case "green":       return new Color(0.42f, 0.86f, 0.52f); // green
            case "energy":      return new Color(0.20f, 0.98f, 0.70f); // emerald
            case "water":       return new Color(0.35f, 0.75f, 0.95f); // water blue
            default:            return new Color(0.80f, 0.84f, 0.90f);
        }
    }

    private static (string, string, Color) LabelForEnergy(EnergyType type)
    {
        switch (type)
        {
            case EnergyType.Wind:    return ("Wind Turbine",        "Renewable Generation", new Color(0.18f, 0.95f, 0.78f));
            case EnergyType.Solar:   return ("Solar Array",         "Renewable Generation", new Color(1.0f, 0.85f, 0.18f));
            case EnergyType.Storage: return ("Battery Storage",     "Grid Buffer",          new Color(0.15f, 0.82f, 0.70f));
            case EnergyType.Grid:    return ("Power Grid",          "Distribution & Substation", new Color(0.20f, 0.98f, 0.45f));
            case EnergyType.EV:      return ("EV Charging Station", "Clean Mobility Hub",   new Color(0.15f, 0.70f, 1.0f));
            default:                 return ("Energy Asset",        "", Color.white);
        }
    }

    private static Texture2D MakeRoundedTex(int width, int height, float radius, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float ix = Mathf.Clamp(px, radius, width - radius);
                float iy = Mathf.Clamp(py, radius, height - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(ix, iy));

                if (dist > radius)
                {
                    float a = Mathf.Clamp01(1f - (dist - radius));
                    tex.SetPixel(x, y, a > 0f ? new Color(border.r, border.g, border.b, border.a * a) : Color.clear);
                }
                else if (dist > radius - 1f)
                {
                    tex.SetPixel(x, y, border);
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
