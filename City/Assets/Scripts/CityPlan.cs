// CityPlan.cs
// Complete C# data model mirroring CityPlan.schema.json
// Uses Newtonsoft.Json for serialization. Do NOT put Unity types here.

using System.Collections.Generic;
using Newtonsoft.Json;

// ─── Root ────────────────────────────────────────────────────────────────────

public class CityPlan
{
    [JsonProperty("city")]       public CityData         city        = new CityData();
    [JsonProperty("terrain")]    public TerrainData      terrain     = new TerrainData();
    [JsonProperty("climate")]    public ClimateData      climate     = new ClimateData();
    [JsonProperty("land_use")]   public LandUseData      land_use    = new LandUseData();
    [JsonProperty("sectors")]    public List<SectorData> sectors     = new List<SectorData>();
    [JsonProperty("road_graph")] public RoadGraphData    road_graph  = new RoadGraphData();
    [JsonProperty("blocks")]     public BlocksData       blocks      = new BlocksData();
    [JsonProperty("buildings")]  public BuildingsData    buildings   = new BuildingsData();
    [JsonProperty("transport")]  public TransportData    transport   = new TransportData();
    [JsonProperty("water")]      public WaterData        water       = new WaterData();
    [JsonProperty("energy_zones")] public EnergyZonesData energy_zones = new EnergyZonesData();
    [JsonProperty("vegetation")] public VegetationData   vegetation  = new VegetationData();
    [JsonProperty("environment")] public EnvironmentData environment = new EnvironmentData();
    [JsonProperty("sustainability")] public SustainabilityData sustainability = new SustainabilityData();
    [JsonProperty("generation")] public GenerationData   generation  = new GenerationData();
}

// ─── Position ─────────────────────────────────────────────────────────────────
// Schema: [x, z] two-element array of numbers

public class Position
{
    public float x;
    public float z;
}

// ─── City ─────────────────────────────────────────────────────────────────────

public class CityData
{
    [JsonProperty("id")]                 public string              id                 = "";
    [JsonProperty("name")]               public string              name               = "";
    [JsonProperty("location")]           public LocationData        location           = new LocationData();
    [JsonProperty("population")]         public PopulationData      population         = new PopulationData();
    [JsonProperty("dimensions")]         public DimensionsData      dimensions         = new DimensionsData();
    [JsonProperty("coordinate_system")]  public CoordinateSystemData coordinate_system = new CoordinateSystemData();
    [JsonProperty("planning")]           public PlanningData        planning           = new PlanningData();
}

public class LocationData
{
    [JsonProperty("country")]   public string country   = "";
    [JsonProperty("region")]    public string region    = "";
    [JsonProperty("latitude")]  public float  latitude  = 0f;
    [JsonProperty("longitude")] public float  longitude = 0f;
}

public class PopulationData
{
    [JsonProperty("target")]      public int   target      = 0;
    [JsonProperty("current")]     public int   current     = 0;
    [JsonProperty("growth_rate")] public float growth_rate = 0f;
}

public class DimensionsData
{
    [JsonProperty("width")] public float width = 0f;
    [JsonProperty("depth")] public float depth = 0f;
}

public class CoordinateSystemData
{
    [JsonProperty("units")]   public string   units  = "meters";
    [JsonProperty("origin")]  public Position origin = new Position();
    [JsonProperty("x_axis")] public string   x_axis = "east";
    [JsonProperty("z_axis")] public string   z_axis = "north";
}

public class PlanningData
{
    [JsonProperty("development_model")]    public string   development_model    = "planned";
    [JsonProperty("density_model")]        public string   density_model        = "center_weighted";
    [JsonProperty("primary_orientation")]  public float    primary_orientation  = 0f;
    [JsonProperty("urban_growth_direction")] public string urban_growth_direction = "none";
    [JsonProperty("centrality")]           public Position centrality           = new Position();
}

// ─── Terrain ──────────────────────────────────────────────────────────────────

public class TerrainData
{
    [JsonProperty("type")]        public string              type        = "flat";
    [JsonProperty("elevation")]   public ElevationData       elevation   = new ElevationData();
    [JsonProperty("slope")]       public SlopeData           slope       = new SlopeData();
    [JsonProperty("roughness")]   public RoughnessData       roughness   = new RoughnessData();
    [JsonProperty("hydrology")]   public HydrologyData       hydrology   = new HydrologyData();
    [JsonProperty("soil")]        public SoilData            soil        = new SoilData();
    [JsonProperty("buildability")] public BuildabilityData   buildability = new BuildabilityData();
    [JsonProperty("generation")]  public TerrainGenerationData generation = new TerrainGenerationData();
}

public class ElevationData
{
    [JsonProperty("minimum")]      public float  minimum      = 0f;
    [JsonProperty("maximum")]      public float  maximum      = 50f;
    [JsonProperty("sea_level")]    public float  sea_level    = 0f;
    [JsonProperty("distribution")] public string distribution = "uniform";
}

public class SlopeData
{
    [JsonProperty("minimum")]           public float minimum           = 0f;
    [JsonProperty("maximum")]           public float maximum           = 30f;
    [JsonProperty("preferred_building")] public float preferred_building = 5f;
    [JsonProperty("maximum_building")]  public float maximum_building  = 15f;
    [JsonProperty("maximum_road")]      public float maximum_road      = 10f;
}

public class RoughnessData
{
    [JsonProperty("level")]       public float level       = 0.3f;
    [JsonProperty("scale")]       public float scale       = 500f;
    [JsonProperty("octaves")]     public int   octaves     = 4;
    [JsonProperty("persistence")] public float persistence = 0.5f;
    [JsonProperty("lacunarity")]  public float lacunarity  = 2f;
}

public class HydrologyData
{
    [JsonProperty("drainage_pattern")]    public string drainage_pattern    = "none";
    [JsonProperty("water_flow_direction")] public float  water_flow_direction = 0f;
    [JsonProperty("flood_risk")]          public string flood_risk          = "low";
    [JsonProperty("flood_plain")]         public bool   flood_plain         = false;
    [JsonProperty("retention_required")]  public bool   retention_required  = false;
    [JsonProperty("natural_drainage")]    public bool   natural_drainage    = true;
}

public class SoilData
{
    [JsonProperty("type")]             public string soil_type       = "loam";
    [JsonProperty("stability")]        public string stability        = "good";
    [JsonProperty("drainage")]         public string drainage         = "good";
    [JsonProperty("bearing_capacity")] public float  bearing_capacity = 150f;
}

public class BuildabilityData
{
    [JsonProperty("minimum_buildable_ratio")] public float minimum_buildable_ratio = 0.6f;
    [JsonProperty("preferred_slope")]         public float preferred_slope         = 3f;
    [JsonProperty("maximum_slope")]           public float maximum_slope           = 15f;
    [JsonProperty("exclude_floodplains")]     public bool  exclude_floodplains     = true;
    [JsonProperty("exclude_water_bodies")]    public bool  exclude_water_bodies    = true;
    [JsonProperty("exclude_unstable_soil")]   public bool  exclude_unstable_soil   = false;
    [JsonProperty("terracing_allowed")]       public bool  terracing_allowed       = false;
}

public class TerrainGenerationData
{
    [JsonProperty("method")]       public string    method       = "noise";
    [JsonProperty("resolution")]   public int       resolution   = 256;
    [JsonProperty("height_scale")] public float     height_scale = 22f;
    [JsonProperty("seed")]         public int       seed         = 0;
    [JsonProperty("noise")]        public NoiseData noise        = new NoiseData();
}

public class NoiseData
{
    [JsonProperty("scale")]       public float scale       = 500f;
    [JsonProperty("octaves")]     public int   octaves     = 4;
    [JsonProperty("persistence")] public float persistence = 0.5f;
    [JsonProperty("lacunarity")]  public float lacunarity  = 2f;
}

// ─── Climate ──────────────────────────────────────────────────────────────────

public class ClimateData
{
    [JsonProperty("classification")]  public string              classification  = "temperate";
    [JsonProperty("temperature")]     public TemperatureData     temperature     = new TemperatureData();
    [JsonProperty("precipitation")]   public PrecipitationData   precipitation   = new PrecipitationData();
    [JsonProperty("humidity")]        public HumidityData        humidity        = new HumidityData();
    [JsonProperty("solar")]           public SolarData           solar           = new SolarData();
    [JsonProperty("wind")]            public WindData            wind            = new WindData();
    [JsonProperty("extreme_weather")] public ExtremeWeatherData  extreme_weather = new ExtremeWeatherData();
    [JsonProperty("seasons")]         public List<SeasonData>    seasons         = new List<SeasonData>();
}

public class TemperatureData
{
    [JsonProperty("annual_average")]   public float annual_average   = 15f;
    [JsonProperty("minimum")]          public float minimum          = -5f;
    [JsonProperty("maximum")]          public float maximum          = 38f;
    [JsonProperty("summer_average")]   public float summer_average   = 25f;
    [JsonProperty("winter_average")]   public float winter_average   = 5f;
    [JsonProperty("heatwave_threshold")] public float heatwave_threshold = 35f;
    [JsonProperty("coldwave_threshold")] public float coldwave_threshold = -10f;
}

public class PrecipitationData
{
    [JsonProperty("annual_mm")]         public float        annual_mm         = 800f;
    [JsonProperty("pattern")]           public string       pattern           = "seasonal";
    [JsonProperty("wet_season")]        public List<int>    wet_season        = new List<int>();
    [JsonProperty("dry_season")]        public List<int>    dry_season        = new List<int>();
    [JsonProperty("peak_monthly_mm")]   public float        peak_monthly_mm   = 120f;
    [JsonProperty("rainfall_intensity")] public string      rainfall_intensity = "moderate";
}

public class HumidityData
{
    [JsonProperty("annual_average")] public float annual_average = 60f;
    [JsonProperty("summer_average")] public float summer_average = 70f;
    [JsonProperty("winter_average")] public float winter_average = 50f;
}

public class SolarData
{
    [JsonProperty("average_irradiance")]        public float average_irradiance        = 4.5f;
    [JsonProperty("peak_sun_hours")]            public float peak_sun_hours            = 4.5f;
    [JsonProperty("annual_solar_energy")]       public float annual_solar_energy       = 1600f;
    [JsonProperty("preferred_panel_orientation")] public float preferred_panel_orientation = 180f;
    [JsonProperty("seasonal_variation")]        public float seasonal_variation        = 0.4f;
}

public class WindData
{
    [JsonProperty("average_speed")]      public float              average_speed      = 4f;
    [JsonProperty("maximum_speed")]      public float              maximum_speed      = 25f;
    [JsonProperty("dominant_direction")] public float              dominant_direction = 270f;
    [JsonProperty("seasonal_directions")] public List<SeasonalWindData> seasonal_directions = new List<SeasonalWindData>();
    [JsonProperty("energy_potential")]   public string             energy_potential   = "moderate";
}

public class SeasonalWindData
{
    [JsonProperty("season")]    public string season    = "";
    [JsonProperty("direction")] public float  direction = 0f;
    [JsonProperty("speed")]     public float  speed     = 0f;
}

public class ExtremeWeatherData
{
    [JsonProperty("heatwaves")] public string heatwaves = "low";
    [JsonProperty("flooding")]  public string flooding  = "low";
    [JsonProperty("storms")]    public string storms    = "low";
    [JsonProperty("drought")]   public string drought   = "low";
    [JsonProperty("cyclones")]  public string cyclones  = "none";
}

public class SeasonData
{
    [JsonProperty("name")]                public string    name                = "";
    [JsonProperty("months")]              public List<int> months              = new List<int>();
    [JsonProperty("average_temperature")] public float     average_temperature = 15f;
    [JsonProperty("precipitation_mm")]    public float     precipitation_mm    = 0f;
    [JsonProperty("solar_factor")]        public float     solar_factor        = 1f;
    [JsonProperty("wind_factor")]         public float     wind_factor         = 1f;
}

// ─── Land Use ─────────────────────────────────────────────────────────────────

public class LandUseData
{
    [JsonProperty("categories")]   public List<string>          categories   = new List<string>();
    [JsonProperty("targets")]      public LandUseTargetsData    targets      = new LandUseTargetsData();
    [JsonProperty("constraints")]  public LandUseConstraintsData constraints  = new LandUseConstraintsData();
    [JsonProperty("mixing")]       public LandUseMixingData     mixing       = new LandUseMixingData();
    [JsonProperty("proximity")]    public LandUseProximityData  proximity    = new LandUseProximityData();
}

public class LandUseTargetsData
{
    // Ratios are stored as a flexible dictionary since categories are user-defined
    [JsonExtensionData]
    public Dictionary<string, object> targets = new Dictionary<string, object>();
}

public class LandUseConstraintsData
{
    [JsonProperty("industrial_buffer")]    public float industrial_buffer    = 200f;
    [JsonProperty("minimum_green_ratio")]  public float minimum_green_ratio  = 0.25f;
    [JsonProperty("maximum_built_ratio")] public float maximum_built_ratio  = 0.65f;
}

public class LandUseMixingData
{
    [JsonProperty("allowed")]              public bool                       allowed              = true;
    [JsonProperty("allowed_combinations")] public List<AllowedCombinationData> allowed_combinations = new List<AllowedCombinationData>();
}

public class AllowedCombinationData
{
    [JsonProperty("primary")]   public string primary   = "";
    [JsonProperty("secondary")] public string secondary = "";
}

public class LandUseProximityData
{
    [JsonProperty("residential_to_parks")]    public DistanceData residential_to_parks    = new DistanceData();
    [JsonProperty("residential_to_services")] public DistanceData residential_to_services = new DistanceData();
    [JsonProperty("residential_to_work")]     public DistanceData residential_to_work     = new DistanceData();
}

public class DistanceData
{
    [JsonProperty("minimum")]  public float minimum  = 0f;
    [JsonProperty("preferred")] public float preferred = 400f;
    [JsonProperty("maximum")]  public float maximum  = 800f;
}

// ─── Sectors ──────────────────────────────────────────────────────────────────

public class SectorData
{
    [JsonProperty("id")]             public string                 id             = "";
    [JsonProperty("name")]           public string                 name           = "";
    [JsonProperty("type")]           public string                 type           = "residential";
    [JsonProperty("geometry")]       public SectorGeometryData     geometry       = new SectorGeometryData();
    [JsonProperty("population")]     public SectorPopulationData   population     = new SectorPopulationData();
    [JsonProperty("density")]        public SectorDensityData      density        = new SectorDensityData();
    [JsonProperty("land_use")]       public SectorLandUseData      land_use       = new SectorLandUseData();
    [JsonProperty("building_rules")] public BuildingRulesData      building_rules = new BuildingRulesData();
    [JsonProperty("street_rules")]   public StreetRulesData        street_rules   = new StreetRulesData();
    [JsonProperty("green_rules")]    public GreenRulesData         green_rules    = new GreenRulesData();
    [JsonProperty("services")]       public List<ServiceData>      services       = new List<ServiceData>();
    [JsonProperty("energy")]         public SectorEnergyData       energy         = new SectorEnergyData();
    [JsonProperty("constraints")]    public SectorConstraintsData  constraints    = new SectorConstraintsData();
}

public class SectorGeometryData
{
    [JsonProperty("type")]    public string         type    = "rectangle";
    [JsonProperty("bounds")]  public float[]        bounds  = new float[4]; // [x, z, width, depth]
    [JsonProperty("polygon")] public List<Position> polygon = new List<Position>();
    [JsonProperty("center")]  public Position       center  = new Position();
    [JsonProperty("radius")]  public float          radius  = 0f;
}

public class SectorPopulationData
{
    [JsonProperty("target")]             public int   target             = 0;
    [JsonProperty("current")]            public int   current            = 0;
    [JsonProperty("daytime_population")] public int   daytime_population = 0;
    [JsonProperty("growth_rate")]        public float growth_rate        = 0f;
}

public class SectorDensityData
{
    [JsonProperty("population")]            public float  population            = 5000f;
    [JsonProperty("floor_area")]            public float  floor_area            = 1.5f;
    [JsonProperty("building_coverage")]     public float  building_coverage     = 0.5f;
    [JsonProperty("development_intensity")] public string development_intensity = "medium";
}

public class SectorLandUseData
{
    [JsonProperty("primary")]          public string       primary          = "";
    [JsonProperty("secondary")]        public List<string> secondary        = new List<string>();
    [JsonProperty("primary_ratio")]    public float        primary_ratio    = 0.7f;
    [JsonProperty("secondary_ratio")]  public float        secondary_ratio  = 0.3f;
}

public class BuildingRulesData
{
    [JsonProperty("allowed_archetypes")] public List<string>            allowed_archetypes = new List<string>();
    [JsonProperty("height")]             public BuildingHeightRulesData  height             = new BuildingHeightRulesData();
    [JsonProperty("footprint")]          public BuildingFootprintRulesData footprint        = new BuildingFootprintRulesData();
    [JsonProperty("spacing")]            public BuildingSpacingRulesData  spacing           = new BuildingSpacingRulesData();
    [JsonProperty("coverage")]           public BuildingCoverageRulesData coverage          = new BuildingCoverageRulesData();
    [JsonProperty("orientation")]        public BuildingOrientationRulesData orientation    = new BuildingOrientationRulesData();
}

public class BuildingHeightRulesData
{
    [JsonProperty("minimum_floors")] public int minimum_floors = 1;
    [JsonProperty("maximum_floors")] public int maximum_floors = 10;
    [JsonProperty("preferred_floors")] public int preferred_floors = 3;
}

public class BuildingFootprintRulesData
{
    [JsonProperty("minimum_width")] public float minimum_width = 6f;
    [JsonProperty("maximum_width")] public float maximum_width = 40f;
    [JsonProperty("minimum_depth")] public float minimum_depth = 6f;
    [JsonProperty("maximum_depth")] public float maximum_depth = 40f;
}

public class BuildingSpacingRulesData
{
    [JsonProperty("building_gap")]  public float building_gap  = 4f;
    [JsonProperty("front_setback")] public float front_setback = 3f;
    [JsonProperty("side_setback")]  public float side_setback  = 2f;
    [JsonProperty("rear_setback")]  public float rear_setback  = 3f;
}

public class BuildingCoverageRulesData
{
    [JsonProperty("maximum")]  public float maximum  = 0.6f;
    [JsonProperty("preferred")] public float preferred = 0.45f;
}

public class BuildingOrientationRulesData
{
    [JsonProperty("mode")]      public string mode      = "road_aligned";
    [JsonProperty("angle")]     public float  angle     = 0f;
    [JsonProperty("variation")] public float  variation = 0f;
}

public class StreetRulesData
{
    [JsonProperty("pattern")]      public string            pattern      = "grid";
    [JsonProperty("orientation")]  public float             orientation  = 0f;
    [JsonProperty("blocks")]       public BlockRulesData    blocks       = new BlockRulesData();
    [JsonProperty("local_roads")]  public LocalRoadRulesData local_roads = new LocalRoadRulesData();
    [JsonProperty("sidewalk")]     public SidewalkRulesData sidewalk     = new SidewalkRulesData();
    [JsonProperty("cycling")]      public CyclingRulesData  cycling      = new CyclingRulesData();
    [JsonProperty("street_trees")] public StreetTreeRulesData street_trees = new StreetTreeRulesData();
}

public class BlockRulesData
{
    [JsonProperty("minimum_width")] public float minimum_width = 60f;
    [JsonProperty("maximum_width")] public float maximum_width = 150f;
    [JsonProperty("minimum_depth")] public float minimum_depth = 40f;
    [JsonProperty("maximum_depth")] public float maximum_depth = 100f;
}

public class LocalRoadRulesData
{
    [JsonProperty("spacing")] public float spacing = 100f;
    [JsonProperty("width")]   public float width   = 8f;
    [JsonProperty("lanes")]   public int   lanes   = 2;
}

public class SidewalkRulesData
{
    [JsonProperty("enabled")] public bool  enabled = true;
    [JsonProperty("width")]   public float width   = 2f;
}

public class CyclingRulesData
{
    [JsonProperty("enabled")] public bool  enabled = false;
    [JsonProperty("width")]   public float width   = 1.5f;
}

public class StreetTreeRulesData
{
    [JsonProperty("enabled")] public bool  enabled = false;
    [JsonProperty("spacing")] public float spacing = 15f;
}

public class GreenRulesData
{
    [JsonProperty("minimum_ratio")]          public float minimum_ratio          = 0.1f;
    [JsonProperty("tree_canopy_target")]     public float tree_canopy_target     = 0.2f;
    [JsonProperty("tree_density")]           public float tree_density           = 50f;
    [JsonProperty("courtyards")]             public bool  courtyards             = false;
    [JsonProperty("green_roofs")]            public bool  green_roofs            = false;
    [JsonProperty("green_walls")]            public bool  green_walls            = false;
    [JsonProperty("permeable_surface_target")] public float permeable_surface_target = 0.3f;
}

public class ServiceData
{
    [JsonProperty("type")]           public string type           = "";
    [JsonProperty("required_count")] public int    required_count = 1;
    [JsonProperty("service_radius")] public float  service_radius = 500f;
}

public class SectorEnergyData
{
    [JsonProperty("rooftop_solar")]         public bool  rooftop_solar         = false;
    [JsonProperty("solar_coverage_target")] public float solar_coverage_target = 0f;
    [JsonProperty("district_energy")]       public bool  district_energy       = false;
    [JsonProperty("battery_storage")]       public bool  battery_storage       = false;
    [JsonProperty("ev_charging")]           public bool  ev_charging           = false;
}

public class SectorConstraintsData
{
    [JsonProperty("maximum_slope")]        public float  maximum_slope        = 15f;
    [JsonProperty("avoid_floodplain")]     public bool   avoid_floodplain     = true;
    [JsonProperty("avoid_water")]          public bool   avoid_water          = true;
    [JsonProperty("protected")]            public bool   protected_area       = false;
    [JsonProperty("development_priority")] public string development_priority = "normal";
}

// ─── Road Graph ───────────────────────────────────────────────────────────────

public class RoadGraphData
{
    [JsonProperty("nodes")]        public List<RoadNodeData>  nodes        = new List<RoadNodeData>();
    [JsonProperty("edges")]        public List<RoadEdgeData>  edges        = new List<RoadEdgeData>();
    [JsonProperty("hierarchy")]    public RoadHierarchyData   hierarchy    = new RoadHierarchyData();
    [JsonProperty("connectivity")] public RoadConnectivityData connectivity = new RoadConnectivityData();
    [JsonProperty("generation")]   public RoadGenerationData  generation   = new RoadGenerationData();
}

public class RoadNodeData
{
    [JsonProperty("id")]       public string   id       = "";
    [JsonProperty("position")] public Position position = new Position();
    [JsonProperty("type")]     public string   type     = "intersection";
    [JsonProperty("capacity")] public int      capacity = 4;
}

public class RoadEdgeData
{
    [JsonProperty("id")]               public string            id               = "";
    [JsonProperty("from")]             public string            from             = "";
    [JsonProperty("to")]               public string            to               = "";
    [JsonProperty("type")]             public string            type             = "local";
    [JsonProperty("geometry")]         public RoadGeometryData  geometry         = new RoadGeometryData();
    [JsonProperty("width")]            public float             width            = 8f;
    [JsonProperty("lanes")]            public int               lanes            = 2;
    [JsonProperty("speed_limit")]      public float             speed_limit      = 50f;
    [JsonProperty("one_way")]          public bool              one_way          = false;
    [JsonProperty("sidewalk")]         public RoadSidewalkData  sidewalk         = new RoadSidewalkData();
    [JsonProperty("cycling")]          public RoadCyclingData   cycling          = new RoadCyclingData();
    [JsonProperty("street_trees")]     public RoadStreetTreeData street_trees    = new RoadStreetTreeData();
    [JsonProperty("street_lighting")]  public bool              street_lighting  = true;
    [JsonProperty("public_transport")] public bool              public_transport = false;
}

public class RoadGeometryData
{
    [JsonProperty("routing")]    public string         routing    = "straight";
    [JsonProperty("waypoints")]  public List<Position> waypoints  = new List<Position>();
}

public class RoadSidewalkData
{
    [JsonProperty("enabled")] public bool  enabled = true;
    [JsonProperty("width")]   public float width   = 2f;
}

public class RoadCyclingData
{
    [JsonProperty("enabled")]   public bool  enabled   = false;
    [JsonProperty("width")]     public float width     = 1.5f;
    [JsonProperty("separated")] public bool  separated = false;
}

public class RoadStreetTreeData
{
    [JsonProperty("enabled")] public bool  enabled = false;
    [JsonProperty("spacing")] public float spacing = 15f;
}

public class RoadHierarchyData
{
    [JsonProperty("arterial")]   public RoadClassData arterial   = new RoadClassData();
    [JsonProperty("collector")]  public RoadClassData collector  = new RoadClassData();
    [JsonProperty("local")]      public RoadClassData local      = new RoadClassData();
    [JsonProperty("pedestrian")] public RoadClassData pedestrian = new RoadClassData();
    [JsonProperty("cycle")]      public RoadClassData cycle      = new RoadClassData();
}

public class RoadClassData
{
    [JsonProperty("default_width")]   public float  default_width   = 8f;
    [JsonProperty("default_lanes")]   public int    default_lanes   = 2;
    [JsonProperty("default_speed")]   public float  default_speed   = 50f;
    [JsonProperty("sidewalk")]        public bool   sidewalk        = true;
    [JsonProperty("cycling")]         public bool   cycling         = false;
    [JsonProperty("street_lighting")] public bool   street_lighting = true;
}

public class RoadConnectivityData
{
    [JsonProperty("minimum_node_degree")]         public int   minimum_node_degree         = 2;
    [JsonProperty("maximum_dead_end_ratio")]       public float maximum_dead_end_ratio       = 0.1f;
    [JsonProperty("maximum_intersection_spacing")] public float maximum_intersection_spacing = 300f;
    [JsonProperty("target_network_density")]      public float target_network_density      = 0f;
    [JsonProperty("require_sector_connections")]  public bool  require_sector_connections  = true;
    [JsonProperty("require_transit_connections")] public bool  require_transit_connections = false;
}

public class RoadGenerationData
{
    [JsonProperty("primary_pattern")]   public string primary_pattern   = "grid";
    [JsonProperty("orientation")]       public float  orientation       = 0f;
    [JsonProperty("arterial_spacing")]  public float  arterial_spacing  = 600f;
    [JsonProperty("collector_spacing")] public float  collector_spacing = 200f;
    [JsonProperty("follow_terrain")]    public bool   follow_terrain    = false;
    [JsonProperty("avoid_steep_slopes")] public bool  avoid_steep_slopes = true;
    [JsonProperty("maximum_slope")]     public float  maximum_slope     = 10f;
}

// ─── Blocks ───────────────────────────────────────────────────────────────────

public class BlocksData
{
    [JsonProperty("generation")]   public BlockGenerationData  generation   = new BlockGenerationData();
    [JsonProperty("size")]         public BlockSizeData        size         = new BlockSizeData();
    [JsonProperty("shape")]        public BlockShapeData       shape        = new BlockShapeData();
    [JsonProperty("subdivision")]  public BlockSubdivisionData subdivision  = new BlockSubdivisionData();
    [JsonProperty("development")]  public BlockDevelopmentData development  = new BlockDevelopmentData();
}

public class BlockGenerationData
{
    [JsonProperty("method")]                    public string method                    = "road_bounded";
    [JsonProperty("respect_sector_boundaries")] public bool   respect_boundaries = true;
    [JsonProperty("respect_road_hierarchy")]    public bool   respect_road_hierarchy    = true;
    [JsonProperty("arterial_block_boundary")]   public bool   arterial_block_boundary   = true;
    [JsonProperty("collector_block_boundary")]  public bool   collector_block_boundary  = true;
    [JsonProperty("allow_irregular_blocks")]    public bool   allow_irregular_blocks    = false;
}

public class BlockSizeData
{
    [JsonProperty("minimum_area")]          public float minimum_area          = 2000f;
    [JsonProperty("maximum_area")]          public float maximum_area          = 20000f;
    [JsonProperty("minimum_width")]         public float minimum_width         = 40f;
    [JsonProperty("maximum_width")]         public float maximum_width         = 200f;
    [JsonProperty("minimum_depth")]         public float minimum_depth         = 30f;
    [JsonProperty("maximum_depth")]         public float maximum_depth         = 150f;
    [JsonProperty("preferred_area")]        public float preferred_area        = 8000f;
    [JsonProperty("preferred_aspect_ratio")] public float preferred_aspect_ratio = 1.5f;
    [JsonProperty("maximum_aspect_ratio")]  public float maximum_aspect_ratio  = 4f;
}

public class BlockShapeData
{
    [JsonProperty("preferred")]        public string preferred        = "rectangular";
    [JsonProperty("regularity")]       public float  regularity       = 0.8f;
    [JsonProperty("corner_treatment")] public string corner_treatment = "sharp";
    [JsonProperty("minimum_angle")]    public float  minimum_angle    = 60f;
    [JsonProperty("maximum_angle")]    public float  maximum_angle    = 120f;
}

public class BlockSubdivisionData
{
    [JsonProperty("enabled")]             public bool   enabled             = true;
    [JsonProperty("method")]              public string method              = "frontage_based";
    [JsonProperty("minimum_plot_area")]   public float  minimum_plot_area   = 150f;
    [JsonProperty("maximum_plot_area")]   public float  maximum_plot_area   = 2000f;
    [JsonProperty("minimum_frontage")]    public float  minimum_frontage    = 8f;
    [JsonProperty("maximum_frontage")]    public float  maximum_frontage    = 50f;
    [JsonProperty("minimum_depth")]       public float  minimum_depth       = 15f;
    [JsonProperty("maximum_depth")]       public float  maximum_depth       = 60f;
    [JsonProperty("internal_access")]     public bool   internal_access     = false;
    [JsonProperty("internal_road_width")] public float  internal_road_width = 0f;
}

public class BlockDevelopmentData
{
    [JsonProperty("maximum_building_coverage")]  public float      maximum_building_coverage  = 0.6f;
    [JsonProperty("preferred_building_coverage")] public float     preferred_building_coverage = 0.45f;
    [JsonProperty("minimum_open_space")]         public float      minimum_open_space         = 0.2f;
    [JsonProperty("minimum_green_space")]        public float      minimum_green_space        = 0.1f;
    [JsonProperty("courtyard_ratio")]            public float      courtyard_ratio            = 0f;
    [JsonProperty("frontage_activation")]        public string     frontage_activation        = "medium";
    [JsonProperty("parking")]                    public ParkingData parking                   = new ParkingData();
}

public class ParkingData
{
    [JsonProperty("allowed")]                 public bool  allowed                 = true;
    [JsonProperty("maximum_surface_ratio")]   public float maximum_surface_ratio   = 0.2f;
    [JsonProperty("underground_preferred")]   public bool  underground_preferred   = false;
}

// ─── Buildings ────────────────────────────────────────────────────────────────

public class BuildingsData
{
    [JsonProperty("archetypes")]     public List<BuildingArchetypeData> archetypes     = new List<BuildingArchetypeData>();
    [JsonProperty("generation")]     public BuildingGenerationData      generation     = new BuildingGenerationData();
    [JsonProperty("orientation")]    public BuildingOrientationData     orientation    = new BuildingOrientationData();
    [JsonProperty("spacing")]        public BuildingSpacingData         spacing        = new BuildingSpacingData();
    [JsonProperty("sustainability")] public BuildingSustainabilityData  sustainability = new BuildingSustainabilityData();
}

public class BuildingArchetypeData
{
    [JsonProperty("id")]        public string              id        = "";
    [JsonProperty("type")]      public string              type      = "apartment";
    [JsonProperty("height")]    public BuildingHeightData  height    = new BuildingHeightData();
    [JsonProperty("footprint")] public BuildingFootprintData footprint = new BuildingFootprintData();
    [JsonProperty("density")]   public string              density   = "medium";
    [JsonProperty("mixed_use")] public bool                mixed_use = false;
}

public class BuildingHeightData
{
    [JsonProperty("minimum_floors")] public int   minimum_floors = 1;
    [JsonProperty("maximum_floors")] public int   maximum_floors = 5;
    [JsonProperty("floor_height")]   public float floor_height   = 3f;
}

public class BuildingFootprintData
{
    [JsonProperty("minimum_width")] public float minimum_width = 6f;
    [JsonProperty("maximum_width")] public float maximum_width = 25f;
    [JsonProperty("minimum_depth")] public float minimum_depth = 6f;
    [JsonProperty("maximum_depth")] public float maximum_depth = 20f;
}

public class BuildingGenerationData
{
    [JsonProperty("distribution")]             public string distribution             = "density_weighted";
    [JsonProperty("placement")]                public string placement                = "plot_based";
    [JsonProperty("seed")]                     public int    seed                     = 0;
    [JsonProperty("maximum_buildings_per_block")] public int maximum_buildings_per_block = 20;
}

public class BuildingOrientationData
{
    [JsonProperty("mode")]            public string mode            = "road_aligned";
    [JsonProperty("preferred_angle")] public float  preferred_angle = 0f;
    [JsonProperty("variation")]       public float  variation       = 0f;
}

public class BuildingSpacingData
{
    [JsonProperty("minimum_building_gap")] public float minimum_building_gap = 3f;
    [JsonProperty("maximum_building_gap")] public float maximum_building_gap = 20f;
    [JsonProperty("minimum_front_setback")] public float minimum_front_setback = 3f;
    [JsonProperty("minimum_side_setback")]  public float minimum_side_setback  = 2f;
    [JsonProperty("minimum_rear_setback")]  public float minimum_rear_setback  = 3f;
}

public class BuildingSustainabilityData
{
    [JsonProperty("rooftop_solar_allowed")]  public bool   rooftop_solar_allowed  = true;
    [JsonProperty("rooftop_solar_required")] public bool   rooftop_solar_required = false;
    [JsonProperty("solar_coverage_target")]  public float  solar_coverage_target  = 0.3f;
    [JsonProperty("green_roof_target")]      public float  green_roof_target      = 0.1f;
    [JsonProperty("green_wall_target")]      public float  green_wall_target      = 0f;
    [JsonProperty("rainwater_collection")]   public bool   rainwater_collection   = false;
    [JsonProperty("passive_cooling")]        public bool   passive_cooling        = false;
    [JsonProperty("natural_ventilation")]    public bool   natural_ventilation    = false;
    [JsonProperty("energy_efficiency")]      public string energy_efficiency      = "efficient";
}

// ─── Transport ────────────────────────────────────────────────────────────────

public class TransportData
{
    [JsonProperty("mode_share")]       public ModeShareData            mode_share       = new ModeShareData();
    [JsonProperty("public_transport")] public PublicTransportData      public_transport = new PublicTransportData();
    [JsonProperty("walking")]          public WalkingData              walking          = new WalkingData();
    [JsonProperty("cycling")]          public CyclingData              cycling          = new CyclingData();
    [JsonProperty("roads")]            public TransportRoadsData       roads            = new TransportRoadsData();
    [JsonProperty("parking")]          public TransportParkingData     parking          = new TransportParkingData();
    [JsonProperty("accessibility")]    public TransportAccessibilityData accessibility  = new TransportAccessibilityData();
}

public class ModeShareData
{
    [JsonProperty("walking")]          public float walking          = 0.25f;
    [JsonProperty("cycling")]          public float cycling          = 0.15f;
    [JsonProperty("public_transport")] public float public_transport = 0.35f;
    [JsonProperty("private_vehicle")]  public float private_vehicle  = 0.20f;
    [JsonProperty("other")]            public float other            = 0.05f;
}

public class PublicTransportData
{
    [JsonProperty("enabled")]  public bool                       enabled  = true;
    [JsonProperty("modes")]    public List<string>               modes    = new List<string>();
    [JsonProperty("coverage")] public PublicTransportCoverageData coverage = new PublicTransportCoverageData();
    [JsonProperty("stops")]    public PublicTransportStopsData   stops    = new PublicTransportStopsData();
    [JsonProperty("priority")] public string                     priority = "high";
}

public class PublicTransportCoverageData
{
    [JsonProperty("maximum_walk_distance")]      public float maximum_walk_distance      = 400f;
    [JsonProperty("minimum_population_coverage")] public float minimum_population_coverage = 0.9f;
}

public class PublicTransportStopsData
{
    [JsonProperty("minimum_spacing")] public float minimum_spacing = 200f;
    [JsonProperty("maximum_spacing")] public float maximum_spacing = 600f;
}

public class WalkingData
{
    [JsonProperty("enabled")]                         public bool  enabled                         = true;
    [JsonProperty("minimum_sidewalk_width")]           public float minimum_sidewalk_width           = 1.8f;
    [JsonProperty("maximum_distance_to_daily_services")] public float maximum_distance_to_daily_services = 500f;
    [JsonProperty("pedestrian_priority_zones")]        public bool  pedestrian_priority_zones        = true;
    [JsonProperty("traffic_calming")]                 public bool  traffic_calming                 = true;
    [JsonProperty("shade_required")]                  public bool  shade_required                  = false;
    [JsonProperty("universal_access")]                public bool  universal_access                = true;
}

public class CyclingData
{
    [JsonProperty("enabled")]                          public bool  enabled                          = true;
    [JsonProperty("minimum_lane_width")]               public float minimum_lane_width               = 1.5f;
    [JsonProperty("separated_from_traffic")]           public bool  separated_from_traffic           = true;
    [JsonProperty("maximum_distance_to_cycle_network")] public float maximum_distance_to_cycle_network = 300f;
    [JsonProperty("bike_parking")]                     public bool  bike_parking                     = true;
}

public class TransportRoadsData
{
    [JsonProperty("private_vehicle_priority")] public string private_vehicle_priority = "low";
    [JsonProperty("traffic_calming")]          public bool   traffic_calming          = true;
    [JsonProperty("low_emission_zones")]       public bool   low_emission_zones       = true;
    [JsonProperty("freight_routes")]           public bool   freight_routes           = false;
    [JsonProperty("emergency_route_access")]   public bool   emergency_route_access   = true;
}

public class TransportParkingData
{
    [JsonProperty("strategy")]                      public string strategy                      = "reduced";
    [JsonProperty("maximum_surface_parking_ratio")] public float  maximum_surface_parking_ratio = 0.1f;
    [JsonProperty("underground_parking_preferred")] public bool   underground_parking_preferred = true;
    [JsonProperty("park_and_ride")]                 public bool   park_and_ride                 = false;
    [JsonProperty("ev_charging")]                   public bool   ev_charging                   = true;
}

public class TransportAccessibilityData
{
    [JsonProperty("universal_design")]    public bool  universal_design    = true;
    [JsonProperty("maximum_gradient")]    public float maximum_gradient    = 8f;
    [JsonProperty("accessible_transit")]  public bool  accessible_transit  = true;
    [JsonProperty("accessible_crossings")] public bool accessible_crossings = true;
}

// ─── Water ────────────────────────────────────────────────────────────────────

public class WaterData
{
    [JsonProperty("supply")]       public WaterSupplyData       supply       = new WaterSupplyData();
    [JsonProperty("distribution")] public WaterDistributionData distribution = new WaterDistributionData();
    [JsonProperty("consumption")]  public WaterConsumptionData  consumption  = new WaterConsumptionData();
    [JsonProperty("stormwater")]   public StormwaterData        stormwater   = new StormwaterData();
    [JsonProperty("wastewater")]   public WastewaterData        wastewater   = new WastewaterData();
    [JsonProperty("conservation")] public WaterConservationData conservation = new WaterConservationData();
}

public class WaterSupplyData
{
    [JsonProperty("sources")]               public List<string> sources               = new List<string>();
    [JsonProperty("daily_capacity")]        public float        daily_capacity        = 0f;
    [JsonProperty("reliability")]           public string       reliability           = "high";
    [JsonProperty("groundwater_recharge")]  public bool         groundwater_recharge  = false;
}

public class WaterDistributionData
{
    [JsonProperty("potable_network")]    public bool  potable_network    = true;
    [JsonProperty("non_potable_network")] public bool non_potable_network = false;
    [JsonProperty("district_metering")]  public bool  district_metering  = true;
    [JsonProperty("leakage_target")]     public float leakage_target     = 0.1f;
    [JsonProperty("storage_required")]   public bool  storage_required   = true;
}

public class WaterConsumptionData
{
    [JsonProperty("target_per_capita_daily")]  public float target_per_capita_daily  = 150f;
    [JsonProperty("maximum_per_capita_daily")] public float maximum_per_capita_daily = 200f;
    [JsonProperty("industrial_reuse_required")] public bool industrial_reuse_required = false;
    [JsonProperty("greywater_reuse")]          public bool  greywater_reuse          = false;
}

public class StormwaterData
{
    [JsonProperty("strategy")]                 public string strategy                 = "sustainable_drainage";
    [JsonProperty("retention_required")]       public bool   retention_required       = true;
    [JsonProperty("infiltration_required")]    public bool   infiltration_required    = false;
    [JsonProperty("permeable_surface_target")] public float  permeable_surface_target = 0.3f;
    [JsonProperty("rain_gardens")]             public bool   rain_gardens             = true;
    [JsonProperty("bioswales")]                public bool   bioswales                = true;
    [JsonProperty("retention_ponds")]          public bool   retention_ponds          = false;
    [JsonProperty("flood_protection_level")]   public string flood_protection_level   = "moderate";
}

public class WastewaterData
{
    [JsonProperty("treatment")]              public string       treatment              = "centralized";
    [JsonProperty("reuse_target")]           public float        reuse_target           = 0.5f;
    [JsonProperty("treated_water_uses")]     public List<string> treated_water_uses     = new List<string>();
    [JsonProperty("sewage_treatment_required")] public bool      sewage_treatment_required = true;
}

public class WaterConservationData
{
    [JsonProperty("rainwater_harvesting")]        public bool  rainwater_harvesting        = true;
    [JsonProperty("water_efficient_landscaping")] public bool  water_efficient_landscaping = true;
    [JsonProperty("native_vegetation_preferred")] public bool  native_vegetation_preferred = true;
    [JsonProperty("irrigation_efficiency_target")] public float irrigation_efficiency_target = 0.8f;
    [JsonProperty("water_reuse_target")]          public float water_reuse_target          = 0.5f;
}

// ─── Energy Zones ─────────────────────────────────────────────────────────────

public class EnergyZonesData
{
    [JsonProperty("generation")]  public EnergyGenerationData  generation  = new EnergyGenerationData();
    [JsonProperty("storage")]     public EnergyStorageData     storage     = new EnergyStorageData();
    [JsonProperty("distribution")] public EnergyDistributionData distribution = new EnergyDistributionData();
    [JsonProperty("zoning")]      public EnergyZoningData      zoning      = new EnergyZoningData();
}

public class EnergyGenerationData
{
    [JsonProperty("sources")]          public List<string>   sources          = new List<string>();
    [JsonProperty("renewable_target")] public float          renewable_target = 0.8f;
    [JsonProperty("solar")]            public EnergySolarData solar            = new EnergySolarData();
    [JsonProperty("wind")]             public EnergyWindData  wind             = new EnergyWindData();
}

public class EnergySolarData
{
    [JsonProperty("preferred")]               public bool  preferred               = true;
    [JsonProperty("minimum_capacity_mw")]     public float minimum_capacity_mw     = 0f;
    [JsonProperty("maximum_capacity_mw")]     public float maximum_capacity_mw     = 0f;
    [JsonProperty("rooftop_generation")]      public bool  rooftop_generation      = true;
    [JsonProperty("ground_mounted_generation")] public bool ground_mounted_generation = true;
}

public class EnergyWindData
{
    [JsonProperty("preferred")]           public bool  preferred           = false;
    [JsonProperty("minimum_capacity_mw")] public float minimum_capacity_mw = 0f;
    [JsonProperty("maximum_capacity_mw")] public float maximum_capacity_mw = 0f;
}

public class EnergyStorageData
{
    [JsonProperty("enabled")]               public bool         enabled               = true;
    [JsonProperty("technologies")]          public List<string> technologies          = new List<string>();
    [JsonProperty("minimum_capacity_mwh")]  public float        minimum_capacity_mwh  = 0f;
    [JsonProperty("maximum_capacity_mwh")]  public float        maximum_capacity_mwh  = 0f;
    [JsonProperty("distributed_storage")]   public bool         distributed_storage   = false;
}

public class EnergyDistributionData
{
    [JsonProperty("grid_connected")]           public bool  grid_connected           = true;
    [JsonProperty("microgrids")]               public bool  microgrids               = false;
    [JsonProperty("smart_grid")]               public bool  smart_grid               = true;
    [JsonProperty("district_energy")]          public bool  district_energy          = false;
    [JsonProperty("maximum_transmission_loss")] public float maximum_transmission_loss = 0.05f;
    [JsonProperty("backup_generation")]        public bool  backup_generation        = true;
}

public class EnergyZoningData
{
    [JsonProperty("renewable_zones")]       public List<RenewableZoneData> renewable_zones       = new List<RenewableZoneData>();
    [JsonProperty("utility_zones")]         public List<string>            utility_zones         = new List<string>();
    [JsonProperty("avoid_residential")]     public bool                    avoid_residential     = true;
    [JsonProperty("avoid_water")]           public bool                    avoid_water           = true;
    [JsonProperty("avoid_protected_land")]  public bool                    avoid_protected_land  = true;
}

public class RenewableZoneData
{
    [JsonProperty("type")]         public string type         = "solar_farm";
    [JsonProperty("priority")]     public string priority     = "high";
    [JsonProperty("minimum_area")] public float  minimum_area = 0f;
}

// ─── Vegetation ───────────────────────────────────────────────────────────────

public class VegetationData
{
    [JsonProperty("green_space")]  public GreenSpaceData          green_space  = new GreenSpaceData();
    [JsonProperty("trees")]        public TreeData                trees        = new TreeData();
    [JsonProperty("planting")]     public PlantingData            planting     = new PlantingData();
    [JsonProperty("biodiversity")] public BiodiversityData        biodiversity = new BiodiversityData();
    [JsonProperty("generation")]   public VegetationGenerationData generation  = new VegetationGenerationData();
}

public class GreenSpaceData
{
    [JsonProperty("minimum_ratio")]               public float        minimum_ratio               = 0.25f;
    [JsonProperty("target_ratio")]                public float        target_ratio                = 0.35f;
    [JsonProperty("types")]                       public List<string> types                       = new List<string>();
    [JsonProperty("connectivity_required")]       public bool         connectivity_required       = true;
    [JsonProperty("maximum_distance_to_green_space")] public float    maximum_distance_to_green_space = 400f;
}

public class TreeData
{
    [JsonProperty("canopy_target")]          public float canopy_target          = 0.25f;
    [JsonProperty("minimum_tree_density")]   public float minimum_tree_density   = 50f;
    [JsonProperty("street_tree_spacing")]    public float street_tree_spacing    = 15f;
    [JsonProperty("native_species_preferred")] public bool native_species_preferred = true;
    [JsonProperty("shade_priority")]         public bool  shade_priority         = true;
    [JsonProperty("mature_canopy_required")] public bool  mature_canopy_required = false;
}

public class PlantingData
{
    [JsonProperty("strategy")]                 public string strategy                 = "mixed";
    [JsonProperty("native_species_only")]      public bool   native_species_only      = false;
    [JsonProperty("drought_tolerant_preferred")] public bool drought_tolerant_preferred = false;
    [JsonProperty("water_efficient")]          public bool   water_efficient          = true;
    [JsonProperty("green_roofs")]              public bool   green_roofs              = false;
    [JsonProperty("green_walls")]              public bool   green_walls              = false;
    [JsonProperty("rain_gardens")]             public bool   rain_gardens             = true;
}

public class BiodiversityData
{
    [JsonProperty("priority")]                 public string priority                 = "medium";
    [JsonProperty("wildlife_corridors")]       public bool   wildlife_corridors       = true;
    [JsonProperty("habitat_zones")]            public bool   habitat_zones            = false;
    [JsonProperty("protected_natural_areas")]  public bool   protected_natural_areas  = false;
    [JsonProperty("minimum_habitat_ratio")]    public float  minimum_habitat_ratio    = 0.05f;
    [JsonProperty("native_species_target")]    public float  native_species_target    = 0.6f;
}

public class VegetationGenerationData
{
    [JsonProperty("distribution")]        public string distribution        = "noise";
    [JsonProperty("density")]             public float  density             = 50f;
    [JsonProperty("seed")]                public int    seed                = 0;
    [JsonProperty("terrain_adaptation")]  public bool   terrain_adaptation  = true;
    [JsonProperty("water_adaptation")]    public bool   water_adaptation    = true;
    [JsonProperty("avoid_buildings")]     public bool   avoid_buildings     = true;
    [JsonProperty("avoid_roads")]         public bool   avoid_roads         = true;
}

// ─── Environment ──────────────────────────────────────────────────────────────

public class EnvironmentData
{
    [JsonProperty("air_quality")]        public AirQualityData        air_quality        = new AirQualityData();
    [JsonProperty("noise")]              public NoiseEnvironmentData  noise              = new NoiseEnvironmentData();
    [JsonProperty("climate_resilience")] public ClimateResilienceData climate_resilience = new ClimateResilienceData();
    [JsonProperty("pollution")]          public PollutionData         pollution          = new PollutionData();
    [JsonProperty("natural_resources")]  public NaturalResourcesData  natural_resources  = new NaturalResourcesData();
}

public class AirQualityData
{
    [JsonProperty("target")]               public string target               = "good";
    [JsonProperty("pollution_control")]    public bool   pollution_control    = true;
    [JsonProperty("green_buffers")]        public bool   green_buffers        = true;
    [JsonProperty("industrial_separation")] public bool  industrial_separation = true;
}

public class NoiseEnvironmentData
{
    [JsonProperty("control_required")]    public bool  control_required    = true;
    [JsonProperty("maximum_daytime_db")]  public float maximum_daytime_db  = 55f;
    [JsonProperty("maximum_nighttime_db")] public float maximum_nighttime_db = 45f;
    [JsonProperty("quiet_zones")]         public bool  quiet_zones         = true;
    [JsonProperty("noise_buffers")]       public bool  noise_buffers       = true;
}

public class ClimateResilienceData
{
    [JsonProperty("priority")]                          public string priority                          = "high";
    [JsonProperty("heat_resilience")]                   public bool   heat_resilience                   = true;
    [JsonProperty("flood_resilience")]                  public bool   flood_resilience                  = true;
    [JsonProperty("drought_resilience")]                public bool   drought_resilience                = false;
    [JsonProperty("storm_resilience")]                  public bool   storm_resilience                  = true;
    [JsonProperty("sea_level_rise_resilience")]         public bool   sea_level_rise_resilience         = false;
    [JsonProperty("maximum_urban_heat_island_intensity")] public float maximum_urban_heat_island_intensity = 3f;
}

public class PollutionData
{
    [JsonProperty("air")]                       public bool air                       = true;
    [JsonProperty("water")]                     public bool water                     = true;
    [JsonProperty("soil")]                      public bool soil                      = true;
    [JsonProperty("light")]                     public bool light                     = false;
    [JsonProperty("industrial_emission_controls")] public bool industrial_emission_controls = true;
    [JsonProperty("waste_management")]          public bool waste_management          = true;
}

public class NaturalResourcesData
{
    [JsonProperty("protected_areas")]       public bool   protected_areas       = true;
    [JsonProperty("resource_efficiency")]   public string resource_efficiency   = "efficient";
    [JsonProperty("soil_protection")]       public bool   soil_protection       = true;
    [JsonProperty("groundwater_protection")] public bool  groundwater_protection = true;
    [JsonProperty("wetland_protection")]    public bool   wetland_protection    = true;
    [JsonProperty("deforestation_avoidance")] public bool deforestation_avoidance = true;
}

// ─── Sustainability ───────────────────────────────────────────────────────────

public class SustainabilityData
{
    [JsonProperty("energy")]             public SustainabilityEnergyData    energy             = new SustainabilityEnergyData();
    [JsonProperty("transport")]          public SustainabilityTransportData transport          = new SustainabilityTransportData();
    [JsonProperty("water")]              public SustainabilityWaterData     water              = new SustainabilityWaterData();
    [JsonProperty("land")]               public SustainabilityLandData      land               = new SustainabilityLandData();
    [JsonProperty("environment")]        public SustainabilityEnvironmentData environment      = new SustainabilityEnvironmentData();
    [JsonProperty("resource_efficiency")] public ResourceEfficiencyData     resource_efficiency = new ResourceEfficiencyData();
}

public class SustainabilityEnergyData
{
    [JsonProperty("renewable_share_target")]  public float renewable_share_target  = 0.9f;
    [JsonProperty("energy_efficiency_target")] public float energy_efficiency_target = 0.4f;
    [JsonProperty("grid_dependency_target")] public float  grid_dependency_target  = 0.1f;
    [JsonProperty("energy_storage_target")]  public float  energy_storage_target   = 0.2f;
    [JsonProperty("net_zero_target")]        public bool   net_zero_target         = true;
}

public class SustainabilityTransportData
{
    [JsonProperty("sustainable_mode_share")]        public float sustainable_mode_share        = 0.75f;
    [JsonProperty("maximum_average_commute_distance")] public float maximum_average_commute_distance = 5000f;
    [JsonProperty("maximum_average_commute_time")]  public float maximum_average_commute_time  = 30f;
    [JsonProperty("vehicle_emission_target")]       public float vehicle_emission_target       = 0f;
}

public class SustainabilityWaterData
{
    [JsonProperty("water_reuse_target")]         public float water_reuse_target         = 0.5f;
    [JsonProperty("water_loss_target")]          public float water_loss_target          = 0.08f;
    [JsonProperty("rainwater_capture_target")]   public float rainwater_capture_target   = 0.3f;
    [JsonProperty("groundwater_recharge_target")] public float groundwater_recharge_target = 0.1f;
}

public class SustainabilityLandData
{
    [JsonProperty("green_space_target")]      public float green_space_target      = 0.35f;
    [JsonProperty("built_land_target")]       public float built_land_target       = 0.45f;
    [JsonProperty("protected_land_target")]   public float protected_land_target   = 0.05f;
    [JsonProperty("permeable_surface_target")] public float permeable_surface_target = 0.5f;
}

public class SustainabilityEnvironmentData
{
    [JsonProperty("carbon_emission_target")]      public float  carbon_emission_target      = 1000f;
    [JsonProperty("air_quality_target")]          public string air_quality_target          = "very_good";
    [JsonProperty("urban_heat_reduction_target")] public float  urban_heat_reduction_target = 2f;
    [JsonProperty("biodiversity_target")]         public float  biodiversity_target         = 0.3f;
}

public class ResourceEfficiencyData
{
    [JsonProperty("waste_recycling_target")]   public float waste_recycling_target   = 0.7f;
    [JsonProperty("waste_to_landfill_target")] public float waste_to_landfill_target = 0.05f;
    [JsonProperty("material_reuse_target")]    public float material_reuse_target    = 0.3f;
    [JsonProperty("circular_economy_target")]  public float circular_economy_target  = 0.5f;
}

// ─── Generation ───────────────────────────────────────────────────────────────

public class GenerationData
{
    [JsonProperty("seed")]               public int       seed               = 0;
    [JsonProperty("deterministic")]      public bool      deterministic      = true;
    [JsonProperty("detail_level")]       public string    detail_level       = "medium";
    [JsonProperty("terrain_detail")]     public string    terrain_detail     = "medium";
    [JsonProperty("building_detail")]    public string    building_detail    = "medium";
    [JsonProperty("vegetation_detail")]  public string    vegetation_detail  = "medium";
    [JsonProperty("road_detail")]        public string    road_detail        = "medium";
    [JsonProperty("randomness")]         public float     randomness         = 0.3f;
    [JsonProperty("lod")]                public LODData   lod                = new LODData();
    [JsonProperty("coordinate_precision")] public float   coordinate_precision = 0.1f;
}

public class LODData
{
    [JsonProperty("enabled")]   public bool         enabled   = false;
    [JsonProperty("distances")] public List<float>  distances = new List<float>();
}
