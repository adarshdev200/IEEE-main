using System;
using UnityEngine;

[Serializable]
public class WindTelemetryData
{
    public int activeCount = 12;
    public float ratedCapacityMw = 30f;
    public float windSpeedMs = 7.5f;
    public float efficiencyPercent = 42.0f;
    public string operatingStatus = "OPERATIONAL";

    public float defaultRatedCapacity = 30f;
    public float defaultWindSpeed = 7.5f;
    public float defaultEfficiency = 42.0f;

    public float CurrentOutputMw
    {
        get
        {
            if (operatingStatus == "MAINTENANCE") return 0f;
            if (operatingStatus == "CURTAILED") return ratedCapacityMw * 0.25f;

            float v = windSpeedMs;
            const float cutIn = 3.0f;
            const float rated = 12.0f;
            const float cutOut = 25.0f;

            if (v < cutIn || v > cutOut) return 0f;
            float effRatio = efficiencyPercent / 42.0f;

            if (v >= rated) return ratedCapacityMw * effRatio;

            float norm = (v - cutIn) / (rated - cutIn);
            return ratedCapacityMw * Mathf.Pow(norm, 3f) * effRatio;
        }
    }

    public float CapacityFactor => ratedCapacityMw > 0f ? Mathf.Clamp01(CurrentOutputMw / ratedCapacityMw) * 100f : 0f;
    public float DailyYieldMwh => CurrentOutputMw * 24f;
    public float Co2OffsetTonsPerHour => CurrentOutputMw * 0.72f;
    public int HomesPowered => (int)(CurrentOutputMw * 1000f / 1.4f);
    public int TreesPlantedEquivalent => (int)(Co2OffsetTonsPerHour * 24f * 45f);

    public void SetEcoOptimal()
    {
        windSpeedMs = 12.5f; // optimal clean generation speed
        efficiencyPercent = 48.0f; // tuned aerodynamics
        operatingStatus = "OPERATIONAL";
    }

    public void ResetDefaults()
    {
        ratedCapacityMw = defaultRatedCapacity;
        windSpeedMs = defaultWindSpeed;
        efficiencyPercent = defaultEfficiency;
        operatingStatus = "OPERATIONAL";
    }
}

[Serializable]
public class SolarTelemetryData
{
    public int activeCount = 64;
    public float ratedCapacityMw = 100f;
    public float solarIrradianceWm2 = 850f;
    public float panelEfficiencyPercent = 21.5f;
    public float tiltDegrees = 15f;
    public float temperatureC = 25f;
    public string operatingStatus = "OPERATIONAL";

    public float defaultRatedCapacity = 100f;
    public float defaultIrradiance = 850f;
    public float defaultEfficiency = 21.5f;
    public float defaultTilt = 15f;
    public float defaultTemp = 25f;

    public float CurrentOutputMw
    {
        get
        {
            if (operatingStatus == "OFFLINE") return 0f;
            if (operatingStatus == "REDUCED") return ratedCapacityMw * 0.3f;

            float irrFactor = Mathf.Clamp01(solarIrradianceWm2 / 1000f);
            float tempFactor = 1f - 0.004f * Mathf.Max(0f, temperatureC - 25f);
            float tiltFactor = Mathf.Cos(Mathf.Abs(tiltDegrees - 15f) * Mathf.Deg2Rad);
            float effRatio = panelEfficiencyPercent / 21.5f;

            float outMw = ratedCapacityMw * irrFactor * tempFactor * tiltFactor * effRatio;
            return Mathf.Clamp(outMw, 0f, ratedCapacityMw * 1.15f);
        }
    }

    public float CapacityFactor => ratedCapacityMw > 0f ? Mathf.Clamp01(CurrentOutputMw / ratedCapacityMw) * 100f : 0f;
    public float DailyYieldMwh => CurrentOutputMw * 5.2f;
    public float Co2OffsetTonsPerHour => CurrentOutputMw * 0.72f;
    public int HomesPowered => (int)(CurrentOutputMw * 1000f / 1.4f);
    public int TreesPlantedEquivalent => (int)(Co2OffsetTonsPerHour * 24f * 45f);

    public void SetEcoOptimal()
    {
        solarIrradianceWm2 = 1000f; // peak sunlight
        tiltDegrees = 15f; // optimal solar angle
        panelEfficiencyPercent = 24.5f; // high-efficiency PV cells
        temperatureC = 25f;
        operatingStatus = "OPERATIONAL";
    }

    public void ResetDefaults()
    {
        ratedCapacityMw = defaultRatedCapacity;
        solarIrradianceWm2 = defaultIrradiance;
        panelEfficiencyPercent = defaultEfficiency;
        tiltDegrees = defaultTilt;
        temperatureC = defaultTemp;
        operatingStatus = "OPERATIONAL";
    }
}

[Serializable]
public class StorageTelemetryData
{
    public int activeCount = 3;
    public float storageCapacityMwh = 150f;
    public float maxPowerMw = 50f;
    public float stateOfChargePercent = 78f;
    public float powerFlowMw = 22f; // Positive = Discharging to grid, Negative = Charging from renewables
    public float roundTripEfficiencyPercent = 92.5f;
    public string operatingMode = "PEAK_SHAVING";

    public float defaultStorageCapacity = 150f;
    public float defaultMaxPower = 50f;
    public float defaultSoc = 78f;
    public float defaultPowerFlow = 22f;
    public float defaultEfficiency = 92.5f;

    public float StoredEnergyMwh => storageCapacityMwh * (stateOfChargePercent / 100f);

    public float EstimatedHoursRemaining
    {
        get
        {
            if (powerFlowMw > 0.5f)
            {
                return StoredEnergyMwh / powerFlowMw;
            }
            if (powerFlowMw < -0.5f)
            {
                return (storageCapacityMwh - StoredEnergyMwh) / Mathf.Abs(powerFlowMw);
            }
            return 99.9f;
        }
    }

    public float FossilPeakerOffsetMw => powerFlowMw > 0f ? powerFlowMw : 0f;

    public void SetEcoOptimal()
    {
        stateOfChargePercent = 90f;
        powerFlowMw = 25f; // clean peaker discharge
        roundTripEfficiencyPercent = 95f;
        operatingMode = "PEAK_SHAVING";
    }

    public void ResetDefaults()
    {
        storageCapacityMwh = defaultStorageCapacity;
        maxPowerMw = defaultMaxPower;
        stateOfChargePercent = defaultSoc;
        powerFlowMw = defaultPowerFlow;
        roundTripEfficiencyPercent = defaultEfficiency;
        operatingMode = "PEAK_SHAVING";
    }
}

[Serializable]
public class GridTelemetryData
{
    public int activeCount = 1;
    public float substationRatingMva = 120f;
    public float gridVoltageKv = 110f;
    public float gridFrequencyHz = 50.0f;
    public float powerFactor = 0.98f;
    public float transmissionLossPercent = 4.0f;
    public float cityDemandMw = 95.0f;
    public bool isGridConnected = true;

    public float defaultVoltage = 110f;
    public float defaultFrequency = 50.0f;
    public float defaultPowerFactor = 0.98f;
    public float defaultLoss = 4.0f;
    public float defaultDemand = 95.0f;

    public void SetEcoOptimal()
    {
        transmissionLossPercent = 2.5f; // ultra-low loss superconducting microgrid
        powerFactor = 0.99f;
        gridFrequencyHz = 50.0f;
    }

    public void ResetDefaults()
    {
        gridVoltageKv = defaultVoltage;
        gridFrequencyHz = defaultFrequency;
        powerFactor = defaultPowerFactor;
        transmissionLossPercent = defaultLoss;
        cityDemandMw = defaultDemand;
        isGridConnected = true;
    }
}

[Serializable]
public class EvTelemetryData
{
    public int activeCount = 8;                 // number of charging stations / hubs
    public int portsPerStation = 8;             // charging ports per hub
    public float ratedCapacityMw = 3.0f;        // total installed charging capacity
    public float utilizationPercent = 55f;      // share of ports currently delivering power
    public float renewableSharePercent = 82f;   // % of charging energy served by clean sources
    public float chargerEfficiencyPercent = 94f;
    public string operatingStatus = "SMART_CHARGING"; // SMART_CHARGING | FULL_POWER | OFFLINE

    public float defaultRatedCapacity = 3.0f;
    public float defaultUtilization = 55f;
    public float defaultRenewableShare = 82f;
    public float defaultEfficiency = 94f;

    // Power currently dispatched to vehicles (treated like a generator "output" in the console).
    public float CurrentOutputMw
    {
        get
        {
            if (operatingStatus == "OFFLINE") return 0f;
            float util = operatingStatus == "FULL_POWER" ? 100f : utilizationPercent;
            float eff = chargerEfficiencyPercent / 100f;
            return Mathf.Clamp(ratedCapacityMw * (util / 100f) * eff, 0f, ratedCapacityMw);
        }
    }

    public int TotalPorts => activeCount * portsPerStation;
    public int ActiveSessions => Mathf.RoundToInt(TotalPorts * Mathf.Clamp01(utilizationPercent / 100f));
    public float CapacityFactor => ratedCapacityMw > 0f ? Mathf.Clamp01(CurrentOutputMw / ratedCapacityMw) * 100f : 0f;
    public float DailyEnergyMwh => CurrentOutputMw * 12f; // ~12 effective charging-hours/day
    // CO2 avoided by displacing ICE miles with (renewable-weighted) electric miles.
    public float Co2OffsetTonsPerHour => CurrentOutputMw * (renewableSharePercent / 100f) * 0.62f;
    public int VehiclesServedDaily => (int)(DailyEnergyMwh * 1000f / 40f); // ~40 kWh per top-up
    public int HomesPowered => (int)(CurrentOutputMw * 1000f / 1.4f);

    public void SetEcoOptimal()
    {
        renewableSharePercent = 100f;      // charge entirely from solar/wind surplus
        utilizationPercent = 70f;          // demand-shifted to clean-generation windows
        chargerEfficiencyPercent = 97f;
        operatingStatus = "SMART_CHARGING";
    }

    public void ResetDefaults()
    {
        ratedCapacityMw = defaultRatedCapacity;
        utilizationPercent = defaultUtilization;
        renewableSharePercent = defaultRenewableShare;
        chargerEfficiencyPercent = defaultEfficiency;
        operatingStatus = "SMART_CHARGING";
    }
}
