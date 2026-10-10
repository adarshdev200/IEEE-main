
using System.Collections.Generic;
using UnityEngine;

public class WaterGenerator
{
    private readonly Transform _parent;
    private readonly CityMaterials _mats;
    private readonly TerrainGenerator _terrainGen;

    private const float WaterOffset = 0.05f;

    public WaterGenerator(
        Transform parent,
        CityMaterials mats,
        TerrainGenerator terrainGen = null)
    {
        _parent = parent;
        _mats = mats;
        _terrainGen = terrainGen;
    }

    public void Generate(
        WaterData waterData,
        List<SectorData> sectors,
        CityData city)
    {
        Transform waterRoot = new GameObject("Water").transform;
        waterRoot.SetParent(_parent, false);

        int count = 0;

        if (sectors != null)
        {
            foreach (var sector in sectors)
            {
                if (sector.type != "water")
                    continue;

                if (sector.geometry?.bounds == null ||
                    sector.geometry.bounds.Length < 4)
                    continue;

                float x = sector.geometry.bounds[0];
                float z = sector.geometry.bounds[1];
                float width = sector.geometry.bounds[2];
                float depth = sector.geometry.bounds[3];

                if (width <= 0f || depth <= 0f)
                    continue;

                CreateWaterBody(
                    sector.id, x, z, width, depth, waterRoot);

                count++;
            }
        }

        Debug.Log($"[Water] Generated {count} water bodies.");
    }

    public void CreateWaterBody(
        string id,
        float x,
        float z,
        float width,
        float depth,
        Transform parent)
    {
        if (width <= 0f || depth <= 0f)
            return;

        float centerX = x + width * 0.5f;
        float centerZ = z + depth * 0.5f;

        float terrainH = _terrainGen != null
            ? _terrainGen.SampleHeight(centerX, centerZ)
            : 0f;

        float surfaceY = terrainH + WaterOffset;

        Vector3[] vertices =
        {
            new Vector3(-width * 0.5f, 0f, -depth * 0.5f),
            new Vector3(-width * 0.5f, 0f,  depth * 0.5f),
            new Vector3( width * 0.5f, 0f, -depth * 0.5f),
            new Vector3( width * 0.5f, 0f,  depth * 0.5f)
        };

        int[] triangles =
        {
            0, 1, 2,
            2, 1, 3
        };

        Vector2[] uv =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f)
        };

        Mesh mesh = new Mesh
        {
            name = $"Water_{id}_Mesh",
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject($"Water_{id}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(
            centerX, surfaceY, centerZ);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        if (_mats?.Water != null)
            renderer.sharedMaterial = _mats.Water;

        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
