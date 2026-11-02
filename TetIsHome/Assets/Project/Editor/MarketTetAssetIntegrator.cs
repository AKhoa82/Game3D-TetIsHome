using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketTetAssetIntegrator
{
    // Import request entry point shared by editor refreshes and map rebuilds.
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string LandmarkFolder = "Assets/Project/Prefabs/Market/Landmarks";
    private const string OutputFolder = LandmarkFolder + "/TetMarketAssets";
    private const string RequestFileName = "MarketTetAssetIntegrator.request";

    private sealed class AssetDefinition
    {
        public string Key;
        public string SourceFolder;
        public string SourceName;
        public string DisplayName;
        public float TargetHeight;
        public float Smoothness;
        public float GroundPercentile;
    }

    private sealed class Placement
    {
        public string AssetKey;
        public string Name;
        public Vector3 Position;
        public float RotationY;
        public string ReplacedName;
        public string ParentName;
        public string PreviousInstanceName;
        public float VerticalOffset;

        public Placement(string assetKey, string name, Vector3 position, float rotationY, string replacedName = null, string parentName = null, string previousInstanceName = null, float verticalOffset = 0f)
        {
            AssetKey = assetKey;
            Name = name;
            Position = position;
            RotationY = rotationY;
            ReplacedName = replacedName;
            ParentName = parentName;
            PreviousInstanceName = previousInstanceName;
            VerticalOffset = verticalOffset;
        }
    }

    private static readonly AssetDefinition[] AssetDefinitions =
    {
        new AssetDefinition
        {
            Key = "Chrysanthemum",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Golden_Chrysanthemum_Garden_20261031_texture_fbx",
            SourceName = "Meshy_AI_Golden_Chrysanthemum_Garden_20261031_texture",
            DisplayName = "Golden Chrysanthemum Garden",
            TargetHeight = 2.45f,
            Smoothness = 0.16f
        },
        new AssetDefinition
        {
            Key = "Kumquat",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Phuc_Loc_Tho_Kumquat_Bonsai_20261031_texture_fbx",
            SourceName = "Meshy_AI_Phuc_Loc_Tho_Kumquat_Bonsai_20261031_texture",
            DisplayName = "Phuc Loc Tho Kumquat Bonsai",
            TargetHeight = 4.6f,
            Smoothness = 0.19f
        },
        new AssetDefinition
        {
            Key = "UmbrellaStall",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Umbrella_Market_Stall_Concept_20261031_texture_fbx",
            SourceName = "Meshy_AI_Umbrella_Market_Stall_Concept_20261031_texture",
            DisplayName = "Umbrella Market Stall",
            TargetHeight = 7.1875f,
            Smoothness = 0.18f
        },
        new AssetDefinition
        {
            Key = "TetStall",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Vietnamese_Tet_Market_Stall_20261031_texture_fbx",
            SourceName = "Meshy_AI_Vietnamese_Tet_Market_Stall_20261031_texture",
            DisplayName = "Vietnamese Tet Market Stall",
            TargetHeight = 7.1875f,
            Smoothness = 0.18f
        },
        new AssetDefinition
        {
            Key = "FlowerMarketStall",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Flower_Market_Stall_20261031_texture_fbx",
            SourceName = "Meshy_AI_Flower_Market_Stall_20261031_texture",
            DisplayName = "Flower Market Stall",
            TargetHeight = 7.1875f,
            Smoothness = 0.17f
        },
        new AssetDefinition
        {
            Key = "FruitMarket",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Fruit_Market_Abundance_20261031_texture_fbx",
            SourceName = "Meshy_AI_Fruit_Market_Abundance_20261031_texture",
            DisplayName = "Fruit Market Abundance",
            TargetHeight = 7.1875f,
            Smoothness = 0.16f
        },
        new AssetDefinition
        {
            Key = "CalligraphyStall",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Rainy_Day_Calligraphy_Stall_20261101_texture_fbx",
            SourceName = "Meshy_AI_Rainy_Day_Calligraphy_Stall_20261101_texture",
            DisplayName = "Rainy Day Calligraphy Stall",
            TargetHeight = 7.1875f,
            Smoothness = 0.17f
        },
        new AssetDefinition
        {
            Key = "FestivalGate",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Chợ_Xuân_Festival_Gate_20261101_texture_fbx",
            SourceName = "Meshy_AI_Chợ_Xuân_Festival_Gate_20261101_texture",
            DisplayName = "Cho Xuan Festival Gate",
            TargetHeight = 11.5f,
            Smoothness = 0.18f
        },
        new AssetDefinition
        {
            Key = "VietnameseFruitStand",
            SourceFolder = LandmarkFolder + "/Meshy_AI_Vietnamese_Market_Fruit_Stand_20261101_texture_fbx",
            SourceName = "Meshy_AI_Vietnamese_Market_Fruit_Stand_20261101_texture",
            DisplayName = "Vietnamese Market Fruit Stand",
            TargetHeight = 7.1875f,
            Smoothness = 0.16f,
            GroundPercentile = 0.025f
        }
    };

    private static readonly Placement[] Placements =
    {
        new Placement("FestivalGate", "Tet Market Festival Gate - South Entrance", new Vector3(0f, 1.85f, -40.5f), 0f, "Main Gate - Cong Lang", "02_Paths_And_Plaza"),
        new Placement("CalligraphyStall", "Rainy Day Calligraphy Stall - South East", new Vector3(48f, 1.85f, -30f), -90f, "Calligraphy Stall"),
        new Placement("FruitMarket", "Fruit Stall - Fresh Tet Produce", new Vector3(51f, 1.85f, 8f), -90f, "Traditional Food Stall"),
        new Placement("VietnameseFruitStand", "Vietnamese Fruit Stand - South East", new Vector3(51f, 1.85f, -13f), -90f, "Traditional Food Stall", null, "Tet Food Court - Traditional Dishes"),
        new Placement("TetStall", "Worship Goods Stall - Tet Offerings", new Vector3(45f, 1.85f, 28f), 0f, "Tet Goods Stall"),
        new Placement("UmbrellaStall", "Tet Ingredients Stall - Banh Chung", new Vector3(28f, 1.85f, 34f), 0f, "Tet Goods Stall"),
        new Placement("FlowerMarketStall", "Flower Market Stall - West Zone", new Vector3(-16f, 1.85f, 38f), 180f, "NPC Quest Flower Stall"),
        new Placement("Kumquat", "Phuc Loc Tho Kumquat - East Market", new Vector3(53.5f, 1.85f, 16f), 18f),
        new Placement("Chrysanthemum", "Golden Chrysanthemum Display - North East", new Vector3(37f, 1.85f, 38.5f), -12f)
    };

    [InitializeOnLoadMethod]
    private static void BuildWhenRequested()
    {
        string requestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + RequestFileName));
        if (!File.Exists(requestPath)) return;

        File.Delete(requestPath);
        EditorApplication.delayCall += () =>
        {
            try { BuildAndPlace(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/Tet Is Home/Integrate New Tet Market Assets")]
    public static void BuildAndPlace()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder(OutputFolder);

        Dictionary<string, GameObject> prefabs = new();
        foreach (AssetDefinition definition in AssetDefinitions)
        {
            ConfigureTextureImporters(definition);
            Material material = CreateOrUpdateMaterial(definition);
            prefabs[definition.Key] = CreateOrUpdatePrefab(definition, material);
        }

        PlaceInMarket(prefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MarketTetAssetIntegrator] Built and placed the imported Tet market landmark assets.");
    }

    [MenuItem("Tools/Tet Is Home/Capture Tet Market Asset Review")]
    public static void CaptureReviewImages()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        CaptureReviewImage(
            "MarketTetAssets_EastReview.png",
            new Vector3(25f, 12f, -7f),
            new Vector3(49f, 3.5f, -1f));
        CaptureReviewImage(
            "MarketTetAssets_NorthEastReview.png",
            new Vector3(17f, 13f, 17f),
            new Vector3(39f, 3.7f, 33f));
        CaptureReviewImage(
            "MarketTetAssets_WestReview.png",
            new Vector3(-39f, 13f, 17f),
            new Vector3(-16f, 3.7f, 35f));
        CaptureReviewImage(
            "MarketTetAssets_SouthGateReview.png",
            new Vector3(0f, 12f, -68f),
            new Vector3(0f, 5.2f, -40.5f));
        Debug.Log("[MarketTetAssetIntegrator] Captured four Tet market review images.");
    }

    private static void CaptureReviewImage(string fileName, Vector3 position, Vector3 target)
    {
        GameObject cameraObject = new("Tet Market Review Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
        camera.fieldOfView = 48f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 180f;
        camera.clearFlags = CameraClearFlags.Skybox;

        RenderTexture renderTexture = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;

        Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + fileName));
        File.WriteAllBytes(outputPath, image.EncodeToPNG());

        camera.targetTexture = null;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    private static void ConfigureTextureImporters(AssetDefinition definition)
    {
        ConfigureTexture(definition.SourceFolder + "/" + definition.SourceName + "_normal.png", true, false);
        ConfigureTexture(definition.SourceFolder + "/" + definition.SourceName + "_metallic.png", false, true);
        ConfigureTexture(definition.SourceFolder + "/" + definition.SourceName + "_roughness.png", false, true);
    }

    private static void ConfigureTexture(string path, bool normalMap, bool linear)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Missing texture importer: " + path);

        bool changed = false;
        TextureImporterType desiredType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != desiredType)
        {
            importer.textureType = desiredType;
            changed = true;
        }
        if (importer.sRGBTexture == linear)
        {
            importer.sRGBTexture = !linear;
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(AssetDefinition definition)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("The URP Lit shader is unavailable.");

        string materialPath = OutputFolder + "/M_" + definition.Key + "_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_" + definition.Key + "_URP" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D baseMap = LoadTexture(definition.SourceFolder + "/" + definition.SourceName + ".png");
        Texture2D normalMap = LoadTexture(definition.SourceFolder + "/" + definition.SourceName + "_normal.png");
        Texture2D metallicMap = LoadTexture(definition.SourceFolder + "/" + definition.SourceName + "_metallic.png");

        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_MainTex", baseMap);
        material.SetTexture("_BumpMap", normalMap);
        material.SetTexture("_MetallicGlossMap", metallicMap);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", definition.Smoothness);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException("Missing imported Tet asset texture: " + path);
        return texture;
    }

    private static GameObject CreateOrUpdatePrefab(AssetDefinition definition, Material material)
    {
        string modelPath = definition.SourceFolder + "/" + definition.SourceName + ".fbx";
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
            throw new InvalidOperationException("Missing imported Tet asset FBX: " + modelPath);

        GameObject root = new(definition.DisplayName);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.name = definition.DisplayName + " Model";
        model.transform.SetParent(root.transform, true);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(definition.DisplayName + " has no renderers.");

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y <= 0.001f)
            throw new InvalidOperationException("Unity could not measure " + definition.DisplayName + ".");

        model.transform.localScale *= definition.TargetHeight / bounds.size.y;
        bounds = CalculateBounds(renderers);
        model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        if (definition.GroundPercentile > 0f)
        {
            float visibleBottom = CalculateVertexHeightPercentile(model, definition.GroundPercentile);
            model.transform.position += Vector3.down * visibleBottom;
            Debug.Log($"[MarketTetAssetIntegrator] Grounded {definition.DisplayName} using vertex percentile; correction={visibleBottom:F3} m.");
        }
        bounds = CalculateBounds(renderers);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = new Vector3(bounds.size.x * 0.9f, bounds.size.y * 0.9f, bounds.size.z * 0.9f);

        string prefabPath = OutputFolder + "/" + definition.Key + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (saved == null)
            throw new InvalidOperationException("Unity could not save prefab: " + prefabPath);
        return saved;
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static float CalculateVertexHeightPercentile(GameObject model, float percentile)
    {
        List<float> heights = new();
        foreach (MeshFilter meshFilter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) continue;
            foreach (Vector3 vertex in mesh.vertices)
                heights.Add(meshFilter.transform.TransformPoint(vertex).y);
        }

        if (heights.Count == 0) return 0f;
        heights.Sort();
        int index = Mathf.Clamp(Mathf.RoundToInt((heights.Count - 1) * percentile), 0, heights.Count - 1);
        return Mathf.Max(0f, heights[index]);
    }

    private static void PlaceInMarket(Dictionary<string, GameObject> prefabs)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the currently open scene before integrating the Tet market assets.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject vendorsRoot = GameObject.Find("04_Market_Vendors");
        if (vendorsRoot == null)
            throw new InvalidOperationException("The market scene is missing its vendor root.");

        Transform zone = vendorsRoot.transform.Find("Food And Traditional Goods - East Zone");
        if (zone == null) zone = vendorsRoot.transform;

        foreach (Placement placement in Placements)
        {
            RemovePreviousInstance(scene, placement.Name);
            if (!string.IsNullOrEmpty(placement.PreviousInstanceName))
                RemovePreviousInstance(scene, placement.PreviousInstanceName);
            if (!string.IsNullOrEmpty(placement.ReplacedName))
                RemoveClosestPlaceholder(scene, placement.ReplacedName, placement.Position);
            if (placement.AssetKey == "FestivalGate")
                RemoveOldGateDecor(scene, placement.Position);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[placement.AssetKey], scene);
            instance.name = placement.Name;
            Transform placementParent = zone;
            if (!string.IsNullOrEmpty(placement.ParentName))
            {
                GameObject requestedParent = GameObject.Find(placement.ParentName);
                if (requestedParent != null) placementParent = requestedParent.transform;
            }
            instance.transform.SetParent(placementParent, true);

            Vector3 position = placement.Position;
            position.y = GroundHeightAt(position) + 0.02f + placement.VerticalOffset;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, placement.RotationY, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the market scene after integrating Tet assets.");
    }

    private static float GroundHeightAt(Vector3 position)
    {
        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        return terrain == null
            ? position.y
            : terrain.SampleHeight(position) + terrain.transform.position.y;
    }

    private static void RemovePreviousInstance(Scene scene, string instanceName)
    {
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.scene == scene && gameObject.name == instanceName)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                return;
            }
        }
    }

    private static void RemoveClosestPlaceholder(Scene scene, string placeholderName, Vector3 position)
    {
        GameObject closest = null;
        float closestDistance = 4f;
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.scene != scene || gameObject.name != placeholderName) continue;
            float distance = Vector2.Distance(
                new Vector2(gameObject.transform.position.x, gameObject.transform.position.z),
                new Vector2(position.x, position.z));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = gameObject;
            }
        }
        if (closest != null) UnityEngine.Object.DestroyImmediate(closest);
    }

    private static void RemoveOldGateDecor(Scene scene, Vector3 position)
    {
        string[] oldGateNames = { "Red Gate Pillar", "Gate Lantern", "Gate Left", "Gate Right", "Gate Beam", "Gate Sign" };
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.scene != scene || Array.IndexOf(oldGateNames, gameObject.name) < 0) continue;
            float distance = Vector2.Distance(
                new Vector2(gameObject.transform.position.x, gameObject.transform.position.z),
                new Vector2(position.x, position.z));
            if (distance <= 10f) UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
