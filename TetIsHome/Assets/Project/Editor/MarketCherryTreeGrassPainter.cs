using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketCherryTreeGrassPainter
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string GeneratedFolder = "Assets/Project/Scenes/04_Market/Generated";
    private const string BaseGrassLayerPath =
        "Assets/FantasyEnvironments/Environments/TerrainLayers/layer_ground_grass.terrainlayer";
    private const string RoadLayerPath =
        "Assets/FantasyEnvironments/Environments/TerrainLayers/layer_ground_road.terrainlayer";
    private const string CenterRootName = "03_Central_Landmark";
    private const string TreeName = "Central Cherry Blossom Tree";
    private const string GrassRootName = "Painted Grass - Cherry Tree";
    private const string NatureRootName = "Random Ground Nature";
    private const string NaturePrefabFolder = "Assets/Project/Prefabs/Environment/MarketNature";
    private const int GrassCount = 84;

    private static readonly string[] GrassPrefabPaths =
    {
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass1.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass2.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass3.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass4.prefab"
    };

    private static readonly string[] NatureGrassSourcePaths =
    {
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass1.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass2.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass3.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Grass4.prefab"
    };

    private static readonly string[] NatureFlowerSourcePaths =
    {
        "Assets/FantasyEnvironments/Environments/Prefabs/Flower1.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Flower3.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Flower5.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Flower7.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Flower9.prefab"
    };

    private static readonly string[] NatureRockSourcePaths =
    {
        "Assets/FantasyEnvironments/Environments/Prefabs/Rock1.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Rock2.prefab",
        "Assets/FantasyEnvironments/Environments/Prefabs/Rock3.prefab"
    };

    private static readonly string[] MeshObjectsToRemove =
    {
        "Central Plaza",
        "Central Garden",
        "Tree Planter Outer",
        "Tree Planter Soil",
        "Main South Path",
        "North Path",
        "East Path",
        "West Path",
        "Circular Path",
        "Approach Road"
    };

    [MenuItem("Tools/Tet Is Home/Rebuild Natural Ground Around Market Cherry Tree")]
    public static void RebuildFromMenu()
    {
        RebuildAndSave();
        Selection.activeGameObject = GameObject.Find(GrassRootName);
    }

    // Entry point used by Unity batch mode and CI.
    public static void RebuildAndSave()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        GameObject centerRoot = GameObject.Find(CenterRootName);
        GameObject tree = GameObject.Find(TreeName);

        if (terrain == null || centerRoot == null || tree == null)
            throw new InvalidOperationException("The market scene is missing its Terrain, central landmark, or cherry tree.");

        RemoveMeshBasedGroundObjects();

        Vector3 treePosition = tree.transform.position;
        float treeGroundY = SampleGroundY(terrain, treePosition);
        tree.transform.position = new Vector3(treePosition.x, treeGroundY + 0.02f, treePosition.z);

        TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BaseGrassLayerPath);
        if (grassLayer == null)
            throw new InvalidOperationException("Missing required grass Terrain Layer: " + BaseGrassLayerPath);
        TerrainLayer roadLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadLayerPath);
        if (roadLayer == null)
            throw new InvalidOperationException("Missing required road Terrain Layer: " + RoadLayerPath);

        TerrainLayer soilLayer = CreateOrUpdateSoilLayer();
        SetLayersAndPaintGround(terrain, grassLayer, soilLayer, roadLayer, tree.transform.position);
        RebuildGrass(scene, centerRoot.transform, terrain, tree.transform.position);
        ScatterGroundNature(scene, terrain, tree.transform.position);
        RepositionInteractionMarker(terrain);

        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the rebuilt market scene.");

        AssetDatabase.SaveAssets();
        Debug.Log("[MarketCherryTreeGrassPainter] Painted Terrain Layers and placed 180 grass, 70 flowers, 32 rocks, plus 84 cherry-tree grass clumps.");
    }

    private static void RemoveMeshBasedGroundObjects()
    {
        GameObject[] sceneObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (Array.IndexOf(MeshObjectsToRemove, sceneObject.name) >= 0)
                UnityEngine.Object.DestroyImmediate(sceneObject);
        }
    }

    private static TerrainLayer CreateOrUpdateSoilLayer()
    {
        string texturePath = $"{GeneratedFolder}/T_Soil.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, true) { name = "T_Soil" };
            AssetDatabase.CreateAsset(texture, texturePath);
        }
        else if (texture.width != 16 || texture.height != 16)
        {
            texture.Reinitialize(16, 16, TextureFormat.RGBA32, true);
        }

        Color[] pixels = new Color[16 * 16];
        Color soilBase = new(0.36f, 0.22f, 0.105f, 1f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.31f + 2.7f, y * 0.31f + 6.2f);
                float variation = Mathf.Lerp(0.78f, 1.18f, noise);
                pixels[y * 16 + x] = new Color(
                    soilBase.r * variation,
                    soilBase.g * variation,
                    soilBase.b * variation,
                    1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);

        string layerPath = $"{GeneratedFolder}/MarketSoil.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer { name = "Market Soil" };
            AssetDatabase.CreateAsset(layer, layerPath);
        }

        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(5.5f, 5.5f);
        layer.metallic = 0f;
        layer.smoothness = 0f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static void SetLayersAndPaintGround(
        Terrain terrain,
        TerrainLayer grassLayer,
        TerrainLayer soilLayer,
        TerrainLayer roadLayer,
        Vector3 center)
    {
        TerrainData data = terrain.terrainData;
        data.terrainLayers = new[] { grassLayer, soilLayer, roadLayer };

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] alphamaps = data.GetAlphamaps(0, 0, width, height);

        for (int z = 0; z < height; z++)
        {
            float worldZ = terrain.transform.position.z + z / (float)(height - 1) * data.size.z;
            for (int x = 0; x < width; x++)
            {
                float worldX = terrain.transform.position.x + x / (float)(width - 1) * data.size.x;
                float distance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(center.x, center.z));
                float edgeNoise = (Mathf.PerlinNoise(worldX * 0.17f + 3.4f, worldZ * 0.17f + 8.1f) - 0.5f) * 0.8f;
                float soilWeight = 1f - SmoothRange(2.2f, 4.2f, distance + edgeNoise);
                float roadWeight = CalculateRoadWeight(worldX, worldZ);
                soilWeight *= 1f - roadWeight;

                alphamaps[z, x, 0] = 1f - soilWeight - roadWeight;
                alphamaps[z, x, 1] = soilWeight;
                alphamaps[z, x, 2] = roadWeight;
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }

    private static float CalculateRoadWeight(float worldX, float worldZ)
    {
        float weight = 0f;
        weight = Mathf.Max(weight, SoftRectangleWeight(worldX, worldZ, 0f, -27f, 8f, 35f));
        weight = Mathf.Max(weight, SoftRectangleWeight(worldX, worldZ, 0f, 33f, 7f, 20f));
        weight = Mathf.Max(weight, SoftRectangleWeight(worldX, worldZ, 40f, 0f, 36f, 7f));
        weight = Mathf.Max(weight, SoftRectangleWeight(worldX, worldZ, -40f, 0f, 36f, 7f));
        weight = Mathf.Max(weight, SoftRectangleWeight(worldX, worldZ, 0f, -61f, 9f, 18f));

        float ringSignedDistance = Mathf.Abs(new Vector2(worldX, worldZ).magnitude - 25.2f) - 1.9f;
        float ringNoise = (Mathf.PerlinNoise(worldX * 0.13f + 4.1f, worldZ * 0.13f + 1.7f) - 0.5f) * 0.35f;
        float ringWeight = 1f - SmoothRange(-0.35f, 0.8f, ringSignedDistance + ringNoise);
        return Mathf.Max(weight, ringWeight);
    }

    private static float SoftRectangleWeight(
        float x,
        float z,
        float centerX,
        float centerZ,
        float sizeX,
        float sizeZ)
    {
        Vector2 delta = new(Mathf.Abs(x - centerX) - sizeX * 0.5f, Mathf.Abs(z - centerZ) - sizeZ * 0.5f);
        float outsideDistance = new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude;
        float insideDistance = Mathf.Min(Mathf.Max(delta.x, delta.y), 0f);
        float signedDistance = outsideDistance + insideDistance;
        float edgeNoise = (Mathf.PerlinNoise(x * 0.15f + 7.3f, z * 0.15f + 2.9f) - 0.5f) * 0.3f;
        return 1f - SmoothRange(-0.35f, 0.8f, signedDistance + edgeNoise);
    }

    private static float SmoothRange(float edge0, float edge1, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge0, edge1, value));
    }

    private static void RebuildGrass(Scene scene, Transform parent, Terrain terrain, Vector3 center)
    {
        Transform oldGrass = parent.Find(GrassRootName);
        if (oldGrass != null)
            UnityEngine.Object.DestroyImmediate(oldGrass.gameObject);

        GameObject[] grassPrefabs = new GameObject[GrassPrefabPaths.Length];
        for (int i = 0; i < GrassPrefabPaths.Length; i++)
        {
            grassPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPaths[i]);
            if (grassPrefabs[i] == null)
                throw new InvalidOperationException("Missing grass prefab: " + GrassPrefabPaths[i]);
        }

        GameObject grassRoot = new(GrassRootName);
        grassRoot.transform.SetParent(parent, false);

        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(20260217);

        for (int i = 0; i < GrassCount; i++)
        {
            float radius = Mathf.Lerp(0.65f, 3.8f, Mathf.Sqrt(UnityEngine.Random.value));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            position.y = SampleGroundY(terrain, position) + 0.025f;

            GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(
                grassPrefabs[UnityEngine.Random.Range(0, grassPrefabs.Length)], scene);
            grass.name = $"Painted Grass {i + 1:00}";
            grass.transform.SetParent(grassRoot.transform, true);
            grass.transform.position = position;
            grass.transform.rotation = Quaternion.Euler(
                0f,
                UnityEngine.Random.Range(0f, 360f),
                0f);

            float scale = UnityEngine.Random.Range(0.48f, 0.76f);
            grass.transform.localScale = new Vector3(
                scale * UnityEngine.Random.Range(0.88f, 1.12f),
                scale * UnityEngine.Random.Range(0.92f, 1.18f),
                scale * UnityEngine.Random.Range(0.88f, 1.12f));
            // These card-based grass meshes have transparent padding below their visible blades.
            // Sink them a little farther so the visible stems meet the soil instead of hovering.
            SnapRendererBottomToTerrain(grass, terrain, 0.08f);
        }

        UnityEngine.Random.state = previousRandomState;
    }

    private static void ScatterGroundNature(Scene scene, Terrain terrain, Vector3 cherryTreePosition)
    {
        GameObject oldRoot = GameObject.Find(NatureRootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject[] grassPrefabs = CreateProjectNaturePrefabs(scene, NatureGrassSourcePaths);
        GameObject[] flowerPrefabs = CreateProjectNaturePrefabs(scene, NatureFlowerSourcePaths);
        GameObject[] rockPrefabs = CreateProjectNaturePrefabs(scene, NatureRockSourcePaths);

        GameObject natureRoot = new(NatureRootName);
        GameObject decorationRoot = GameObject.Find("05_Decoration");
        if (decorationRoot != null)
            natureRoot.transform.SetParent(decorationRoot.transform, false);

        Physics.SyncTransforms();
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(20260301);

        ScatterCategory(scene, terrain, natureRoot.transform, cherryTreePosition, "Grass", grassPrefabs, 180, 0.44f, 0.76f, 0.45f, false);
        ScatterCategory(scene, terrain, natureRoot.transform, cherryTreePosition, "Flowers", flowerPrefabs, 70, 0.52f, 0.90f, 0.52f, false);
        ScatterCategory(scene, terrain, natureRoot.transform, cherryTreePosition, "Rocks", rockPrefabs, 32, 0.14f, 0.32f, 0.48f, true);

        UnityEngine.Random.state = previousRandomState;
    }

    private static GameObject[] CreateProjectNaturePrefabs(Scene scene, string[] sourcePaths)
    {
        EnsureNaturePrefabFolder();
        GameObject[] projectPrefabs = new GameObject[sourcePaths.Length];

        for (int i = 0; i < sourcePaths.Length; i++)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePaths[i]);
            if (sourcePrefab == null)
                throw new InvalidOperationException("Missing nature source prefab: " + sourcePaths[i]);

            string targetPath = $"{NaturePrefabFolder}/{Path.GetFileName(sourcePaths[i])}";
            GameObject projectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (projectPrefab == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, scene);
                projectPrefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (projectPrefab == null)
                throw new InvalidOperationException("Could not create project nature prefab: " + targetPath);
            projectPrefabs[i] = projectPrefab;
        }

        AssetDatabase.SaveAssets();
        return projectPrefabs;
    }

    private static void EnsureNaturePrefabFolder()
    {
        const string environmentFolder = "Assets/Project/Prefabs/Environment";
        if (!AssetDatabase.IsValidFolder(environmentFolder))
            AssetDatabase.CreateFolder("Assets/Project/Prefabs", "Environment");
        if (!AssetDatabase.IsValidFolder(NaturePrefabFolder))
            AssetDatabase.CreateFolder(environmentFolder, "MarketNature");
    }

    private static void ScatterCategory(
        Scene scene,
        Terrain terrain,
        Transform natureRoot,
        Vector3 cherryTreePosition,
        string categoryName,
        GameObject[] prefabs,
        int count,
        float minScale,
        float maxScale,
        float clearance,
        bool isRock)
    {
        GameObject categoryRoot = new(categoryName);
        categoryRoot.transform.SetParent(natureRoot, false);

        int placed = 0;
        int attempts = 0;
        int roadRejected = 0;
        int centerRejected = 0;
        int slopeRejected = 0;
        int collisionRejected = 0;
        int maximumAttempts = count * 60;
        while (placed < count && attempts++ < maximumAttempts)
        {
            float x = UnityEngine.Random.Range(-57f, 57f);
            float z = UnityEngine.Random.Range(-39f, 39f);
            if (CalculateRoadWeight(x, z) > 0.08f)
            {
                roadRejected++;
                continue;
            }
            if (Vector2.Distance(new Vector2(x, z), new Vector2(cherryTreePosition.x, cherryTreePosition.z)) < 4.6f)
            {
                centerRejected++;
                continue;
            }

            Vector3 position = new(x, 0f, z);
            float normalizedX = Mathf.InverseLerp(terrain.transform.position.x, terrain.transform.position.x + terrain.terrainData.size.x, x);
            float normalizedZ = Mathf.InverseLerp(terrain.transform.position.z, terrain.transform.position.z + terrain.terrainData.size.z, z);
            if (terrain.terrainData.GetSteepness(normalizedX, normalizedZ) > 22f)
            {
                slopeRejected++;
                continue;
            }

            position.y = SampleGroundY(terrain, position) + (isRock ? -0.035f : 0.02f);
            if (IsPlacementBlocked(position, clearance, terrain, natureRoot))
            {
                collisionRejected++;
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefabs[UnityEngine.Random.Range(0, prefabs.Length)], scene);
            instance.name = $"{categoryName} {placed + 1:000}";
            instance.transform.SetParent(categoryRoot.transform, true);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(
                isRock ? 0f : UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(0f, 360f),
                isRock ? 0f : UnityEngine.Random.Range(-2f, 2f));

            float scale = UnityEngine.Random.Range(minScale, maxScale);
            instance.transform.localScale = new Vector3(
                scale * UnityEngine.Random.Range(0.86f, 1.14f),
                scale * UnityEngine.Random.Range(0.90f, 1.18f),
                scale * UnityEngine.Random.Range(0.86f, 1.14f));
            SnapRendererBottomToTerrain(instance, terrain, isRock ? 0.08f : 0.015f);
            placed++;
        }

        if (placed != count)
            Debug.LogWarning($"[MarketCherryTreeGrassPainter] Placed {placed}/{count} {categoryName}; rejected road={roadRejected}, center={centerRejected}, slope={slopeRejected}, collision={collisionRejected}.");
    }

    private static bool IsPlacementBlocked(Vector3 position, float clearance, Terrain terrain, Transform natureRoot)
    {
        Collider[] overlaps = Physics.OverlapSphere(position + Vector3.up * 0.35f, clearance);
        foreach (Collider overlap in overlaps)
        {
            if (overlap is TerrainCollider || overlap.transform.IsChildOf(natureRoot))
                continue;
            return true;
        }
        return false;
    }

    private static void RepositionInteractionMarker(Terrain terrain)
    {
        GameObject marker = GameObject.Find("Interaction - Cherry Blossom Tree");
        if (marker == null) return;

        Vector3 position = marker.transform.position;
        position.y = SampleGroundY(terrain, position) + 0.15f;
        marker.transform.position = position;
    }

    private static float SampleGroundY(Terrain terrain, Vector3 worldPosition)
    {
        return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
    }

    private static void SnapRendererBottomToTerrain(GameObject instance, Terrain terrain, float groundInset)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        float lowestRendererY = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
        {
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                lowestRendererY = Mathf.Min(lowestRendererY, renderer.bounds.min.y);
        }

        if (float.IsPositiveInfinity(lowestRendererY))
            return;

        float groundY = SampleGroundY(terrain, instance.transform.position);
        instance.transform.position += Vector3.up * (groundY - groundInset - lowestRendererY);
    }
}
