using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MarketPottedCherryBlossomBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string LandmarkFolder = "Assets/Project/Prefabs/Market/Landmarks";
    private const string OutputFolder = LandmarkFolder + "/PottedCherryBlossom";
    private const string MaterialFolder = OutputFolder + "/Materials";
    private const string PrefabPath = OutputFolder + "/PottedCherryBlossom.prefab";
    private const string LargePlanterMeshPath = OutputFolder + "/TerracottaPlanter_LargeOnly.asset";
    private const string LayoutRootName = "Potted Cherry Blossoms - Reference Layout";
    private const string CenterRootName = "03_Central_Landmark";
    private const string CenterTreeName = "Central Cherry Blossom Tree";
    private const string BranchFolder = LandmarkFolder + "/Meshy_AI_Blushing_Branches_20261028_texture_fbx";
    private const string PlanterFolder = LandmarkFolder + "/Meshy_AI_Terracotta_Garden_Planter_20261028_texture_fbx";
    private const string RequestFileName = "MarketPottedCherryBlossomBuilder.request";

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

    [MenuItem("Tools/Tet Is Home/Build Potted Cherry Blossoms")]
    public static void BuildAndPlace()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureAssetFolder(OutputFolder);
        EnsureAssetFolder(MaterialFolder);

        GameObject branchModel = LoadRequiredModel(
            BranchFolder + "/Meshy_AI_Blushing_Branches_20261028_texture.fbx");
        GameObject planterModel = LoadRequiredModel(
            PlanterFolder + "/Meshy_AI_Terracotta_Garden_Planter_20261028_texture.fbx");

        Material branchMaterial = CreateMaterial(
            "M_CherryBlossomBranches",
            BranchFolder + "/Meshy_AI_Blushing_Branches_20261028_texture.png",
            BranchFolder + "/Meshy_AI_Blushing_Branches_20261028_texture_normal.png",
            BranchFolder + "/Meshy_AI_Blushing_Branches_20261028_texture_metallic.png",
            0.18f);
        Material planterMaterial = CreateMaterial(
            "M_TerracottaPlanter",
            PlanterFolder + "/Meshy_AI_Terracotta_Garden_Planter_20261028_texture.png",
            PlanterFolder + "/Meshy_AI_Terracotta_Garden_Planter_20261028_texture_normal.png",
            PlanterFolder + "/Meshy_AI_Terracotta_Garden_Planter_20261028_texture_metallic.png",
            0.12f);
        Material soilMaterial = CreateSoilMaterial();

        GameObject pottedPrefab = CreatePottedPrefab(branchModel, planterModel, branchMaterial, planterMaterial, soilMaterial);
        PlaceInMarket(pottedPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MarketPottedCherryBlossomBuilder] Created a soil-filled planter prefab and placed four cherry blossom arrangements from the reference layout.");
    }

    private static GameObject CreatePottedPrefab(
        GameObject branchModel,
        GameObject planterModel,
        Material branchMaterial,
        Material planterMaterial,
        Material soilMaterial)
    {
        GameObject root = new("Potted Cherry Blossom");

        GameObject planter = UnityEngine.Object.Instantiate(planterModel, root.transform);
        planter.name = "Terracotta Planter";
        ApplyMaterial(planter, planterMaterial);
        KeepOnlyLargestPlanter(planter);
        Bounds planterBounds = FitHeightSetBottomAndCenter(planter, 1.45f, 0f);

        // Measure the actual top rim instead of using the model pivot/bounds center.
        // This keeps the soil aligned even when the large pot was off-center in the source trio.
        CalculatePlanterOpening(planter, planterBounds, out Vector3 openingCenter, out float soilDiameter);
        float soilY = planterBounds.max.y - planterBounds.size.y * 0.065f;
        GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        soil.name = "Soil";
        soil.transform.SetParent(root.transform, false);
        soil.transform.localPosition = new Vector3(openingCenter.x, soilY, openingCenter.z);
        soil.transform.localScale = new Vector3(soilDiameter, 0.04f, soilDiameter);
        soil.GetComponent<Renderer>().sharedMaterial = soilMaterial;
        UnityEngine.Object.DestroyImmediate(soil.GetComponent<Collider>());

        GameObject branches = UnityEngine.Object.Instantiate(branchModel, root.transform);
        branches.name = "Cherry Blossom Branches";
        ApplyMaterial(branches, branchMaterial);
        FitHeightAndSetBottom(branches, 5.8f, soilY - 0.20f);
        branches.transform.position += new Vector3(openingCenter.x, 0f, openingCenter.z);

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.72f, 0f);
        collider.height = 1.44f;
        collider.radius = Mathf.Max(0.35f, soilDiameter * 0.48f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null)
            throw new InvalidOperationException("Unity could not create prefab: " + PrefabPath);
        return prefab;
    }

    private static void PlaceInMarket(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject centerRoot = GameObject.Find(CenterRootName);
        GameObject centerTree = GameObject.Find(CenterTreeName);
        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (centerRoot == null || centerTree == null || terrain == null)
            throw new InvalidOperationException("The market scene is missing its center landmark, cherry tree, or terrain.");

        Transform oldLayout = centerRoot.transform.Find(LayoutRootName);
        if (oldLayout != null)
            UnityEngine.Object.DestroyImmediate(oldLayout.gameObject);

        GameObject layoutRoot = new(LayoutRootName);
        layoutRoot.transform.SetParent(centerRoot.transform, false);

        // Four large pink blossom planters surround the central tree, matching the
        // upper-left, upper-right, lower-left and lower-right placements in MapChoHoa.png.
        Vector3[] offsets =
        {
            new(-7.8f, 0f, 6.2f),
            new( 7.8f, 0f, 6.2f),
            new(-7.8f, 0f,-6.2f),
            new( 7.8f, 0f,-6.2f)
        };
        float[] yaws = { 24f, -31f, 153f, 205f };
        float[] scales = { 1.00f, 0.94f, 0.97f, 1.03f };

        Vector3 center = centerTree.transform.position;
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 position = center + offsets[i];
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y + 0.02f;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = $"Potted Cherry Blossom {i + 1:00}";
            instance.transform.SetParent(layoutRoot.transform, true);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaws[i], 0f);
            instance.transform.localScale = Vector3.one * scales[i];
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the updated market scene.");
    }

    private static Bounds FitHeightAndSetBottom(GameObject instance, float targetHeight, float bottomY)
    {
        Bounds bounds = CalculateBounds(instance);
        if (bounds.size.y < 0.001f)
            throw new InvalidOperationException("Imported model has no usable renderer bounds: " + instance.name);

        instance.transform.localScale *= targetHeight / bounds.size.y;
        bounds = CalculateBounds(instance);
        instance.transform.position += Vector3.up * (bottomY - bounds.min.y);
        return CalculateBounds(instance);
    }

    private static Bounds FitHeightSetBottomAndCenter(GameObject instance, float targetHeight, float bottomY)
    {
        Bounds bounds = FitHeightAndSetBottom(instance, targetHeight, bottomY);
        instance.transform.position += new Vector3(-bounds.center.x, 0f, -bounds.center.z);
        return CalculateBounds(instance);
    }

    private static void KeepOnlyLargestPlanter(GameObject planter)
    {
        MeshFilter[] filters = planter.GetComponentsInChildren<MeshFilter>();
        if (filters.Length != 1 || filters[0].sharedMesh == null)
            throw new InvalidOperationException("The planter import must contain exactly one mesh before it can be separated.");

        Mesh source = filters[0].sharedMesh;
        int[] triangles = source.triangles;
        Vector3[] vertices = source.vertices;
        if (triangles.Length < 3 || vertices.Length == 0)
            throw new InvalidOperationException("The imported planter mesh is empty.");

        DisjointSet components = new(vertices.Length);
        float tolerance = Mathf.Max(0.000001f, source.bounds.size.magnitude * 0.00001f);
        float inverseTolerance = 1f / tolerance;
        var weldedVertices = new System.Collections.Generic.Dictionary<Vector3Int, int>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3Int key = new(
                Mathf.RoundToInt(vertex.x * inverseTolerance),
                Mathf.RoundToInt(vertex.y * inverseTolerance),
                Mathf.RoundToInt(vertex.z * inverseTolerance));
            if (weldedVertices.TryGetValue(key, out int matchingVertex))
                components.Union(i, matchingVertex);
            else
                weldedVertices.Add(key, i);
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            components.Union(triangles[i], triangles[i + 1]);
            components.Union(triangles[i], triangles[i + 2]);
        }

        var componentAreas = new System.Collections.Generic.Dictionary<int, float>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int root = components.Find(triangles[i]);
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            componentAreas[root] = componentAreas.TryGetValue(root, out float total) ? total + area : area;
        }

        int largestRoot = -1;
        float largestArea = -1f;
        foreach (var pair in componentAreas)
        {
            if (pair.Value <= largestArea) continue;
            largestRoot = pair.Key;
            largestArea = pair.Value;
        }

        var keptTriangles = new System.Collections.Generic.List<int>(triangles.Length);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (components.Find(triangles[i]) != largestRoot) continue;
            keptTriangles.Add(triangles[i]);
            keptTriangles.Add(triangles[i + 1]);
            keptTriangles.Add(triangles[i + 2]);
        }

        if (keptTriangles.Count == 0 || keptTriangles.Count == triangles.Length)
            throw new InvalidOperationException("Unity could not separate the large planter from the two small planters.");

        Mesh generated = UnityEngine.Object.Instantiate(source);
        generated.name = "TerracottaPlanter_LargeOnly";
        generated.subMeshCount = 1;
        generated.SetTriangles(keptTriangles, 0, true);
        // RecalculateBounds includes every vertex in the source buffer, including
        // now-unused vertices from the two deleted pots. Set a tight bounds from
        // the retained triangle indices so later centering uses only the large pot.
        Bounds tightBounds = new(vertices[keptTriangles[0]], Vector3.zero);
        for (int i = 1; i < keptTriangles.Count; i++)
            tightBounds.Encapsulate(vertices[keptTriangles[i]]);
        generated.bounds = tightBounds;

        Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(LargePlanterMeshPath);
        if (saved == null)
        {
            AssetDatabase.CreateAsset(generated, LargePlanterMeshPath);
            saved = generated;
        }
        else
        {
            EditorUtility.CopySerialized(generated, saved);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(saved);
        }

        filters[0].sharedMesh = saved;
        Debug.Log($"[MarketPottedCherryBlossomBuilder] Kept the largest planter component ({keptTriangles.Count / 3} of {triangles.Length / 3} triangles).");
    }

    private static void CalculatePlanterOpening(
        GameObject planter,
        Bounds planterBounds,
        out Vector3 center,
        out float soilDiameter)
    {
        MeshFilter filter = planter.GetComponentInChildren<MeshFilter>();
        Mesh mesh = filter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float topBandBottom = planterBounds.max.y - planterBounds.size.y * 0.14f;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        var visited = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < triangles.Length; i++)
        {
            int vertexIndex = triangles[i];
            if (!visited.Add(vertexIndex)) continue;
            Vector3 worldVertex = filter.transform.TransformPoint(vertices[vertexIndex]);
            if (worldVertex.y < topBandBottom) continue;
            minX = Mathf.Min(minX, worldVertex.x);
            maxX = Mathf.Max(maxX, worldVertex.x);
            minZ = Mathf.Min(minZ, worldVertex.z);
            maxZ = Mathf.Max(maxZ, worldVertex.z);
        }

        if (float.IsInfinity(minX) || maxX <= minX || maxZ <= minZ)
            throw new InvalidOperationException("Unity could not measure the large planter opening.");

        center = new Vector3((minX + maxX) * 0.5f, planterBounds.max.y, (minZ + maxZ) * 0.5f);
        soilDiameter = Mathf.Min(maxX - minX, maxZ - minZ) * 0.78f;
        Debug.Log($"[MarketPottedCherryBlossomBuilder] Large planter opening center=({center.x:F3}, {center.z:F3}), soil diameter={soilDiameter:F3}.");
    }

    private sealed class DisjointSet
    {
        private readonly int[] parent;
        private readonly byte[] rank;

        public DisjointSet(int count)
        {
            parent = new int[count];
            rank = new byte[count];
            for (int i = 0; i < count; i++) parent[i] = i;
        }

        public int Find(int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }

        public void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA == rootB) return;
            if (rank[rootA] < rank[rootB]) parent[rootA] = rootB;
            else if (rank[rootA] > rank[rootB]) parent[rootB] = rootA;
            else
            {
                parent[rootB] = rootA;
                rank[rootA]++;
            }
        }
    }

    private static Bounds CalculateBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void ApplyMaterial(GameObject instance, Material material)
    {
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static Material CreateMaterial(
        string name,
        string albedoPath,
        string normalPath,
        string metallicPath,
        float smoothness)
    {
        string materialPath = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        MarkAsNormalMap(normalPath);
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
        if (albedo == null)
            throw new InvalidOperationException("Missing imported texture: " + albedoPath);

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }
        if (metallic != null && material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", metallic);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSoilMaterial()
    {
        const string materialPath = MaterialFolder + "/M_PlanterSoil.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = "M_PlanterSoil" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        Color soil = new(0.20f, 0.085f, 0.025f, 1f);
        material.color = soil;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", soil);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.03f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void MarkAsNormalMap(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer ||
            importer.textureType == TextureImporterType.NormalMap) return;
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static GameObject LoadRequiredModel(string path)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) throw new InvalidOperationException("Missing imported model: " + path);
        return model;
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
