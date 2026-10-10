# Environmental Impact Telemetry — Data Sources & Methodology

The energy console's **Environmental Impact Telemetry** (CO₂ avoided, clean homes,
forest equivalent, carbon intensity) is not made up — every number is computed from
**published, citable reference datasets** applied to the **live clean-energy output**
of the city's wind / solar / storage / grid sources.

- **Machine-readable dataset:** [`City/Assets/Resources/sustainability_datasets.json`](../City/Assets/Resources/sustainability_datasets.json)
- **Loader + formulas:** [`City/Assets/Scripts/Energy/SustainabilityDatasets.cs`](../City/Assets/Scripts/Energy/SustainabilityDatasets.cs)
- **Display:** [`City/Assets/Scripts/Energy/EnergyConsoleUI.cs`](../City/Assets/Scripts/Energy/EnergyConsoleUI.cs) (`DrawSustainabilityImpactBanner`)

---

## 1. Datasets used

### A. Grid emission factor — **CEA CO₂ Baseline Database (India)**
| | |
|---|---|
| **Value** | **0.727 tCO₂ / MWh** (FY 2023-24 weighted average; FY 2024-25 provisional 0.710) |
| **Publisher** | Central Electricity Authority (CEA), Government of India |
| **Dataset** | *CO₂ Baseline Database for the Indian Power Sector, Version 21.0* |
| **Link** | https://cea.nic.in/cdm-co2-baseline-database/ |
| **Used for** | How much CO₂ the fossil grid *would* have emitted to produce the same electricity → basis of "CO₂ Avoided" |

### B. Lifecycle carbon intensity — **IPCC AR5 WG3 Annex III**
| Source | Median lifecycle emissions (gCO₂eq/kWh) |
|---|---|
| Wind (onshore) | **11** |
| Solar PV (utility) | **48** |
| Battery storage | **25** (round-trip overhead estimate) |
| Coal (pulverized) | 820 |
| Natural gas (CCGT) | 490 |

- **Publisher:** IPCC, Fifth Assessment Report, Working Group III, Annex III (Schlömer et al., 2014)
- **Link:** https://www.ipcc.ch/site/assets/uploads/2018/02/ipcc_wg3_ar5_annex-iii.pdf
- **Used for:** the real **Carbon Intensity** of each clean source (not 0), and subtracted from the grid factor to get *net* CO₂ avoided.

### C. Household electricity consumption
| | |
|---|---|
| **Value** | **1,937 kWh / household / year** (India, urban average; rural ≈ 1,486) |
| **Source** | India household electricity-consumption survey (reported via Down To Earth) |
| **Link** | https://www.downtoearth.org.in/energy/cooling-appliances-now-drive-indias-household-electricity-use |
| **Used for** | "Clean Homes" — how many average homes the output could power |
| **Localise** | Swap `householdConsumption.annual_kWh` in the JSON per region (e.g. US EIA ≈ 10,500 kWh/yr) |

### D. Tree CO₂ sequestration — **US EPA / urban forestry**
| | |
|---|---|
| **Value** | **≈ 21.8 kg CO₂ / tree / year** (⇒ ~45.9 trees per tonne CO₂/yr) |
| **Source** | US EPA Greenhouse Gas Equivalencies Calculator (urban-forestry basis) |
| **Link** | https://www.epa.gov/energy/greenhouse-gases-equivalencies-calculator-calculations-and-references |
| **Used for** | "Forest Equivalent" — trees whose annual CO₂ uptake equals the CO₂ avoided |

---

## 2. Formulas

Let `P` = live clean output (MW), `GF` = grid factor (gCO₂/kWh = 727), `LC` = this
source's lifecycle intensity (gCO₂eq/kWh), `H` = household consumption (kWh/yr),
`T` = tree sequestration (tCO₂/yr).

```
Net avoided factor  = max(0, (GF − LC) / 1000)        # tCO₂ / MWh
CO₂ Avoided (t/h)   = P × Net avoided factor
CO₂ Avoided (t/d)   = CO₂ Avoided (t/h) × 24
Clean Homes         = (P × 1000) / (H / 8760)         # output_kW ÷ avg home kW
Forest Equivalent   = CO₂ Avoided (t/d) × (1 / T)     # trees
Carbon Intensity    = LC  gCO₂e/kWh   (shown vs grid GF)
```

## 3. Worked example — Solar farm at 102 MW

| Metric | Calculation | Result |
|---|---|---|
| Net avoided factor | (727 − 48) / 1000 | 0.679 tCO₂/MWh |
| CO₂ Avoided | 102 × 0.679 | **69.3 t/h (1,662 t/d)** |
| Clean Homes | 102,000 kW ÷ (1,937/8760 kW) | **≈ 461,000** |
| Forest Equivalent | 1,662 × 45.9 | **≈ 76,200 trees** |
| Carbon Intensity | lifecycle of solar PV | **48 gCO₂e/kWh** (vs grid 727) |

> All figures scale live with the slider-controlled output, so the console shows
> real, dataset-grounded estimates for any operating point.
