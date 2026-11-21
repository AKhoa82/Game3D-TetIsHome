using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketGoldenBlossomBonsaiBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string SourceFolder = "Assets/Project/Prefabs/Market/Landmarks/Meshy_AI_Golden_Blossom_Bonsai_20261029_texture_fbx";
    private const string SourceName = "Meshy_AI_Golden_Blossom_Bonsai_20261029_texture";
    private const string OutputFolder = "Assets/Project/Prefabs/Market/Landmarks/GoldenBlossomBonsai";
    private const string MaterialPath = OutputFolder + "/M_GoldenBlossomBonsai_URP.mat";
    private const string PrefabPath = OutputFolder + "/GoldenBlossomBonsai.prefab";
    private const string InstanceName = "Golden Blossom Bonsai - Market";
    private const string RequestFileName = "MarketGoldenBlossomBonsaiBuilder.request";
    private const float TargetHeight = 5.8f;

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

    [MenuItem("Tools/Tet Is Home/Build and Place Golden Blossom Bonsai")]
    public static void BuildAndPlace()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder(OutputFolder);
        ConfigureTextureImporters();
        Material material = CreateOrUpdateMaterial();
        GameObject prefab = CreateOrUpdatePrefab(material);
        PlaceInMarket(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MarketGoldenBlossomBonsaiBuilder] Built the imported golden blossom bonsai and placed it beside the central cherry blossom tree.");
    }

    private static void ConfigureTextureImporters()
    {
        ConfigureTexture(SourceFolder + "/" + SourceName + "_normal.png", true, false);
        ConfigureTexture(SourceFolder + "/" + SourceName + "_metallic.png", false, true);
        ConfigureTexture(SourceFolder + "/" + SourceName + "_roughness.png", false, true);
    }

    private static void ConfigureTexture(string path, bool normalMap, bool linear)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

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

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("The URP Lit shader is unavailable.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_GoldenBlossomBonsai_URP" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D baseMap = LoadTexture(SourceFolder + "/" + SourceName + ".png");
        Texture2D normalMap = LoadTexture(SourceFolder + "/" + SourceName + "_normal.png");
        Texture2D metallicMap = LoadTexture(SourceFolder + "/" + SourceName + "_metallic.png");

        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_MainTex", baseMap);
        material.SetTexture("_BumpMap", normalMap);
        material.SetTexture("_MetallicGlossMap", metallicMap);
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.24f);
        material.SetColor("_BaseColor", Color.white);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException("Missing bonsai texture: " + path);
        return texture;
    }

    private static GameObject CreateOrUpdatePrefab(Material material)
    {
        string modelPath = SourceFolder + "/" + SourceName + ".fbx";
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
            throw new InvalidOperationException("Missing bonsai FBX: " + modelPath);

        GameObject root = new("Golden Blossom Bonsai");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.name = "Golden Blossom Bonsai Model";
        model.transform.SetParent(root.transform, true);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("The imported bonsai model has no renderers.");

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y <= 0.001f)
            throw new InvalidOperationException("Unity could not measure the imported bonsai model.");

        model.transform.localScale *= TargetHeight / bounds.size.y;
        bounds = CalculateBounds(renderers);
        model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.05f, 0f);
        collider.height = 2.1f;
        collider.radius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.16f, 0.48f, 0.9f);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (saved == null)
            throw new InvalidOperationException("Unity could not save the golden blossom bonsai prefab.");
        return saved;
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void PlaceInMarket(GameObject prefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the currently open scene before placing the golden blossom bonsai.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject centerRoot = GameObject.Find("03_Central_Landmark");
        if (centerRoot == null)
            throw new InvalidOperationException("The market scene is missing its central landmark root.");

        Transform existing = centerRoot.transform.Find(InstanceName);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        Vector3 position = new(-6.8f, 1.87f, 0f);
        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y + 0.02f;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = InstanceName;
        instance.transform.SetParent(centerRoot.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, 18f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the market scene after placing the golden blossom bonsai.");
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
