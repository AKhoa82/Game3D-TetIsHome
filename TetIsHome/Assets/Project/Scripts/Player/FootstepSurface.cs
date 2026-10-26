using System;
using UnityEngine;

public enum FootstepSurfaceType
{
    Default,
    Grass,
    Dirt,
    Stone
}

[Serializable]
public class TerrainSurfaceRule
{
    public TerrainLayer terrainLayer;
    public FootstepSurfaceType surface = FootstepSurfaceType.Grass;
}

[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [Header("Surface Settings")]
    [SerializeField]
    private FootstepSurfaceType defaultSurface = FootstepSurfaceType.Grass;

    [Header("Terrain Layer Mapping")]
    [SerializeField]
    private TerrainSurfaceRule[] terrainSurfaceRules;

    private Terrain terrain;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
    }

    public FootstepSurfaceType GetSurface(Vector3 position)
    {
        // Doi tuong thong thuong: san, duong, da
        if (terrain == null || terrain.terrainData == null)
            return defaultSurface;

        TerrainData data = terrain.terrainData;

        if (data.terrainLayers.Length == 0)
            return defaultSurface;

        if (data.alphamapWidth < 1 || data.alphamapHeight < 1)
            return defaultSurface;

        Vector3 local = position - terrain.transform.position;

        float x = Mathf.Clamp01(local.x / data.size.x);
        float z = Mathf.Clamp01(local.z / data.size.z);

        int mapX = Mathf.Clamp(Mathf.FloorToInt(x * data.alphamapWidth), 0, data.alphamapWidth - 1);

        int mapZ = Mathf.Clamp(Mathf.FloorToInt(z * data.alphamapHeight), 0, data.alphamapHeight - 1);

        float[,,] weights = data.GetAlphamaps(mapX, mapZ, 1, 1);

        int bestLayer = 0;
        float bestWeight = -1f;

        for (int i = 0; i < weights.GetLength(2); i++)
        {
            if (weights[0, 0, i] > bestWeight)
            {
                bestWeight = weights[0, 0, i];
                bestLayer = i;
            }
        }

        TerrainLayer selectedLayer = data.terrainLayers[bestLayer];

        if (terrainSurfaceRules != null)
        {
            foreach (var rule in terrainSurfaceRules)
            {
                if (rule != null && rule.terrainLayer == selectedLayer)
                {
                    return rule.surface;
                }
            }
        }

        return defaultSurface;
    }
}