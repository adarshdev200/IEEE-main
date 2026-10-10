// BlockGenerator.cs
// Generates urban blocks by subdividing each sector with a local grid of roads.
// Optimized using MeshBatcher: combines all 2000+ block sidewalk slabs into a single mesh!

using System.Collections.Generic;
using UnityEngine;

/// <summary>Lightweight representation of a generated urban block.</summary>
public struct GeneratedBlock
{
    public float  x, z;       // origin (world space)
    public float  width, depth;
    public string sectorId;
    public string sectorType;
}

public class BlockGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public List<GeneratedBlock> GeneratedBlocks { get; private set; } = new List<GeneratedBlock>();

    private Transform _blocksRoot;
    public const float BlockTopY = 0.20f;  // Top surface of block sidewalk slabs

    public BlockGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        List<SectorData> sectors,
        RoadGraphData    roadGraph,
        BlocksData       blocks,
        CityData         city,
        int              seed)
    {
        _blocksRoot = new GameObject("Blocks").transform;
        _blocksRoot.SetParent(_parent, false);
        GeneratedBlocks.Clear();

        if (sectors == null) return;

        BlockSizeData sizeRules = blocks?.size ?? new BlockSizeData();

        MeshBatcher batcher = new MeshBatcher();
        int totalBlocks = 0;

        foreach (var sector in sectors)
        {
            if (IsNonDevelopable(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            float blockW = GetBlockWidth(sector, sizeRules);
            float blockD = GetBlockDepth(sector, sizeRules);
            float roadW  = GetLocalRoadWidth(sector);

            float curZ = sz + roadW;
            while (curZ + blockD < sz + sDepth - roadW)
            {
                float curX = sx + roadW;
                while (curX + blockW < sx + sWidth - roadW)
                {
                    var block = new GeneratedBlock
                    {
                        x = curX, z = curZ,
                        width = blockW, depth = blockD,
                        sectorId   = sector.id,
                        sectorType = sector.type,
                    };
                    GeneratedBlocks.Add(block);

                    // Add to batched mesh instead of creating a GameObject
                    float centerX = curX + blockW * 0.5f;
                    float centerZ = curZ + blockD * 0.5f;
                    float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(centerX, centerZ) : 0f;
                    // Slab thickness 0.10m, top face sits at terrainH + BlockTopY (0.20m)
                    batcher.AddBox(new Vector3(centerX, terrainH + BlockTopY - 0.05f, centerZ), new Vector3(blockW, 0.10f, blockD));

                    totalBlocks++;
                    curX += blockW + roadW;
                }
                curZ += blockD + roadW;
            }
        }

        // Single batched mesh for all blocks
        batcher.BuildGameObject("Batched_Blocks", _mats.Sidewalk, _blocksRoot);

        Debug.Log($"[Blocks] Generated {totalBlocks} blocks batched into 1 mesh.");
    }

    private static bool IsNonDevelopable(string type) => type is
        "park" or "forest" or "wetland" or "water" or "agriculture" or "energy";

    private static float GetBlockWidth(SectorData sector, BlockSizeData size)
    {
        if (sector.street_rules?.blocks != null)
        {
            float min = sector.street_rules.blocks.minimum_width;
            float max = sector.street_rules.blocks.maximum_width;
            return (min + max) * 0.5f;
        }
        float smin = size.minimum_width > 0 ? size.minimum_width : 60f;
        float smax = size.maximum_width > 0 ? size.maximum_width : 150f;
        return (smin + smax) * 0.5f;
    }

    private static float GetBlockDepth(SectorData sector, BlockSizeData size)
    {
        if (sector.street_rules?.blocks != null)
        {
            float min = sector.street_rules.blocks.minimum_depth;
            float max = sector.street_rules.blocks.maximum_depth;
            return (min + max) * 0.5f;
        }
        float smin = size.minimum_depth > 0 ? size.minimum_depth : 40f;
        float smax = size.maximum_depth > 0 ? size.maximum_depth : 100f;
        return (smin + smax) * 0.5f;
    }

    private static float GetLocalRoadWidth(SectorData sector)
    {
        return sector.street_rules?.local_roads?.width > 0
            ? sector.street_rules.local_roads.width
            : 8f;
    }
}
