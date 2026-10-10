// VegetationGenerator.cs
// Places vegetation (trees, shrubs) inside park/forest/green sectors.
// Generates natural rounded trees (cylindrical trunks and spherical organic canopies)
// batched into unified meshes for maximum visual fidelity and 60+ FPS performance.

using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator
{
    private readonly Transform        _parent;
    private readonly CityMaterials    _mats;
    private readonly TerrainGenerator _terrainGen;

    public VegetationGenerator(Transform parent, CityMaterials mats, TerrainGenerator terrainGen = null)
    {
        _parent     = parent;
        _mats       = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        VegetationData    vegetation,
        List<SectorData>  sectors,
        CityData          city,
        int               seed)
    {
        Transform vegRoot = new GameObject("Vegetation").transform;
        vegRoot.SetParent(_parent, false);

        if (vegetation == null || sectors == null)
        {
            Debug.Log("[Vegetation] Generated 0 vegetation objects.");
            return;
        }

        VegetationGenerationData gen = vegetation.generation ?? new VegetationGenerationData();
        float density = gen.density > 0 ? gen.density : 50f;
        var rng = new System.Random(seed + 3);

        MeshBatcher groundBatcher = new MeshBatcher();
        MeshBatcher trunkBatcher  = new MeshBatcher();
        MeshBatcher canopyBatcher = new MeshBatcher();

        int count = 0;

        foreach (var sector in sectors)
        {
            if (!IsVegetationSector(sector.type)) continue;
            if (sector.geometry?.bounds == null || sector.geometry.bounds.Length < 4) continue;

            float sx     = sector.geometry.bounds[0];
            float sz     = sector.geometry.bounds[1];
            float sWidth = sector.geometry.bounds[2];
            float sDepth = sector.geometry.bounds[3];

            // Render rich green park turf/slab at Y=0.10m — clearly above floor (-0.50m)
            // and below block slabs (0.20m) so there is zero z-fighting.
            float pCenterX = sx + sWidth * 0.5f;
            float pCenterZ = sz + sDepth * 0.5f;
            float terrainH = _terrainGen != null ? _terrainGen.SampleHeight(pCenterX, pCenterZ) : 0f;
            groundBatcher.AddBox(new Vector3(pCenterX, terrainH + 0.05f, pCenterZ), new Vector3(sWidth - 2f, 0.10f, sDepth - 2f));

            float areaHa    = (sWidth * sDepth) / 10000f;
            int   treeCount = Mathf.RoundToInt(areaHa * density);
            treeCount = Mathf.Clamp(treeCount, 25, 200);

            for (int i = 0; i < treeCount; i++)
            {
                float px = sx + 5f + (float)rng.NextDouble() * (sWidth - 10f);
                float pz = sz + 5f + (float)rng.NextDouble() * (sDepth - 10f);

                float tHeight = _terrainGen != null ? _terrainGen.SampleHeight(px, pz) : 0f;

                // Organic variety: 70% mature trees, 30% flowering/green shrubs
                bool isShrub = (rng.NextDouble() < 0.25f);

                if (isShrub)
                {
                    float shrubR = 1.0f + (float)rng.NextDouble() * 0.8f;
                    canopyBatcher.AddCanopy(
                        new Vector3(px, tHeight + shrubR * 0.6f, pz),
                        new Vector3(shrubR, shrubR * 0.7f, shrubR),
                        6, 10);
                }
                else
                {
                    float trunkH  = 1.8f + (float)rng.NextDouble() * 2.2f;
                    float trunkR  = 0.25f + (float)rng.NextDouble() * 0.15f;
                    float canopyR = 2.4f + (float)rng.NextDouble() * 2.2f;

                    // Smooth cylindrical trunk
                    trunkBatcher.AddCylinder(
                        new Vector3(px, tHeight, pz),
                        trunkR,
                        trunkH,
                        12);

                    // Multi-tiered smooth canopy
                    canopyBatcher.AddCanopy(
                        new Vector3(px, tHeight + trunkH + canopyR * 0.75f, pz),
                        new Vector3(canopyR, canopyR * 1.15f, canopyR),
                        8,
                        14);
                }

                count++;
            }
        }

        groundBatcher.BuildGameObject("Batched_ParkGrounds",   _mats?.Grass ?? _mats.Vegetation, vegRoot);
        trunkBatcher.BuildGameObject("Batched_Trunks",         _mats?.Industrial ?? _mats.Building, vegRoot);
        canopyBatcher.BuildGameObject("Batched_Canopies",       _mats?.Vegetation ?? _mats.Grass, vegRoot);

        Debug.Log($"[Vegetation] Generated {count} natural trees and shrubs with lush park grounds.");
    }

    private static bool IsVegetationSector(string type) => type is
        "park" or "forest" or "wetland" or "recreation" or "agriculture";
}
