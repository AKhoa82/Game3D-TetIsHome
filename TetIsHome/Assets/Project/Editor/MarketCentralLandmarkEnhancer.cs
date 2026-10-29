using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketCentralLandmarkEnhancer
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string CenterRootName = "03_Central_Landmark";
    private const string TreeName = "Central Cherry Blossom Tree";
    private const string AccentRootName = "Low Flower Accents - Cherry Tree";
    private const float TargetTreeHeight = 16.8f;
    private const float CanopyWidthMultiplier = 1.0833334f;
    private const string RequestFileName = "MarketCentralLandmarkEnhancer.request";

    [InitializeOnLoadMethod]
    private static void ApplyWhenRequested()
    {
        string requestPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../" + RequestFileName));
        if (!System.IO.File.Exists(requestPath)) return;

        System.IO.File.Delete(requestPath);
        EditorApplication.delayCall += () =>
        {
            try { ApplyToMarketScene(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    private readonly struct FlowerAccent
    {
        public FlowerAccent(float x, float z, float height, float yaw, int variant)
        {
            X = x;
            Z = z;
            Height = height;
            Yaw = yaw;
            Variant = variant;
        }

        public float X { get; }
        public float Z { get; }
        public float Height { get; }
        public float Yaw { get; }
        public int Variant { get; }
    }

    // The positive-Z center is intentionally empty so the interaction marker keeps
    // a generous approach corridor. Uneven group sizes avoid a rigid flower ring.
    private static readonly FlowerAccent[] FlowerAccents =
    {
        new(-5.0f,  4.2f, 0.82f,  18f, 2),
        new(-6.1f,  3.3f, 1.05f,  71f, 5),
        new(-4.6f,  2.6f, 0.72f, 133f, 8),

        new( 5.2f,  4.0f, 0.90f, 204f, 4),
        new( 6.2f,  2.9f, 1.10f, 257f, 7),
        new( 4.7f,  2.3f, 0.68f, 319f, 1),
        new( 6.7f,  1.5f, 0.78f,  42f, 6),

        new(-4.4f, -3.7f, 0.92f,  94f, 3),
        new(-5.8f, -4.7f, 1.12f, 151f, 9),
        new(-4.9f, -5.8f, 0.74f, 218f, 6),
        new(-2.8f, -6.2f, 0.84f, 283f, 1),

        new( 0.1f, -6.6f, 1.00f, 337f, 5),
        new( 2.9f, -6.1f, 0.76f,  29f, 8),
        new( 4.7f, -4.9f, 1.08f, 112f, 2),
        new( 5.6f, -3.5f, 0.86f, 176f, 7)
    };

    [MenuItem("Tools/Tet Is Home/Enhance Central Cherry Blossom Landmark")]
    public static void ApplyToMarketScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the currently open scene before enhancing the market landmark.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        GameObject centerRoot = GameObject.Find(CenterRootName);
        GameObject tree = GameObject.Find(TreeName);

        if (centerRoot == null || tree == null)
            throw new InvalidOperationException("The market scene is missing its central landmark or cherry blossom tree.");

        ResizeTree(tree);
        CreateLowFlowerClusters(centerRoot.transform, new Vector3(tree.transform.position.x, 2.97f, tree.transform.position.z));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the enhanced market scene.");

        AssetDatabase.SaveAssets();
        Debug.Log("[MarketCentralLandmarkEnhancer] Enlarged the central canopy and added asymmetric low flower accents.");
    }

    public static void CreateLowFlowerClusters(Transform parent, Vector3 center)
    {
        Transform existing = parent.Find(AccentRootName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject accentRoot = new(AccentRootName);
        accentRoot.transform.SetParent(parent, true);

        Scene scene = parent.gameObject.scene;
        foreach (FlowerAccent accent in FlowerAccents)
        {
            string path = $"Assets/FantasyEnvironments/Environments/Prefabs/Flower{accent.Variant}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new InvalidOperationException("Missing flower prefab: " + path);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Low Flower Accent";
            instance.transform.SetParent(accentRoot.transform, true);
            instance.transform.position = center + new Vector3(accent.X, 0f, accent.Z);
            instance.transform.rotation = Quaternion.Euler(0f, accent.Yaw, 0f);
            FitHeight(instance, accent.Height);
        }
    }

    private static void ResizeTree(GameObject tree)
    {
        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            throw new InvalidOperationException("The central cherry blossom tree has no renderers.");

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float currentWorldScaleY = Mathf.Abs(tree.transform.lossyScale.y);
        if (bounds.size.y <= 0.001f || currentWorldScaleY <= 0.001f)
            throw new InvalidOperationException("Unity could not measure the central cherry blossom tree.");

        float unscaledHeight = bounds.size.y / currentWorldScaleY;
        float targetScale = TargetTreeHeight / unscaledHeight;
        tree.transform.localScale = new Vector3(
            targetScale * CanopyWidthMultiplier,
            targetScale,
            targetScale * CanopyWidthMultiplier);
    }

    private static void FitHeight(GameObject instance, float targetHeight)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y > 0.001f)
            instance.transform.localScale *= targetHeight / bounds.size.y;
    }
}
