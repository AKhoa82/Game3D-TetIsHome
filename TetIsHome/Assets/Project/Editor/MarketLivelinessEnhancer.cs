using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketLivelinessEnhancer
{
    // The pass is idempotent: rebuilding replaces only its own generated hierarchy.
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string MaterialFolder = "Assets/Project/Scenes/04_Market/Generated";
    private const float GroundY = 1.85f;
    private const string RootName = "08_Market_Liveliness";
    private const string RequestFileName = "MarketLivelinessEnhancer.request";

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

    [MenuItem("Tools/Tet Is Home/Make Market More Lively")]
    public static void BuildAndPlace()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject mapRoot = GameObject.Find("CHO_HOA_TET__GENERATED");
        if (mapRoot == null)
            throw new InvalidOperationException("The generated market root was not found.");

        Transform oldRoot = mapRoot.transform.Find(RootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);

        GameObject root = new(RootName);
        root.transform.SetParent(mapRoot.transform, false);
        Transform props = Child(root.transform, "Market Goods - Baskets And Crates");
        Transform bunting = Child(root.transform, "Tet Bunting - Red And Gold");

        CreateGoodsClusters(props);
        CreateBunting(bunting);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[MarketLivelinessEnhancer] Added market goods clusters and Tet bunting without generated visitors.");
    }

    public static void BuildAndCaptureReview()
    {
        BuildAndPlace();
        MarketTetAssetIntegrator.CaptureReviewImages();
    }

    private static void CreateGoodsClusters(Transform parent)
    {
        Vector3[] positions =
        {
            new(-42f, GroundY, -23f), new(-48f, GroundY, 14f), new(-34f, GroundY, 30f),
            new( 42f, GroundY, -23f), new( 48f, GroundY, 14f), new( 34f, GroundY, 30f),
            new(-20f, GroundY, 35f), new( 20f, GroundY, 35f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Transform cluster = Child(parent, $"Goods Cluster {i + 1:00}");
            cluster.position = positions[i];
            cluster.rotation = Quaternion.Euler(0f, i * 37f, 0f);
            Primitive("Wooden Crate", PrimitiveType.Cube, cluster, new Vector3(0f, .45f, 0f),
                new Vector3(1.15f, .9f, 1.15f), "Wood");
            Primitive("Basket A", PrimitiveType.Cylinder, cluster, new Vector3(.9f, .30f, .25f),
                new Vector3(.55f, .30f, .55f), "WoodLight");
            Primitive("Basket B", PrimitiveType.Cylinder, cluster, new Vector3(-.65f, .26f, .75f),
                new Vector3(.48f, .26f, .48f), "WoodLight");
            Primitive("Tet Goods A", PrimitiveType.Sphere, cluster, new Vector3(.9f, .68f, .25f),
                new Vector3(.42f, .24f, .42f), i % 2 == 0 ? "Yellow" : "Green");
            Primitive("Tet Goods B", PrimitiveType.Sphere, cluster, new Vector3(-.65f, .60f, .75f),
                new Vector3(.38f, .22f, .38f), i % 2 == 0 ? "Pink" : "Red");
        }
    }

    private static void CreateBunting(Transform parent)
    {
        CreateBuntingLine(parent, new Vector3(-53f, 7.0f, -20f), new Vector3(-26f, 7.0f, -20f), 12, "West");
        CreateBuntingLine(parent, new Vector3( 26f, 7.0f, -20f), new Vector3( 53f, 7.0f, -20f), 12, "East");
        CreateBuntingLine(parent, new Vector3(-24f, 7.4f,  35f), new Vector3( 24f, 7.4f,  35f), 18, "North");
    }

    private static void CreateBuntingLine(Transform parent, Vector3 start, Vector3 end, int count, string label)
    {
        Transform line = Child(parent, $"{label} Bunting Line");
        Vector3 direction = end - start;
        float length = direction.magnitude;
        Vector3 midpoint = (start + end) * .5f;
        GameObject rope = Primitive("Rope", PrimitiveType.Cube, line, midpoint,
            new Vector3(.045f, .045f, length), "Gold", true);
        rope.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        for (int i = 0; i < count; i++)
        {
            float t = (i + .5f) / count;
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y -= Mathf.Sin(t * Mathf.PI) * .35f;
            GameObject flag = Primitive("Tet Flag", PrimitiveType.Cube, line, p,
                new Vector3(.48f, .62f, .08f), i % 2 == 0 ? "Red" : "Gold", true);
            flag.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
        Vector3 scale, string materialName, bool worldPosition = false)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        if (worldPosition) gameObject.transform.position = position;
        else gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/M_{materialName}.mat");
        if (material == null) throw new InvalidOperationException("Missing market material: " + materialName);
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }
}
