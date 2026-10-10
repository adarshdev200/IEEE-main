// RoadGenerator.cs
// Generates a realistic layered road network:
//   1. Renders all edges from the road_graph (arterials, collectors, locals, cycle, pedestrian).
//   2. Procedurally generates an orthogonal local-road grid inside every developable sector.
//   3. Street trees along arterials and collectors.
// Clear visual hierarchy:
//   - Elevated above ground and blocks so they are crisp, bold, and fully visible from any altitude.
//   - Strong contrast asphalt colors + distinct sidewalk pavements + bright road markings.
//   - High-throughput mesh batching for optimal framerate.

using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator
{
    private Transform _roadsRoot;

    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    // Per-type materials
    private Material _matArterial;
    private Material _matCollector;
    private Material _matLocal;
    private Material _matCurb;
    private Material _matSidewalk;
    private Material _matCycleway;
    private Material _matPedestrian;
    private Material _matCenterLine;
    private Material _matIntersection;
    private Material _matTreeTrunk;
    private Material _matTreeCanopy;

    // Layer elevations — strictly tiered to ensure no coplanar flickering
    private const float IntersectY   = 0.10f;
    private const float RoadY        = 0.10f;
    private const float CenterLineY  = 0.12f;
    private const float SidewalkY    = 0.20f;
    private const float CurbY        = 0.22f;

    private const float MinSegLen    = 0.5f;
    private const float SidewalkWidth = 2.5f;
    private const float CurbWidth     = 0.35f;

    private const float TreeSpacing       = 35f;
    private const float TreeMinSpacing    = 15f;
    private const float TreeTrunkRadius   = 0.25f;
    private const float TreeTrunkHeight   = 2.5f;
    private const float TreeCanopyRadius  = 2.8f;
    private const float TreeCanopyOffsetY = 3.2f;

    private Dictionary<string, Vector2> _nodePositions;

    // High performance batchers
    private MeshBatcher _bArterial;
    private MeshBatcher _bCollector;
    private MeshBatcher _bLocal;
    private MeshBatcher _bCurb;
    private MeshBatcher _bSidewalk;
    private MeshBatcher _bCycleway;
    private MeshBatcher _bPedestrian;
    private MeshBatcher _bCenterLine;
    private MeshBatcher _bIntersection;
    private MeshBatcher _bTreeTrunk;
    private MeshBatcher _bTreeCanopy;

    public RoadGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(RoadGraphData roadGraph, CityData city)
    {
        BuildMaterials();
        InitBatchers();

        _roadsRoot = new GameObject("Roads").transform;
        _roadsRoot.SetParent(_parent, false);

        _nodePositions = new Dictionary<string, Vector2>();

        if (roadGraph != null)
        {
            foreach (var node in roadGraph.nodes)
                _nodePositions[node.id] = new Vector2(node.position.x, node.position.z);

            foreach (var node in roadGraph.nodes)
                RenderIntersectionPad(node, roadGraph.edges);

            int edgeCount = 0;
            foreach (var edge in roadGraph.edges)
            {
                if (!_nodePositions.TryGetValue(edge.from, out var a)) continue;
                if (!_nodePositions.TryGetValue(edge.to,   out var b)) continue;
                RenderEdge(edge, a, b);
                edgeCount++;
            }

            Debug.Log($"[Roads] Rendered {edgeCount} road_graph edges.");
        }

        FlushBatches();
        Debug.Log("[Roads] Road generation complete.");
    }

    public void GenerateWithSectors(
        RoadGraphData      roadGraph,
        List<SectorData>   sectors,
        CityData           city)
    {
        BuildMaterials();
        InitBatchers();

        _roadsRoot = new GameObject("Roads").transform;
        _roadsRoot.SetParent(_parent, false);

        _nodePositions = new Dictionary<string, Vector2>();

        if (roadGraph != null)
        {
            foreach (var node in roadGraph.nodes)
                _nodePositions[node.id] = new Vector2(node.position.x, node.position.z);

            foreach (var node in roadGraph.nodes)
                RenderIntersectionPad(node, roadGraph.edges);

            foreach (var edge in roadGraph.edges)
            {
                if (!_nodePositions.TryGetValue(edge.from, out var a)) continue;
                if (!_nodePositions.TryGetValue(edge.to,   out var b)) continue;
                RenderEdge(edge, a, b);
            }
        }

        if (sectors != null)
        {
            int localCount = 0;
            foreach (var sector in sectors)
            {
                if (IsNonRoadSector(sector.type)) continue;
                if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;
                localCount += GenerateLocalGrid(sector);
            }
            Debug.Log($"[Roads] Generated {localCount} local road segments across sectors.");
        }

        FlushBatches();
        Debug.Log("[Roads] Road generation complete with all meshes batched into single draw calls.");
    }

    private void InitBatchers()
    {
        _bArterial     = new MeshBatcher();
        _bCollector    = new MeshBatcher();
        _bLocal        = new MeshBatcher();
        _bCurb         = new MeshBatcher();
        _bSidewalk     = new MeshBatcher();
        _bCycleway     = new MeshBatcher();
        _bPedestrian   = new MeshBatcher();
        _bCenterLine   = new MeshBatcher();
        _bIntersection = new MeshBatcher();
        _bTreeTrunk    = new MeshBatcher();
        _bTreeCanopy   = new MeshBatcher();
    }

    private void FlushBatches()
    {
        _bIntersection.BuildGameObject("Batched_Intersections", _matIntersection, _roadsRoot);
        _bArterial.BuildGameObject("Batched_Arterials",         _matArterial,     _roadsRoot);
        _bCollector.BuildGameObject("Batched_Collectors",       _matCollector,    _roadsRoot);
        _bLocal.BuildGameObject("Batched_Locals",               _matLocal,        _roadsRoot);
        _bCurb.BuildGameObject("Batched_Curbs",                 _matCurb,         _roadsRoot);
        _bSidewalk.BuildGameObject("Batched_Sidewalks",         _matSidewalk,     _roadsRoot);
        _bCycleway.BuildGameObject("Batched_Cycleways",         _matCycleway,     _roadsRoot);
        _bPedestrian.BuildGameObject("Batched_Pedestrians",     _matPedestrian,   _roadsRoot);
        _bCenterLine.BuildGameObject("Batched_CenterLines",     _matCenterLine,   _roadsRoot);
        _bTreeTrunk.BuildGameObject("Batched_TreeTrunks",       _matTreeTrunk,    _roadsRoot);
        _bTreeCanopy.BuildGameObject("Batched_TreeCanopies",    _matTreeCanopy,   _roadsRoot);
    }

    private void BuildMaterials()
    {
        // High-contrast, vibrant road materials
        _matArterial    = MakeRoad("Road_Arterial",    new Color(0.12f, 0.13f, 0.15f), 0.40f);
        _matCollector   = MakeRoad("Road_Collector",   new Color(0.15f, 0.16f, 0.18f), 0.35f);
        _matLocal       = MakeRoad("Road_Local",       new Color(0.18f, 0.19f, 0.22f), 0.30f);
        _matCurb        = MakeRoad("Road_Curb",        new Color(0.65f, 0.65f, 0.68f), 0.25f);
        _matSidewalk    = MakeRoad("Road_Sidewalk",    new Color(0.85f, 0.83f, 0.78f), 0.15f);
        _matCycleway    = MakeRoad("Road_Cycle",       new Color(0.20f, 0.65f, 0.35f), 0.30f);
        _matPedestrian  = MakeRoad("Road_Pedestrian",  new Color(0.88f, 0.85f, 0.80f), 0.15f);
        _matCenterLine  = MakeEmissive("Road_CenterLine", new Color(1.0f, 0.90f, 0.2f), new Color(1.5f, 1.2f, 0.1f));
        _matIntersection = MakeRoad("Road_Intersection", new Color(0.11f, 0.12f, 0.14f), 0.40f);
        _matTreeTrunk   = MakeRoad("Tree_Trunk",  new Color(0.38f, 0.25f, 0.14f), 0.15f);
        _matTreeCanopy  = MakeRoad("Tree_Canopy", new Color(0.15f, 0.52f, 0.18f), 0.25f);
    }

    private static Material MakeRoad(string name, Color color, float smoothness)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = name };
        m.enableInstancing = true;
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor",  color);
        if (m.HasProperty("_Color"))      m.SetColor("_Color",      color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private static Material MakeEmissive(string name, Color color, Color emit)
    {
        var m = MakeRoad(name, color, 0.6f);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emit);
        }
        return m;
    }

    private float GetNodeHalfPad(string nodeId)
    {
        return 8f;
    }

    private void RenderIntersectionPad(RoadNodeData node, List<RoadEdgeData> edges)
    {
        Vector2 pos = new Vector2(node.position.x, node.position.z);
        float h = GetNodeHalfPad(node.id);
        float y = GetY(pos.x, pos.y) + IntersectY;

        Vector3 v0 = new Vector3(pos.x - h, y, pos.y - h);
        Vector3 v1 = new Vector3(pos.x - h, y, pos.y + h);
        Vector3 v2 = new Vector3(pos.x + h, y, pos.y + h);
        Vector3 v3 = new Vector3(pos.x + h, y, pos.y - h);

        _bIntersection.AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
    }

    private void RenderEdge(RoadEdgeData edge, Vector2 a, Vector2 b)
    {
        bool isArterial  = edge.type == "arterial";
        bool isCollector = edge.type == "collector";
        bool isCycle     = edge.type == "cycle";
        bool isPed       = edge.type == "pedestrian";

        float roadW = edge.width > 0f ? edge.width : DefaultWidth(edge.type);
        MeshBatcher targetBatcher = BatcherForType(edge.type);

        float padHalfA = GetNodeHalfPad(edge.from);
        float padHalfB = GetNodeHalfPad(edge.to);

        Vector2 dir = (b - a).normalized;
        float totalLen = Vector2.Distance(a, b);
        if (totalLen > padHalfA + padHalfB + 1f)
        {
            a += dir * padHalfA;
            b -= dir * padHalfB;
        }

        RenderCrossSection(edge, a, b, roadW, targetBatcher, isArterial, isCollector, isCycle, isPed);
    }

    private static bool ShouldRouteOrthogonally(string edgeType, Vector2 a, Vector2 b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dz = Mathf.Abs(a.y - b.y);

        // If road is essentially straight along X or Z axis, no elbow needed
        if (dx < 40f || dz < 40f) return false;

        // Long-distance inter-district connections (arterials, collectors, local utility links)
        // look believable when adhering to orthogonal grid planning rather than arbitrary diagonals
        return edgeType is "arterial" or "collector" or "local";
    }

    private void RenderCornerPad(Vector2 corner, float roadWidth)
    {
        float padSize = roadWidth + (SidewalkWidth + CurbWidth) * 2f;
        float y       = GetY(corner.x, corner.y) + IntersectY;
        float h       = padSize * 0.5f;

        Vector3 v0 = new Vector3(corner.x - h, y, corner.y - h);
        Vector3 v1 = new Vector3(corner.x - h, y, corner.y + h);
        Vector3 v2 = new Vector3(corner.x + h, y, corner.y + h);
        Vector3 v3 = new Vector3(corner.x + h, y, corner.y - h);

        _bIntersection.AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
    }

    private void RenderCrossSection(
        RoadEdgeData edge,
        Vector2 a2, Vector2 b2,
        float roadW,
        MeshBatcher roadBatcher,
        bool isArterial, bool isCollector,
        bool isCycle,    bool isPed)
    {
        float len = Vector2.Distance(a2, b2);
        if (len < MinSegLen) return;

        bool hasSidewalk = !isCycle && !isPed && (edge.sidewalk?.enabled ?? true);
        bool hasCycle    = !isCycle && !isPed && (edge.cycling?.enabled ?? false);

        // Road body
        AddFlatStrip(roadBatcher, a2, b2, roadW, 0f, RoadY);

        // Sidewalks & Curbs
        if (hasSidewalk)
        {
            float leftCenter = roadW * 0.5f + CurbWidth * 0.5f;
            AddFlatStrip(_bCurb, a2, b2, CurbWidth, -leftCenter, CurbY);
            AddFlatStrip(_bCurb, a2, b2, CurbWidth,  leftCenter, CurbY);

            float leftSWCenter = roadW * 0.5f + CurbWidth + SidewalkWidth * 0.5f;
            AddFlatStrip(_bSidewalk, a2, b2, SidewalkWidth, -leftSWCenter, SidewalkY);
            AddFlatStrip(_bSidewalk, a2, b2, SidewalkWidth,  leftSWCenter, SidewalkY);
        }

        // Cycle lane
        if (hasCycle)
        {
            float cycleW      = edge.cycling.width > 0f ? edge.cycling.width : 1.5f;
            float totalOffset = roadW * 0.5f + CurbWidth + SidewalkWidth + cycleW * 0.5f;
            AddFlatStrip(_bCycleway, a2, b2, cycleW, -totalOffset, SidewalkY + 0.005f);
            AddFlatStrip(_bCycleway, a2, b2, cycleW,  totalOffset, SidewalkY + 0.005f);
        }

        // Center line markings
        if (isArterial || isCollector)
        {
            float dashLen  = isArterial ? 5f : 3f;
            float dashGap  = isArterial ? 5f : 4f;
            float dashW    = 0.35f;
            float markingY = GetMidY(a2, b2) + CenterLineY;

            AddDashedLine(_bCenterLine, a2, b2, dashW, dashLen, dashGap, markingY);
        }

        // Street trees
        if ((isArterial || isCollector) && (edge.street_trees?.enabled ?? false))
        {
            float treeOffset = roadW * 0.5f + CurbWidth + SidewalkWidth * 0.7f;
            AddStreetTrees(a2, b2, treeOffset, TreeSpacing);
        }
    }

    private void AddRectQuad(MeshBatcher batcher, float minX, float minZ, float width, float depth, float yOffset)
    {
        float y = GetY(minX + width * 0.5f, minZ + depth * 0.5f) + yOffset;
        Vector3 v0 = new Vector3(minX,         y, minZ);
        Vector3 v1 = new Vector3(minX,         y, minZ + depth);
        Vector3 v2 = new Vector3(minX + width, y, minZ + depth);
        Vector3 v3 = new Vector3(minX + width, y, minZ);
        batcher.AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
    }

    private int GenerateLocalGrid(SectorData sector)
    {
        float[] bounds = sector.geometry.bounds;
        float sx     = bounds[0];
        float sz     = bounds[1];
        float sWidth = bounds[2];
        float sDepth = bounds[3];

        float roadW   = sector.street_rules?.local_roads?.width > 0
            ? sector.street_rules.local_roads.width : 9f;

        float blockW = sector.street_rules?.blocks != null
            ? (sector.street_rules.blocks.minimum_width + sector.street_rules.blocks.maximum_width) * 0.5f
            : BlockSpacingForType(sector.type);

        float blockD = sector.street_rules?.blocks != null
            ? (sector.street_rules.blocks.minimum_depth + sector.street_rules.blocks.maximum_depth) * 0.5f
            : blockW * 0.8f;

        blockW = Mathf.Max(blockW, 60f);
        blockD = Mathf.Max(blockD, 50f);

        bool hasTrees     = sector.street_rules?.street_trees?.enabled ?? false;
        float treeSpacing = sector.street_rules?.street_trees?.spacing > 0
            ? Mathf.Max(sector.street_rules.street_trees.spacing, 15f) : 25f;

        int count = 0;

        float curZ = sz + roadW;
        while (curZ + blockD < sz + sDepth - roadW)
        {
            float curX = sx + roadW;
            while (curX + blockW < sx + sWidth - roadW)
            {
                // East road segment between this block and next column (disjoint from block)
                AddRectQuad(_bLocal, curX + blockW, curZ, roadW, blockD, RoadY);

                // North road segment between this block and next row (disjoint from block)
                AddRectQuad(_bLocal, curX, curZ + blockD, blockW, roadW, RoadY);

                // Intersection square at north-east corner (disjoint from road segments and blocks)
                AddRectQuad(_bIntersection, curX + blockW, curZ + blockD, roadW, roadW, IntersectY);

                if (hasTrees && blockD > 25f)
                {
                    AddStreetTrees(
                        new Vector2(curX + blockW + roadW * 0.5f, curZ),
                        new Vector2(curX + blockW + roadW * 0.5f, curZ + blockD),
                        roadW * 0.5f + 1.5f, treeSpacing);
                }

                curX += blockW + roadW;
                count++;
            }
            curZ += blockD + roadW;
        }

        return count;
    }

    private void AddFlatStrip(
        MeshBatcher batcher,
        Vector2 a2, Vector2 b2,
        float width,
        float lateralOffset,
        float yOffset)
    {
        float len = Vector2.Distance(a2, b2);
        if (len < MinSegLen) return;

        Vector2 dir2   = (b2 - a2).normalized;
        Vector2 right2 = new Vector2(dir2.y, -dir2.x);

        Vector2 ca = a2 + right2 * lateralOffset;
        Vector2 cb = b2 + right2 * lateralOffset;

        float hw = width * 0.5f;
        const float ovlp = 0f;
        Vector2 ea = ca - dir2 * ovlp;
        Vector2 eb = cb + dir2 * ovlp;

        // Looking from above (+Y), CCW order is:
        // leftStart -> leftEnd -> rightEnd -> rightStart
        Vector3 v0 = ToV3(ea - right2 * hw, yOffset);
        Vector3 v1 = ToV3(eb - right2 * hw, yOffset);
        Vector3 v2 = ToV3(eb + right2 * hw, yOffset);
        Vector3 v3 = ToV3(ea + right2 * hw, yOffset);

        batcher.AddQuad(v0, v1, v2, v3, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
    }

    private void AddDashedLine(
        MeshBatcher batcher,
        Vector2 a2, Vector2 b2,
        float dashW, float dashLen, float dashGap,
        float worldY)
    {
        float totalLen = Vector2.Distance(a2, b2);
        if (totalLen < dashLen) return;

        Vector2 dir    = (b2 - a2).normalized;
        Vector2 right2 = new Vector2(dir.y, -dir.x);
        float hw       = dashW * 0.5f;

        float traveled = dashLen * 0.5f;
        int count = 0;

        while (traveled + dashLen < totalLen && count < 250)
        {
            Vector2 da = a2 + dir * traveled;
            Vector2 db = a2 + dir * (traveled + dashLen);

            // Looking from above (+Y), CCW order
            Vector3 v0 = new Vector3(da.x - right2.x * hw, worldY, da.y - right2.y * hw);
            Vector3 v1 = new Vector3(db.x - right2.x * hw, worldY, db.y - right2.y * hw);
            Vector3 v2 = new Vector3(db.x + right2.x * hw, worldY, db.y + right2.y * hw);
            Vector3 v3 = new Vector3(da.x + right2.x * hw, worldY, da.y + right2.y * hw);

            batcher.AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);

            traveled += dashLen + dashGap;
            count++;
        }
    }

    private void AddStreetTrees(
        Vector2 a2, Vector2 b2,
        float lateralOffset,
        float spacing)
    {
        float totalLen = Vector2.Distance(a2, b2);
        if (totalLen < TreeMinSpacing) return;

        Vector2 dir   = (b2 - a2).normalized;
        Vector2 right = new Vector2(dir.y, -dir.x);

        float dist = spacing * 0.5f;
        float[] sides = { -lateralOffset, lateralOffset };
        int count = 0;

        while (dist < totalLen && count < 50)
        {
            Vector2 pt = a2 + dir * dist;

            foreach (float side in sides)
            {
                Vector2 treePos = pt + right * side;
                float ty = GetY(treePos.x, treePos.y) + SidewalkY;

                // Rounded organic tree trunk with smooth normals
                _bTreeTrunk.AddCylinder(
                    new Vector3(treePos.x, ty, treePos.y),
                    TreeTrunkRadius,
                    TreeTrunkHeight,
                    12);

                // Rounded organic spherical canopy with smooth outward normals
                _bTreeCanopy.AddCanopy(
                    new Vector3(treePos.x, ty + TreeTrunkHeight + TreeCanopyRadius * 0.8f, treePos.y),
                    Vector3.one * TreeCanopyRadius,
                    8,
                    12);
            }

            dist += spacing;
            count++;
        }
    }

    private Vector3 ToV3(Vector2 p, float yOff)
        => new Vector3(p.x, GetY(p.x, p.y) + yOff, p.y);

    private float GetY(float x, float z)
        => (_terrainGen != null ? _terrainGen.SampleHeight(x, z) : 0f);

    private float GetMidY(Vector2 a, Vector2 b)
    {
        Vector2 m = (a + b) * 0.5f;
        return GetY(m.x, m.y);
    }

    private MeshBatcher BatcherForType(string type) => type switch
    {
        "arterial"   => _bArterial,
        "collector"  => _bCollector,
        "pedestrian" => _bPedestrian,
        "cycle"      => _bCycleway,
        _            => _bLocal,
    };

    private static float DefaultWidth(string type) => type switch
    {
        "arterial"   => 18f,
        "collector"  => 12f,
        "pedestrian" => 4f,
        "cycle"      => 3f,
        _            => 8f,
    };

    private static float BlockSpacingForType(string type) => type switch
    {
        "commercial" or "mixed_use" => 120f,
        "industrial"                => 180f,
        "civic"                     => 130f,
        _                           => 100f,
    };

    private static bool IsNonRoadSector(string type) => type is
        "park" or "forest" or "wetland" or "water" or "energy";
}
