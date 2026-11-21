using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketYellowMaiTreeBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string OutputFolder = "Assets/Project/Prefabs/Market/Landmarks/YellowMaiTree";
    private const string MaterialPath = OutputFolder + "/M_YellowMaiBlossoms.mat";
    private const string PrefabPath = OutputFolder + "/PottedYellowMaiTree.prefab";
    private const string TreeSourcePath = "Assets/Project/Prefabs/Market/Landmarks/CherryBlossomTree.prefab";
    private const string PlanterSourcePath = "Assets/Project/Prefabs/Market/Landmarks/PottedCherryBlossom/PottedCherryBlossom.prefab";
    private const string BlossomMaterialSourcePath = "Assets/Project/Prefabs/Market/Landmarks/Materials/CherryBlossom_Flowers.mat";
    private const string InstanceName = "Potted Yellow Mai Tree - Market";
    private const string RequestFileName = "MarketYellowMaiTreeBuilder.request";
    private const string RemoveRequestFileName = "MarketYellowMaiTreeBuilder.remove.request";
    private const float MaiTreeHeight = 6.5f;

    [InitializeOnLoadMethod]
    private static void BuildWhenRequested()
    {
        string requestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + RequestFileName));
        string removeRequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + RemoveRequestFileName));
        if (File.Exists(removeRequestPath))
        {
            File.Delete(removeRequestPath);
            EditorApplication.delayCall += () =>
            {
                try { RemoveFromMarket(); }
                catch (Exception exception) { Debug.LogException(exception); }
            };
            return;
        }

        if (File.Exists(requestPath))
        {
            File.Delete(requestPath);
            EditorApplication.delayCall += () =>
            {
                try { BuildAndPlace(); }
                catch (Exception exception) { Debug.LogException(exception); }
            };
        }
    }

    [MenuItem("Tools/Tet Is Home/Build and Place Yellow Mai Tree")]
    public static void BuildAndPlace()
    {
        EnsureFolder(OutputFolder);
        Material yellowBlossoms = CreateYellowBlossomMaterial();
        GameObject prefab = CreatePottedMaiPrefab(yellowBlossoms);
        PlaceInMarket(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MarketYellowMaiTreeBuilder] Built a potted yellow mai tree and placed it beside the central cherry blossom landmark.");
    }

    [MenuItem("Tools/Tet Is Home/Remove Yellow Mai Tree From Market")]
    public static void RemoveFromMarket()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the currently open scene before removing the yellow mai tree.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject centerRoot = GameObject.Find("03_Central_Landmark");
        Transform instance = centerRoot != null ? centerRoot.transform.Find(InstanceName) : null;
        if (instance == null)
        {
            Debug.Log("[MarketYellowMaiTreeBuilder] The market does not contain a yellow mai tree instance.");
            return;
        }

        Undo.DestroyObjectImmediate(instance.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the market scene after removing the yellow mai tree.");

        Debug.Log("[MarketYellowMaiTreeBuilder] Removed the yellow mai tree instance from the market scene.");
    }

    private static Material CreateYellowBlossomMaterial()
    {
        Material source = AssetDatabase.LoadAssetAtPath<Material>(BlossomMaterialSourcePath);
        if (source == null)
            throw new InvalidOperationException("Missing blossom material: " + BlossomMaterialSourcePath);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(source) { name = "M_YellowMaiBlossoms" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = source.shader;
            material.CopyPropertiesFromMaterial(source);
        }

        Color maiYellow = new(1f, 0.74f, 0.08f, 1f);
        material.color = maiYellow;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", maiYellow);
        if (material.HasProperty("_Color")) material.SetColor("_Color", maiYellow);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.16f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreatePottedMaiPrefab(Material yellowBlossoms)
    {
        GameObject planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanterSourcePath);
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreeSourcePath);
        if (planterPrefab == null || treePrefab == null)
            throw new InvalidOperationException("The planter or source tree prefab is missing.");

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab);
        root.name = "Potted Yellow Mai Tree";
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Transform oldBranches = root.transform.Find("Cherry Blossom Branches");
        if (oldBranches != null)
            UnityEngine.Object.DestroyImmediate(oldBranches.gameObject);

        Renderer soilRenderer = root.transform.Find("Soil")?.GetComponent<Renderer>();
        if (soilRenderer == null)
            throw new InvalidOperationException("The source planter does not contain its soil renderer.");

        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
        tree.name = "Yellow Mai Canopy";
        PrefabUtility.UnpackPrefabInstance(tree, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        tree.transform.SetParent(root.transform, true);

        foreach (Renderer renderer in tree.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].name.IndexOf("CherryBlossom_Flowers", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    materials[i] = yellowBlossoms;
                    changed = true;
                }
            }
            if (changed) renderer.sharedMaterials = materials;
        }

        FitTreeIntoPlanter(tree, soilRenderer.bounds, MaiTreeHeight);

        CapsuleCollider collider = root.GetComponent<CapsuleCollider>();
        if (collider == null) collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.25f, 0f);
        collider.height = 2.5f;
        collider.radius = 0.78f;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (saved == null)
            throw new InvalidOperationException("Unity could not save the yellow mai tree prefab.");
        return saved;
    }

    private static void FitTreeIntoPlanter(GameObject tree, Bounds soilBounds, float targetHeight)
    {
        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("The source tree has no renderers.");

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y <= 0.001f)
            throw new InvalidOperationException("Unity could not measure the source tree.");

        tree.transform.localScale *= targetHeight / bounds.size.y;
        bounds = CalculateBounds(renderers);

        Vector3 offset = new(
            soilBounds.center.x - bounds.center.x,
            soilBounds.max.y - bounds.min.y - 0.04f,
            soilBounds.center.z - bounds.center.z);
        tree.transform.position += offset;
        tree.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void PlaceInMarket(GameObject prefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the currently open scene before placing the yellow mai tree.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject centerRoot = GameObject.Find("03_Central_Landmark");
        if (centerRoot == null)
            throw new InvalidOperationException("The market scene is missing its central landmark root.");

        Transform existing = centerRoot.transform.Find(InstanceName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

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
            throw new InvalidOperationException("Unity could not save the market scene after placing the yellow mai tree.");
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
