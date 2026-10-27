using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MarketMapBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string GeneratedFolder = "Assets/Project/Scenes/04_Market/Generated";
    private const float GroundY = 1.85f;

    private static readonly Dictionary<string, Material> Materials = new();

    [InitializeOnLoadMethod]
    private static void BuildWhenRequested()
    {
        string requestPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../MarketMapBuilder.request"));
        if (!System.IO.File.Exists(requestPath)) return;
        System.IO.File.Delete(requestPath);
        EditorApplication.delayCall += () =>
        {
            try { BuildMarket(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/Tet Is Home/Build Tet Flower Market")]
    public static void BuildFromMenu() => BuildMarket();

    public static void BuildMarket()
    {
        EnsureFolder("Assets/Project/Scenes/04_Market", "Generated");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupMaterials();
        SetupLighting();

        GameObject root = new("CHO_HOA_TET__GENERATED");
        GameObject terrainRoot = Child(root, "01_Terrain_And_Water");
        GameObject pathsRoot = Child(root, "02_Paths_And_Plaza");
        GameObject centerRoot = Child(root, "03_Central_Landmark");
        GameObject vendorsRoot = Child(root, "04_Market_Vendors");
        GameObject decorRoot = Child(root, "05_Decoration");
        GameObject hazardRoot = Child(root, "06_Enemy_Event_Area");
        GameObject markersRoot = Child(root, "07_Gameplay_Markers");

        CreateTerrain(terrainRoot.transform);
        CreateMoat(terrainRoot.transform);
        CreatePathsAndPlaza(pathsRoot.transform);
        CreateGateAndBridges(pathsRoot.transform);
        CreateCentralLandmark(centerRoot.transform);
        CreateVendorZones(vendorsRoot.transform);
        CreatePerimeterDecor(decorRoot.transform);
        CreateHazardArea(hazardRoot.transform);
        CreateGameplayMarkers(markersRoot.transform);
        SetupCamera(root.transform);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MarketMapBuilder] Tet Flower Market generated successfully: " + ScenePath);
    }

    private static void SetupMaterials()
    {
        Materials.Clear();
        AddMaterial("Grass", new Color(0.29f, 0.50f, 0.22f), 0.05f);
        AddMaterial("Stone", new Color(0.54f, 0.55f, 0.50f), 0.08f);
        AddMaterial("StoneLight", new Color(0.72f, 0.70f, 0.60f), 0.05f);
        AddMaterial("Soil", new Color(0.48f, 0.31f, 0.17f), 0.02f);
        AddMaterial("Sand", new Color(0.80f, 0.69f, 0.47f), 0.02f);
        AddMaterial("Water", new Color(0.12f, 0.55f, 0.68f), 0.72f, 0.08f);
        AddMaterial("Wood", new Color(0.34f, 0.16f, 0.07f), 0.05f);
        AddMaterial("WoodLight", new Color(0.57f, 0.32f, 0.13f), 0.04f);
        AddMaterial("Red", new Color(0.78f, 0.055f, 0.035f), 0.15f);
        AddMaterial("DarkRed", new Color(0.38f, 0.025f, 0.018f), 0.08f);
        AddMaterial("Gold", new Color(1.0f, 0.64f, 0.06f), 0.55f, 0.18f);
        AddMaterial("Yellow", new Color(1.0f, 0.82f, 0.08f), 0.12f);
        AddMaterial("Pink", new Color(0.95f, 0.35f, 0.57f), 0.12f);
        AddMaterial("Green", new Color(0.18f, 0.42f, 0.12f), 0.06f);
        AddMaterial("Dark", new Color(0.11f, 0.09f, 0.075f), 0.0f);
        AddMaterial("White", new Color(0.94f, 0.90f, 0.77f), 0.08f);
    }

    private static void AddMaterial(string name, Color color, float smoothness, float metallic = 0f)
    {
        string path = $"{GeneratedFolder}/M_{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = "M_" + name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
        Materials[name] = material;
    }

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.67f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.48f, 0.40f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.15f, 0.11f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.69f, 0.80f, 0.83f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 105f;
        RenderSettings.fogEndDistance = 230f;

        GameObject sun = new("Sun - Warm Tet Afternoon");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.84f, 0.62f);
        light.intensity = 1.35f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(43f, -32f, 0f);

        GameObject fill = new("Sky Fill Light");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.38f, 0.52f, 0.78f);
        fillLight.intensity = 0.22f;
        fillLight.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(55f, 145f, 0f);
    }

    private static void CreateTerrain(Transform parent)
    {
        string dataPath = $"{GeneratedFolder}/MarketTerrainData.asset";
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.heightmapResolution = 257;
        data.size = new Vector3(180f, 32f, 140f);
        float[,] heights = new float[257, 257];
        for (int z = 0; z < 257; z++)
        {
            for (int x = 0; x < 257; x++)
            {
                float wx = x / 256f * 180f - 90f;
                float wz = z / 256f * 140f - 70f;
                float ax = Mathf.Abs(wx);
                float az = Mathf.Abs(wz);
                float h = GroundY;
                bool moat = (ax > 62f && ax < 70f && az < 52f) || (az > 44f && az < 52f && ax < 70f);
                if (moat) h = 0.28f;
                else if (ax > 71f || az > 53f)
                {
                    float edge = Mathf.Clamp01(Mathf.Max((ax - 70f) / 20f, (az - 52f) / 18f));
                    h = GroundY + edge * 2.1f + (Mathf.PerlinNoise(wx * 0.047f + 8f, wz * 0.047f + 3f) - 0.5f) * 1.15f;
                }
                heights[z, x] = Mathf.Clamp01(h / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);

        string texPath = $"{GeneratedFolder}/T_Grass.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (texture == null)
        {
            texture = new Texture2D(4, 4, TextureFormat.RGBA32, true) { name = "T_Grass" };
            Color grass = new(0.27f, 0.47f, 0.19f, 1f);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = grass * UnityEngine.Random.Range(0.88f, 1.12f);
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texPath);
        }
        string layerPath = $"{GeneratedFolder}/MarketGrass.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer { name = "Market Grass" };
            AssetDatabase.CreateAsset(layer, layerPath);
        }
        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(9f, 9f);
        data.terrainLayers = new[] { layer };

        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Terrain - Tet Flower Market";
        terrainObject.transform.SetParent(parent);
        terrainObject.transform.position = new Vector3(-90f, 0f, -70f);
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 300f;
        terrain.detailObjectDistance = 90f;
    }

    private static void CreateMoat(Transform parent)
    {
        Transform water = Child(parent.gameObject, "Moat Water").transform;
        Box("Water North", water, new Vector3(0, 0.52f, 48f), new Vector3(132f, 0.28f, 7f), "Water");
        Box("Water South", water, new Vector3(0, 0.52f, -48f), new Vector3(132f, 0.28f, 7f), "Water");
        Box("Water West", water, new Vector3(-66f, 0.52f, 0), new Vector3(7f, 0.28f, 90f), "Water");
        Box("Water East", water, new Vector3(66f, 0.52f, 0), new Vector3(7f, 0.28f, 90f), "Water");
        foreach (Vector3 corner in new[] { new Vector3(-66, .52f, -48), new Vector3(66, .52f, -48), new Vector3(-66, .52f, 48), new Vector3(66, .52f, 48) })
            Cylinder("Water Corner", water, corner, new Vector3(7f, .14f, 7f), "Water", 32);

        Transform banks = Child(parent.gameObject, "Stone Moat Banks").transform;
        CreateRectBorder(banks, 61.8f, 43.8f, .8f, GroundY + .02f, "StoneLight");
        CreateRectBorder(banks, 70.2f, 52.2f, .8f, GroundY + .08f, "Stone");
    }

    private static void CreatePathsAndPlaza(Transform parent)
    {
        Box("Main South Path", parent, new Vector3(0, GroundY + .09f, -27f), new Vector3(8f, .18f, 35f), "Stone");
        Box("North Path", parent, new Vector3(0, GroundY + .09f, 33f), new Vector3(7f, .18f, 20f), "Stone");
        Box("East Path", parent, new Vector3(40f, GroundY + .09f, 0), new Vector3(36f, .18f, 7f), "Stone");
        Box("West Path", parent, new Vector3(-40f, GroundY + .09f, 0), new Vector3(36f, .18f, 7f), "Stone");

        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48f;
            Vector3 pos = new(Mathf.Sin(angle) * 25.2f, GroundY + .10f, Mathf.Cos(angle) * 25.2f);
            GameObject segment = Box("Circular Path", parent, pos, new Vector3(4.0f, .18f, 3.3f), "StoneLight");
            segment.transform.rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
        }

        Cylinder("Central Plaza", parent, new Vector3(0, GroundY + .06f, 0), new Vector3(22.2f, .10f, 22.2f), "Sand", 64);
        Cylinder("Central Garden", parent, new Vector3(0, GroundY + .18f, 0), new Vector3(16.8f, .20f, 16.8f), "Soil", 64);
        Box("Approach Road", parent, new Vector3(0, GroundY + .08f, -61f), new Vector3(9f, .16f, 18f), "Stone");
    }

    private static void CreateGateAndBridges(Transform parent)
    {
        Box("South Bridge", parent, new Vector3(0, 2.0f, -48f), new Vector3(10f, .45f, 12f), "WoodLight");
        for (int i = -4; i <= 4; i++)
            Box("Bridge Plank", parent, new Vector3(0, 2.25f, -48f + i * 1.25f), new Vector3(10.4f, .15f, 1.05f), i % 2 == 0 ? "WoodLight" : "Wood");
        Box("North Bridge", parent, new Vector3(0, 1.95f, 48f), new Vector3(7f, .35f, 11f), "WoodLight");

        GameObject gate = InstantiatePrefab("Assets/FantasyEnvironments/Environments/Town/Prefabs/TownGate.prefab", parent, new Vector3(0, GroundY, -40.5f), Quaternion.identity, "Main Gate - Cong Lang", 9.5f);
        if (gate == null) CreateCustomGate(parent);

        for (int side = -1; side <= 1; side += 2)
        {
            Box("Red Gate Pillar", parent, new Vector3(side * 6.0f, 5.1f, -40.8f), new Vector3(1.0f, 6.4f, 1.0f), "Red");
            Cylinder("Gate Lantern", parent, new Vector3(side * 6.0f, 8.6f, -40.8f), new Vector3(.65f, .75f, .65f), "Gold", 16);
        }
    }

    private static void CreateCustomGate(Transform parent)
    {
        Box("Gate Left", parent, new Vector3(-5f, 4.5f, -40f), new Vector3(2f, 7f, 2f), "StoneLight");
        Box("Gate Right", parent, new Vector3(5f, 4.5f, -40f), new Vector3(2f, 7f, 2f), "StoneLight");
        Box("Gate Beam", parent, new Vector3(0, 8f, -40f), new Vector3(12f, 1.5f, 2f), "Red");
        Box("Gate Sign", parent, new Vector3(0, 8f, -38.85f), new Vector3(5f, 1.05f, .25f), "Gold");
    }

    private static void CreateCentralLandmark(Transform parent)
    {
        Cylinder("Tree Planter Outer", parent, new Vector3(0, GroundY + .48f, 0), new Vector3(5.2f, .52f, 5.2f), "StoneLight", 48);
        Cylinder("Tree Planter Soil", parent, new Vector3(0, GroundY + 1.02f, 0), new Vector3(4.3f, .10f, 4.3f), "Soil", 48);
        GameObject tree = InstantiatePrefab("Assets/Project/Prefabs/Market/Landmarks/CherryBlossomTree.prefab", parent, new Vector3(0, GroundY + 1.05f, 0), Quaternion.Euler(0, 22f, 0), "Central Cherry Blossom Tree", 14f);
        if (tree == null)
        {
            Cylinder("Tree Trunk", parent, new Vector3(0, 6.2f, 0), new Vector3(1.0f, 4.3f, 1.0f), "Wood", 16);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                Sphere("Golden Canopy", parent, new Vector3(Mathf.Sin(a) * 3.6f, 11f + (i % 3), Mathf.Cos(a) * 3.6f), new Vector3(4.5f, 3.2f, 4.5f), "Yellow");
            }
        }

        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            float r = i % 2 == 0 ? 11.9f : 14.2f;
            ColorFlowerPot(parent, new Vector3(Mathf.Sin(a) * r, GroundY + .35f, Mathf.Cos(a) * r), i % 3 == 0 ? "Pink" : "Yellow", 0.85f);
        }
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            InstantiatePrefab($"Assets/FantasyEnvironments/Environments/Prefabs/Flower{(i % 9) + 1}.prefab", parent,
                new Vector3(Mathf.Sin(a) * 7.0f, GroundY + 1.12f, Mathf.Cos(a) * 7.0f), Quaternion.Euler(0, i * 37f, 0), "Mai Garden Flower", 1.0f);
        }
    }

    private static void CreateVendorZones(Transform parent)
    {
        Transform flowers = Child(parent.gameObject, "Flower Vendors - West Zone").transform;
        Transform goods = Child(parent.gameObject, "Food And Traditional Goods - East Zone").transform;
        Transform north = Child(parent.gameObject, "Quest Vendors - North Zone").transform;

        Vector3[] leftPositions =
        {
            new(-48, GroundY, -30), new(-34, GroundY, -34), new(-51, GroundY, -13),
            new(-51, GroundY, 8), new(-45, GroundY, 28), new(-28, GroundY, 34)
        };
        for (int i = 0; i < leftPositions.Length; i++)
        {
            float yaw = leftPositions[i].x < -45 ? 90f : 0f;
            Transform stall = CreateStall(flowers, "Flower Stall " + (i + 1).ToString("00"), leftPositions[i], yaw, i % 2 == 0 ? "Red" : "DarkRed");
            DecorateStallWithFlowers(stall, i);
        }

        Vector3[] rightPositions =
        {
            new(48, GroundY, -30), new(34, GroundY, -34), new(51, GroundY, -13),
            new(51, GroundY, 8), new(45, GroundY, 28), new(28, GroundY, 34)
        };
        for (int i = 0; i < rightPositions.Length; i++)
        {
            float yaw = rightPositions[i].x > 45 ? -90f : 0f;
            Transform stall = CreateStall(goods, i < 2 ? "Calligraphy Stall" : i < 4 ? "Traditional Food Stall" : "Tet Goods Stall", rightPositions[i], yaw, "Red");
            DecorateStallWithGoods(stall, i);
        }

        Transform northLeft = CreateStall(north, "NPC Quest Flower Stall", new Vector3(-16, GroundY, 38), 180f, "DarkRed");
        Transform northRight = CreateStall(north, "NPC Quest Souvenir Stall", new Vector3(16, GroundY, 38), 180f, "Red");
        DecorateStallWithFlowers(northLeft, 7);
        DecorateStallWithGoods(northRight, 7);
    }

    private static Transform CreateStall(Transform parent, string name, Vector3 position, float yaw, string canopyMaterial)
    {
        Transform stall = Child(parent.gameObject, name).transform;
        stall.position = position;
        stall.rotation = Quaternion.Euler(0, yaw, 0);
        Box("Platform", stall, new Vector3(0, .18f, 0), new Vector3(7.5f, .35f, 4.8f), "WoodLight", true);
        Box("Counter", stall, new Vector3(0, 1.25f, -1.45f), new Vector3(6.5f, 1.7f, 1.0f), "WoodLight", true);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Post", stall, new Vector3(side * 3.1f, 3.0f, side > 0 ? 1.6f : 1.6f), new Vector3(.28f, 5.5f, .28f), "Wood", true);
            Box("Front Post", stall, new Vector3(side * 3.1f, 3.0f, -1.6f), new Vector3(.28f, 5.5f, .28f), "Wood", true);
        }
        Box("Canopy", stall, new Vector3(0, 5.55f, 0), new Vector3(7.8f, .32f, 5.5f), canopyMaterial, true);
        for (int i = -3; i <= 3; i++)
            Box("Canopy Stripe", stall, new Vector3(i * 1.05f, 5.74f, 0), new Vector3(.40f, .08f, 5.4f), i % 2 == 0 ? "Gold" : canopyMaterial, true);
        Cylinder("Lantern", stall, new Vector3(-2.3f, 4.45f, -1.9f), new Vector3(.35f, .55f, .35f), "Red", 12, true);
        Cylinder("Lantern", stall, new Vector3(2.3f, 4.45f, -1.9f), new Vector3(.35f, .55f, .35f), "Red", 12, true);
        return stall;
    }

    private static void DecorateStallWithFlowers(Transform stall, int seed)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = -2.6f + i * .86f;
            ColorFlowerPot(stall, new Vector3(x, 2.15f, -1.45f), (i + seed) % 3 == 0 ? "Pink" : "Yellow", .48f, true);
        }
        for (int i = 0; i < 4; i++)
            ColorFlowerPot(stall, new Vector3(-2.4f + i * 1.6f, .58f, 1.45f), i % 2 == 0 ? "Yellow" : "Pink", .6f, true);
    }

    private static void DecorateStallWithGoods(Transform stall, int seed)
    {
        for (int i = 0; i < 5; i++)
        {
            Box("Tet Gift Box", stall, new Vector3(-2.2f + i * 1.1f, 2.15f, -1.45f), new Vector3(.75f, .65f + .12f * (i % 2), .75f), i % 2 == 0 ? "Red" : "Gold", true);
        }
        for (int i = 0; i < 3; i++)
            Cylinder("Food Basket", stall, new Vector3(-1.7f + i * 1.7f, .70f, 1.2f), new Vector3(.7f, .42f, .7f), "WoodLight", 12, true);
    }

    private static void CreatePerimeterDecor(Transform parent)
    {
        Vector3[] benches = { new(-28, GroundY, -23), new(28, GroundY, -23), new(-28, GroundY, 23), new(28, GroundY, 23) };
        foreach (Vector3 p in benches) CreateBench(parent, p, p.z > 0 ? 180f : 0f);

        for (int i = 0; i < 18; i++)
        {
            float t = i / 17f;
            float x = Mathf.Lerp(-55f, 55f, t);
            if (Mathf.Abs(x) < 8f) continue;
            ColorFlowerPot(parent, new Vector3(x, GroundY + .35f, -38f), i % 3 == 0 ? "Pink" : "Yellow", .72f);
        }

        Vector3[] trees =
        {
            new(-77, 2.0f, -58), new(77, 2.0f, -58), new(-79, 2.0f, 57), new(79, 2.0f, 57),
            new(-84, 2.2f, -18), new(84, 2.2f, 18), new(-82, 2.2f, 24), new(82, 2.2f, -24)
        };
        for (int i = 0; i < trees.Length; i++)
        {
            GameObject tree = InstantiatePrefab($"Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/{(i % 2 == 0 ? "Willow_tree1" : "Deciduous_tree2")}.prefab", parent, trees[i], Quaternion.Euler(0, i * 47f, 0), "Perimeter Tree", 8f + (i % 3));
            if (tree == null) CreateSimpleTree(parent, trees[i], i);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -34; z <= 34; z += 11)
                CreateLamp(parent, new Vector3(side * 58f, GroundY, z));
        }
        for (int x = -48; x <= 48; x += 12)
        {
            if (Mathf.Abs(x) < 8) continue;
            CreateLamp(parent, new Vector3(x, GroundY, 39f));
        }
    }

    private static void CreateBench(Transform parent, Vector3 pos, float yaw)
    {
        Transform bench = Child(parent.gameObject, "Rest Bench").transform;
        bench.position = pos;
        bench.rotation = Quaternion.Euler(0, yaw, 0);
        Box("Seat", bench, new Vector3(0, .85f, 0), new Vector3(4.2f, .32f, 1.2f), "WoodLight", true);
        Box("Back", bench, new Vector3(0, 1.65f, .45f), new Vector3(4.2f, 1.15f, .28f), "WoodLight", true);
        Box("Leg L", bench, new Vector3(-1.55f, .42f, 0), new Vector3(.35f, .85f, .75f), "Wood", true);
        Box("Leg R", bench, new Vector3(1.55f, .42f, 0), new Vector3(.35f, .85f, .75f), "Wood", true);
    }

    private static void CreateLamp(Transform parent, Vector3 pos)
    {
        Transform lamp = Child(parent.gameObject, "Tet Lantern Post").transform;
        lamp.position = pos;
        Cylinder("Pole", lamp, new Vector3(0, 2.3f, 0), new Vector3(.16f, 2.3f, .16f), "Wood", 10, true);
        Cylinder("Lantern", lamp, new Vector3(0, 4.8f, 0), new Vector3(.48f, .68f, .48f), "Red", 12, true);
        Sphere("Warm Glow", lamp, new Vector3(0, 4.8f, 0), Vector3.one * .18f, "Gold", true);
        GameObject lightObject = new("Lantern Light");
        lightObject.transform.SetParent(lamp, false);
        lightObject.transform.localPosition = new Vector3(0, 4.8f, 0);
        Light point = lightObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = new Color(1f, .36f, .08f);
        point.range = 6f;
        point.intensity = 2.0f;
        point.shadows = LightShadows.None;
    }

    private static void CreateSimpleTree(Transform parent, Vector3 pos, int variant)
    {
        Transform tree = Child(parent.gameObject, "Perimeter Tree").transform;
        tree.position = pos;
        float height = 6.5f + (variant % 3);
        Cylinder("Trunk", tree, new Vector3(0, height * .42f, 0), new Vector3(.45f, height * .42f, .45f), "Wood", 12, true);
        Sphere("Crown A", tree, new Vector3(0, height, 0), new Vector3(3.6f, 3.0f, 3.6f), "Green", true);
        Sphere("Crown B", tree, new Vector3(2.1f, height - .5f, .4f), new Vector3(2.4f, 2.1f, 2.4f), "Green", true);
        Sphere("Crown C", tree, new Vector3(-1.8f, height - .2f, -.7f), new Vector3(2.5f, 2.2f, 2.5f), "Green", true);
        for (int i = 0; i < 5; i++)
        {
            float angle = (i * 72f + variant * 19f) * Mathf.Deg2Rad;
            Sphere("Tet Blossom", tree, new Vector3(Mathf.Sin(angle) * 2.5f, height + (i % 2) * .8f, Mathf.Cos(angle) * 2.5f), Vector3.one * .45f, i % 2 == 0 ? "Yellow" : "Pink", true);
        }
    }

    private static void CreateHazardArea(Transform parent)
    {
        Box("Dark Event Ground", parent, new Vector3(47f, GroundY + .05f, 57.5f), new Vector3(34f, .12f, 16f), "Dark");
        for (int i = 0; i < 6; i++)
        {
            float x = 35f + (i % 3) * 7f;
            float z = 54f + (i / 3) * 7f;
            Transform crate = Child(parent.gameObject, "Abandoned Crate").transform;
            crate.position = new Vector3(x, GroundY + .8f, z);
            crate.rotation = Quaternion.Euler(UnityEngine.Random.Range(-8f, 8f), i * 29f, UnityEngine.Random.Range(-8f, 8f));
            Box("Crate", crate, Vector3.zero, new Vector3(2.3f, 1.7f, 2.3f), "Wood", true);
            Box("Band A", crate, new Vector3(0, 0, -1.18f), new Vector3(2.5f, .18f, .12f), "WoodLight", true);
        }
        for (int i = 0; i < 8; i++)
        {
            float x = 31f + i * 4.7f;
            Cylinder("Hazard Post", parent, new Vector3(x, 3.0f, 49.8f), new Vector3(.22f, 1.2f, .22f), "Red", 10);
            if (i < 7) Box("Hazard Rope", parent, new Vector3(x + 2.35f, 3.45f, 49.8f), new Vector3(4.6f, .10f, .10f), "Gold");
        }
        Cylinder("Enemy Event Marker", parent, new Vector3(47f, GroundY + .3f, 57.5f), new Vector3(2.2f, .2f, 2.2f), "Red", 32);
    }

    private static void CreateGameplayMarkers(Transform parent)
    {
        CreateMarker(parent, "Player Spawn - Main Entrance", new Vector3(0, GroundY + .15f, -56f), "Gold");
        CreateMarker(parent, "Interaction - Cherry Blossom Tree", new Vector3(0, GroundY + 1.2f, 5.4f), "Pink");
        CreateMarker(parent, "Quest NPC - Flower Vendor", new Vector3(-17f, GroundY + .15f, 32f), "Gold");
        CreateMarker(parent, "Quest NPC - Traditional Goods", new Vector3(17f, GroundY + .15f, 32f), "Gold");
        CreateMarker(parent, "Rest Save Point", new Vector3(28f, GroundY + .15f, -20f), "Green");
    }

    private static void CreateMarker(Transform parent, string name, Vector3 pos, string material)
    {
        GameObject marker = Cylinder(name, parent, pos, new Vector3(.75f, .08f, .75f), material, 24);
        marker.AddComponent<SphereCollider>().radius = .8f;
    }

    private static void SetupCamera(Transform parent)
    {
        GameObject cameraObject = new("Main Camera - Market Overview");
        cameraObject.transform.SetParent(parent);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 92f, -103f);
        cameraObject.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 450f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();

        GameObject topCamera = new("Design Camera - Top Down");
        topCamera.transform.SetParent(parent);
        topCamera.transform.position = new Vector3(0, 125f, 0);
        topCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Camera designCamera = topCamera.AddComponent<Camera>();
        designCamera.orthographic = true;
        designCamera.orthographicSize = 72f;
        designCamera.enabled = false;
    }

    private static void CreateRectBorder(Transform parent, float halfX, float halfZ, float width, float y, string material)
    {
        Box("Bank North", parent, new Vector3(0, y, halfZ), new Vector3(halfX * 2f, .32f, width), material);
        Box("Bank South", parent, new Vector3(0, y, -halfZ), new Vector3(halfX * 2f, .32f, width), material);
        Box("Bank East", parent, new Vector3(halfX, y, 0), new Vector3(width, .32f, halfZ * 2f), material);
        Box("Bank West", parent, new Vector3(-halfX, y, 0), new Vector3(width, .32f, halfZ * 2f), material);
    }

    private static void ColorFlowerPot(Transform parent, Vector3 position, string flowerMaterial, float scale, bool local = false)
    {
        Transform pot = Child(parent.gameObject, "Flower Pot").transform;
        if (local) pot.localPosition = position; else pot.position = position;
        pot.localScale = Vector3.one * scale;
        Cylinder("Clay Pot", pot, Vector3.zero, new Vector3(.48f, .45f, .48f), "WoodLight", 16, true);
        Cylinder("Flower Crown", pot, new Vector3(0, .78f, 0), new Vector3(.75f, .38f, .75f), flowerMaterial, 16, true);
        Cylinder("Leaves", pot, new Vector3(0, .56f, 0), new Vector3(.55f, .25f, .55f), "Green", 12, true);
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, Vector3 position, Quaternion rotation, string name, float targetHeight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return null;
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y > .001f)
            {
                float factor = targetHeight / bounds.size.y;
                instance.transform.localScale *= factor;
            }
        }
        return instance;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, string material, bool local = false)
        => Primitive(PrimitiveType.Cube, name, parent, position, scale, material, local);

    private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, string material, bool local = false)
        => Primitive(PrimitiveType.Sphere, name, parent, position, scale, material, local);

    private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, string material, int sides, bool local = false)
        => Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, local);

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string material, bool local)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        if (local) go.transform.localPosition = position; else go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && Materials.TryGetValue(material, out Material mat)) renderer.sharedMaterial = mat;
        return go;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folder);
    }
}
