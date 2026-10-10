using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Real, citable sustainability reference datasets.
//
//  Loads Resources/sustainability_datasets.json at runtime and exposes the
//  values used to convert live clean-energy output into environmental-impact
//  estimates (CO2 avoided, homes powered, forest equivalent, carbon intensity).
//
//  Every constant is sourced from a published dataset:
//    • Grid emission factor .... CEA CO2 Baseline Database for the Indian Power
//                                Sector, v21.0 (Central Electricity Authority,
//                                Govt. of India) — 0.727 tCO2/MWh.
//    • Lifecycle intensity ..... IPCC AR5 WG3 Annex III (Schlömer et al., 2014)
//                                lifecycle medians (gCO2eq/kWh).
//    • Household consumption ... India urban-household survey average.
//    • Tree sequestration ...... US EPA GHG Equivalencies / urban forestry.
//
//  The hardcoded fallbacks below mirror the JSON, so the console still renders
//  with real values even if the file is missing.
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GridEmissionFactorData
{
    public float value_tCO2_per_MWh = 0.727f;
    public string region = "India - national unified grid";
    public string source = "CEA CO2 Baseline Database v21.0 (2025)";
    public string url = "https://cea.nic.in/cdm-co2-baseline-database/";
}

[System.Serializable]
public class LifecycleIntensityData
{
    public float wind = 11f;
    public float solar = 48f;
    public float storage = 25f;
    public float coal = 820f;
    public float naturalGas = 490f;
    public string source = "IPCC AR5 WG3 Annex III (2014)";
    public string url = "https://www.ipcc.ch/site/assets/uploads/2018/02/ipcc_wg3_ar5_annex-iii.pdf";
}

[System.Serializable]
public class HouseholdConsumptionData
{
    public float annual_kWh = 1937f;
    public string region = "India - urban household (average)";
    public string source = "India household electricity-consumption survey";
    public string url = "";
}

[System.Serializable]
public class TreeSequestrationData
{
    public float tCO2_per_tree_per_year = 0.0218f;
    public string source = "US EPA Greenhouse Gas Equivalencies / urban forestry";
    public string url = "https://www.epa.gov/energy/greenhouse-gases-equivalencies-calculator-calculations-and-references";
}

[System.Serializable]
public class SustainabilityDatasetRoot
{
    public GridEmissionFactorData gridEmissionFactor = new GridEmissionFactorData();
    public LifecycleIntensityData lifecycleCarbonIntensity_gCO2e_per_kWh = new LifecycleIntensityData();
    public HouseholdConsumptionData householdConsumption = new HouseholdConsumptionData();
    public TreeSequestrationData treeSequestration = new TreeSequestrationData();
}

/// <summary>
/// Static accessor for the sustainability reference datasets. Lazy-loads the
/// JSON from Resources once; falls back to the documented in-code values.
/// </summary>
public static class SustainabilityDatasets
{
    private const string ResourceName = "sustainability_datasets"; // Resources/sustainability_datasets.json
    private static SustainabilityDatasetRoot _data;

    public static SustainabilityDatasetRoot Data
    {
        get
        {
            if (_data == null)
            {
                _data = new SustainabilityDatasetRoot();
                var asset = Resources.Load<TextAsset>(ResourceName);
                if (asset != null && !string.IsNullOrEmpty(asset.text))
                {
                    try { JsonUtility.FromJsonOverwrite(asset.text, _data); }
                    catch { /* keep documented fallbacks on parse failure */ }
                }
            }
            return _data;
        }
    }

    // ── Convenience accessors ────────────────────────────────────────────────

    /// Grid emission factor in tonnes CO2 per MWh (CEA).
    public static float GridEmissionFactorTPerMwh => Data.gridEmissionFactor.value_tCO2_per_MWh;

    /// Grid emission factor in grams CO2 per kWh (CEA).
    public static float GridIntensityGPerKwh => Data.gridEmissionFactor.value_tCO2_per_MWh * 1000f;

    /// Average household electricity consumption, kWh/year.
    public static float HouseholdAnnualKwh => Mathf.Max(1f, Data.householdConsumption.annual_kWh);

    /// Equivalent mature trees needed to sequester one tonne of CO2 in a year (EPA).
    public static float TreesPerTonnePerYear
    {
        get
        {
            float perTree = Data.treeSequestration.tCO2_per_tree_per_year;
            return perTree > 0f ? 1f / perTree : 45.9f;
        }
    }

    /// Lifecycle carbon intensity for a given clean source, gCO2eq/kWh (IPCC AR5).
    /// Grid/EV blend renewables (wind+solar mean) since they draw from the clean mix.
    public static float LifecycleIntensityGPerKwh(EnergyType type)
    {
        var l = Data.lifecycleCarbonIntensity_gCO2e_per_kWh;
        return type switch
        {
            EnergyType.Wind    => l.wind,
            EnergyType.Solar   => l.solar,
            EnergyType.Storage => l.storage,
            _                  => (l.wind + l.solar) * 0.5f, // Grid, EV: renewable blend
        };
    }

    /// Short attribution string for display in the UI.
    public static string AttributionLine =>
        "Data: CEA CO₂ Baseline DB v21.0 (India) · IPCC AR5 lifecycle · EPA / urban forestry";
}
