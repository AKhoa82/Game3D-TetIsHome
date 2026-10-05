using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using RiceFoliage;
using TetIsHome.Environment;

namespace TetIsHome.EditorTools
{
    /// <summary>
    /// Deterministically builds the 05_Suburban environment from project assets.
    /// Re-running replaces only the generated environment root and reuses TerrainData.
    /// </summary>
    public static class SuburbanSceneBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/05_Suburban/05_Suburban.unity";
        private const string GeneratedRootName = "[GENERATED] Suburban Environment";
        private const string GeneratedFolder = "Assets/Project/Scenes/05_Suburban/Generated";
        private const string TerrainDataPath = GeneratedFolder + "/SuburbanTerrainData.asset";
        private const string GeneratedMaterialsFolder = GeneratedFolder + "/Materials";

        private const float WorldWidth = 240f;
        private const float WorldLength = 170f;
        private const float WorldHeight = 40f;
        private const int HeightResolution = 257;
        private const int AlphaResolution = 256;

        private const string EnvironmentRoot = "Assets/FantasyEnvironments/Environments";
        private const string TerrainLayerRoot = EnvironmentRoot + "/TerrainLayers";
        private const string EnvironmentPrefabRoot = EnvironmentRoot + "/Prefabs";
        private const string TreePrefabRoot = EnvironmentRoot + "/Ambient-Occlusion-Trees/Prefabs";
        private const string RiceLibraryPath = "Assets/_Project/Art/Vegetation/Rice/RiceFoliageLibrary.asset";
        private const string BambooPrefab = "Assets/ThirdParty/Bamboo/Prefabs/BambusaVulgaris-Master.prefab";
        private const string RiceWaterMaterialPath = "Assets/Project/Materials/Mat_nuoc_ruong.mat";
        private const string RiceWaterRuleMarker = "Water Rule v5 - Flat Edge Blend 025 - Exclude 01 05 10 11 13 15 18 20 21 - Bund 058";

        private static readonly string[] TreePrefabs =
        {
            TreePrefabRoot + "/Oak_tree1.prefab",
            TreePrefabRoot + "/Oak_tree2.prefab",
            TreePrefabRoot + "/Oak_tree3.prefab",
            TreePrefabRoot + "/Deciduous_tree1.prefab",
            TreePrefabRoot + "/Deciduous_tree2.prefab",
            TreePrefabRoot + "/Deciduous_tree3.prefab",
            TreePrefabRoot + "/Deciduous_tree4.prefab",
            TreePrefabRoot + "/Willow_tree1.prefab",
            TreePrefabRoot + "/Willow_tree2.prefab"
        };

        private static readonly string[] GroundDetailPrefabs =
        {
            EnvironmentPrefabRoot + "/Fern1.prefab",
            EnvironmentPrefabRoot + "/Fern2.prefab",
            EnvironmentPrefabRoot + "/Fern3.prefab",
            EnvironmentPrefabRoot + "/Mushroom1.prefab",
            EnvironmentPrefabRoot + "/Mushroom3.prefab",
            EnvironmentPrefabRoot + "/Grass3.prefab",
            EnvironmentPrefabRoot + "/Bush1.prefab"
        };

        private sealed class RicePatchDefinition
        {
            public readonly string name;
            public readonly int stage;
            public readonly float spacing;
            public readonly Vector2[] points;

            public RicePatchDefinition(string name, int stage, float spacing, params Vector2[] points)
            {
                this.name = name;
                this.stage = stage;
                this.spacing = spacing;
                this.points = points;
            }
        }

        private static readonly RicePatchDefinition[] RicePatches = CreateRicePatches();

        [InitializeOnLoadMethod]
        private static void RegisterSuburbanAutoHouseBuild()
        {
            EditorSceneManager.sceneOpened -= HandleSuburbanSceneOpened;
            EditorSceneManager.sceneOpened += HandleSuburbanSceneOpened;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                EditorApplication.delayCall += () => TryAutoBuildVillageHouses(activeScene);
                EditorApplication.delayCall += () => TryAutoBuildRiceWater(activeScene);
                EditorApplication.delayCall += () => TryAutoConfigureSuburbanCamera(activeScene);
            }
        }

        private static void HandleSuburbanSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.delayCall += () => TryAutoBuildVillageHouses(scene);
            EditorApplication.delayCall += () => TryAutoBuildRiceWater(scene);
            EditorApplication.delayCall += () => TryAutoConfigureSuburbanCamera(scene);
        }

        private static void TryAutoConfigureSuburbanCamera(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null || camera.name == "Main Camera - Boundary Path Eye Level")
            {
                return;
            }

            var wasDirty = scene.isDirty;
            ConfigureCamera();
            camera.name = "Main Camera - Boundary Path Eye Level";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void TryAutoBuildRiceWater(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var wasDirty = scene.isDirty;

            var environment = GameObject.Find("Suburban Environment") ?? GameObject.Find(GeneratedRootName);
            var terrain = environment != null ? environment.GetComponentInChildren<Terrain>(true) : null;
            var villageRoot = environment != null
                ? environment.transform.Find("01_Irregular_Rice_Fields_And_Bamboo")
                : null;
            var waterRoot = villageRoot != null ? villageRoot.Find("Water In Young Rice Paddies") : null;
            if (terrain == null || villageRoot == null)
            {
                return;
            }

            if (waterRoot != null && waterRoot.Find(RiceWaterRuleMarker) != null)
            {
                return;
            }

            if (waterRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(waterRoot.gameObject);
            }

            EnsureGeneratedFolder();
            RefreshSuburbanTerrainHeight(terrain);
            BuildRiceWater(terrain, villageRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty)
            {
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[05_Suburban] Young rice paddies were given polygon-matched water automatically.");
        }

        private static void RefreshSuburbanTerrainHeight(Terrain terrain)
        {
            var data = terrain.terrainData;
            if (data == null || data.heightmapResolution != HeightResolution)
            {
                return;
            }

            data.SetHeights(0, 0, GenerateHeights());
            EditorUtility.SetDirty(data);
            terrain.Flush();
        }

        private static void TryAutoBuildVillageHouses(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var environment = GameObject.Find("Suburban Environment") ?? GameObject.Find(GeneratedRootName);
            var terrain = environment != null ? environment.GetComponentInChildren<Terrain>(true) : null;
            var villageRoot = environment != null
                ? environment.transform.Find("01_Irregular_Rice_Fields_And_Bamboo")
                : null;
            var houses = villageRoot != null ? villageRoot.Find("Village Houses") : null;
            var currentHouse = houses != null ? houses.Find("Nha Ven Duong 01 Nho") : null;
            var currentSteps = currentHouse != null ? currentHouse.Find("Stone Porch Step 01") : null;

            if (terrain == null || villageRoot == null || (currentHouse != null && currentSteps != null))
            {
                return;
            }

            if (houses != null)
            {
                UnityEngine.Object.DestroyImmediate(houses.gameObject);
            }

            EnsureGeneratedFolder();
            BuildVillageHouses(terrain, villageRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[05_Suburban] Village houses were generated automatically when the scene opened.");
        }

        [MenuItem("Tools/Tet Is Home/Build Scene/05 Suburban From Reference")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                BuildScene();
                EditorUtility.DisplayDialog(
                    "05_Suburban built",
                    "Village, rice fields, boundary road, dark forest, ruins, lighting and encounter markers were generated successfully.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("05_Suburban build failed", exception.Message, "OK");
            }
        }

        public static void BuildBatch()
        {
            try
            {
                BuildScene();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildScene()
        {
            ValidateRequiredAssets();
            EnsureGeneratedFolder();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveOldGeneratedRoot();

            var generatedRoot = new GameObject(GeneratedRootName);
            var terrain = BuildTerrain(generatedRoot.transform);

            var villageRoot = CreateGroup("01_Irregular_Rice_Fields_And_Bamboo", generatedRoot.transform);
            var roadRoot = CreateGroup("02_Boundary_Road", generatedRoot.transform);
            var forestRoot = CreateGroup("03_Dark_Forest", generatedRoot.transform);
            var markerRoot = CreateGroup("04_Gameplay_Markers", generatedRoot.transform);

            BuildRiceFields(terrain, villageRoot);
            BuildBambooFieldBoundary(terrain, villageRoot);
            BuildVillageHouses(terrain, villageRoot);
            BuildBoundaryRoadSetDressing(terrain, roadRoot);
            BuildForest(terrain, forestRoot);
            BuildGameplayMarkers(terrain, markerRoot);
            ConvertPlacedMaterialsToUrp(generatedRoot);
            ConfigureSceneLighting();
            ConfigureCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Unity could not save " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = generatedRoot;
            Debug.Log(
                "[05_Suburban] Build complete. Created terrain, " + RicePatches.Length +
                " tightly arranged irregular rice fields with mixed growth stages and raised earthen bund paths, " +
                "boundary road composition, dark forest, ruins and gameplay markers in " + ScenePath + ".");
        }

        private static void ValidateRequiredAssets()
        {
            var required = new List<string>
            {
                ScenePath,
                RiceLibraryPath,
                BambooPrefab,
                RiceWaterMaterialPath,
                TerrainLayerRoot + "/layer_ground_grass.terrainlayer",
                TerrainLayerRoot + "/layer_ground_soil.terrainlayer",
                TerrainLayerRoot + "/layer_ground_road.terrainlayer",
                TerrainLayerRoot + "/layer_ground_rock.terrainlayer",
                TerrainLayerRoot + "/layer_ground_stones.terrainlayer"
            };
            required.AddRange(TreePrefabs);

            var missing = required.Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException("Missing required scene assets:\n" + string.Join("\n", missing));
            }
        }

        private static void EnsureGeneratedFolder()
        {
            const string sceneFolder = "Assets/Project/Scenes/05_Suburban";
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder(sceneFolder, "Generated");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedMaterialsFolder))
            {
                AssetDatabase.CreateFolder(GeneratedFolder, "Materials");
            }
        }

        private static void RemoveOldGeneratedRoot()
        {
            var previous = GameObject.Find(GeneratedRootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static Terrain BuildTerrain(Transform parent)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData { name = "SuburbanTerrainData" };
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }

            data.heightmapResolution = HeightResolution;
            data.alphamapResolution = AlphaResolution;
            data.baseMapResolution = 512;
            data.size = new Vector3(WorldWidth, WorldHeight, WorldLength);
            data.SetHeights(0, 0, GenerateHeights());
            data.terrainLayers = LoadTerrainLayers();
            data.SetAlphamaps(0, 0, GenerateSplatMap(data.terrainLayers.Length));
            data.SetDetailResolution(512, 16);
            EditorUtility.SetDirty(data);

            var terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Suburban Terrain";
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.position = Vector3.zero;
            terrainObject.isStatic = true;

            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 900f;
            terrain.treeDistance = 450f;
            terrain.detailObjectDistance = 120f;
            terrain.detailObjectDensity = 0.8f;
            terrain.shadowCastingMode = ShadowCastingMode.On;
            terrain.allowAutoConnect = false;
            terrain.Flush();
            return terrain;
        }

        private static TerrainLayer[] LoadTerrainLayers()
        {
            var names = new[]
            {
                "layer_ground_grass",
                "layer_ground_soil",
                "layer_ground_road",
                "layer_ground_rock",
                "layer_ground_stones"
            };

            return names.Select(name =>
            {
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerRoot + "/" + name + ".terrainlayer");
                if (layer == null)
                {
                    throw new InvalidOperationException("Could not load TerrainLayer " + name);
                }

                return layer;
            }).ToArray();
        }

        private static float[,] GenerateHeights()
        {
            var heights = new float[HeightResolution, HeightResolution];
            for (var zIndex = 0; zIndex < HeightResolution; zIndex++)
            {
                var z = zIndex / (float)(HeightResolution - 1) * WorldLength;
                for (var xIndex = 0; xIndex < HeightResolution; xIndex++)
                {
                    var x = xIndex / (float)(HeightResolution - 1) * WorldWidth;
                    var roadX = RoadCenterX(z);
                    var signedForestDistance = x - roadX;
                    var forestBlend = SmoothStep(-3f, 24f, signedForestDistance);

                    var villageTerrace = Mathf.Floor(z / 28f) * 0.13f;
                    var villageRoll = Mathf.Sin(x * 0.045f) * 0.10f + Mathf.Sin(z * 0.055f) * 0.08f;
                    var forestNoiseLarge = Mathf.PerlinNoise(x * 0.020f + 8.3f, z * 0.022f + 19.1f);
                    var forestNoiseSmall = Mathf.PerlinNoise(x * 0.061f + 31.7f, z * 0.057f + 6.4f);
                    var forestRidge = Mathf.Sin(x * 0.072f + z * 0.041f) * 1.45f;
                    var forestCrossRidge = Mathf.Sin(x * 0.031f - z * 0.083f) * 0.95f;
                    var easternRise = SmoothStep(190f, 240f, x) * 7.5f;

                    // Paddy interiors form a shallow basin. Even the upper terraces stay
                    // below the boundary road, while the forest rises into rolling hills.
                    var worldHeight = 0.62f + villageTerrace + villageRoll;
                    var forestHeight = 4.8f + forestNoiseLarge * 6.8f + forestNoiseSmall * 2.6f +
                                       forestRidge + forestCrossRidge + easternRise;
                    worldHeight = Mathf.Lerp(worldHeight, forestHeight, forestBlend);

                    var roadDistance = Mathf.Abs(signedForestDistance);
                    var roadBlend = Mathf.Exp(-(roadDistance * roadDistance) / 62f);
                    var roadHeight = 2.90f + z * 0.003f;
                    worldHeight = Mathf.Lerp(worldHeight, roadHeight, roadBlend * 0.92f);

                    // Paddy boundaries double as narrow footpaths. Raising the polygon
                    // edges produces the compacted earthen bunds common between fields.
                    var bund = RiceBundMask(x, z);
                    worldHeight += bund * 0.58f;

                    heights[zIndex, xIndex] = Mathf.Clamp01(worldHeight / WorldHeight);
                }
            }

            return heights;
        }

        private static float[,,] GenerateSplatMap(int layerCount)
        {
            var map = new float[AlphaResolution, AlphaResolution, layerCount];
            for (var zIndex = 0; zIndex < AlphaResolution; zIndex++)
            {
                var z = zIndex / (float)(AlphaResolution - 1) * WorldLength;
                for (var xIndex = 0; xIndex < AlphaResolution; xIndex++)
                {
                    var x = xIndex / (float)(AlphaResolution - 1) * WorldWidth;
                    var distanceToRoad = Mathf.Abs(x - RoadCenterX(z));
                    var forest = SmoothStep(4f, 35f, x - RoadCenterX(z));
                    var field = FieldMask(x, z);
                    var bund = RiceBundMask(x, z);
                    var road = Mathf.Exp(-(distanceToRoad * distanceToRoad) / 24f);
                    var noise = Mathf.PerlinNoise(x * 0.055f + 4f, z * 0.052f + 11f);

                    var weights = new float[layerCount];
                    weights[0] = Mathf.Max(0.04f, 1.2f - road - field * 0.65f - forest * 0.35f - bund * 0.7f);
                    weights[1] = 0.08f + field * 1.7f + bund * 2.8f + (1f - forest) * noise * 0.12f;
                    weights[2] = road * 3.5f;
                    weights[3] = forest * (0.25f + noise * 0.8f);
                    weights[4] = forest * (0.15f + (1f - noise) * 0.65f);

                    var sum = weights.Sum();
                    for (var layer = 0; layer < layerCount; layer++)
                    {
                        map[zIndex, xIndex, layer] = weights[layer] / sum;
                    }
                }
            }

            return map;
        }

        private static float FieldMask(float x, float z)
        {
            var point = new Vector2(x, z);
            foreach (var patch in RicePatches)
            {
                if (ContainsPoint(patch.points, point))
                {
                    return 1f;
                }
            }

            return 0f;
        }

        private static float RiceBundMask(float x, float z)
        {
            if (z < 1f || z > WorldLength - 1f || x < 1f || x > RoadCenterX(z) - 4.5f)
            {
                return 0f;
            }

            var point = new Vector2(x, z);
            var minimumDistance = float.MaxValue;
            foreach (var patch in RicePatches)
            {
                for (var i = 0; i < patch.points.Length; i++)
                {
                    var a = patch.points[i];
                    var b = patch.points[(i + 1) % patch.points.Length];
                    minimumDistance = Mathf.Min(minimumDistance, DistanceToSegment(point, a, b));
                }
            }

            return 1f - SmoothStep(0.28f, 1.45f, minimumDistance);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return Vector2.Distance(point, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
            return Vector2.Distance(point, a + segment * t);
        }

        private static bool ContainsPoint(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                var crosses = (a.y > point.y) != (b.y > point.y) &&
                              point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float RoadCenterX(float z)
        {
            var broadCurve = Mathf.Sin(z * 0.041f + 0.35f) * 10.5f;
            var smallMeander = Mathf.Sin(z * 0.096f + 1.15f) * 2.6f;
            return 90f + z * 0.17f + broadCurve + smallMeander;
        }

        private static RicePatchDefinition[] CreateRicePatches()
        {
            // Shared row boundaries keep the paddies tightly packed. Each polygon is
            // inset from those boundaries, leaving a continuous 1.8 m walking network.
            var rowEdges = new[] { 2f, 29f, 55f, 82f, 109f, 137f, 168f };
            var stageCycle = new[] { 4, 2, 3, 1, 4, 0, 2, 4, 1, 3 };
            var patches = new List<RicePatchDefinition>();
            const float pathWidth = 1.8f;

            for (var row = 0; row < rowEdges.Length - 1; row++)
            {
                var bottomZ = rowEdges[row];
                var topZ = rowEdges[row + 1];
                var columns = row % 2 == 0 ? 4 : 3;
                var fractions = columns == 4
                    ? new[] { 0f, 0.22f, 0.49f, 0.75f, 1f }
                    : new[] { 0f, 0.31f, 0.64f, 1f };
                // Leave only a narrow verge: the bamboo is grouped into a few natural
                // clumps instead of forming a continuous artificial-looking screen.
                var bottomRight = RoadCenterX(bottomZ) - 9.5f;
                var topRight = RoadCenterX(topZ) - 9.5f;

                for (var column = 0; column < columns; column++)
                {
                    var leftBottom = Mathf.Lerp(2f, bottomRight, fractions[column]) + pathWidth * 0.5f;
                    var rightBottom = Mathf.Lerp(2f, bottomRight, fractions[column + 1]) - pathWidth * 0.5f;
                    var leftTop = Mathf.Lerp(2f, topRight, fractions[column]) + pathWidth * 0.5f;
                    var rightTop = Mathf.Lerp(2f, topRight, fractions[column + 1]) - pathWidth * 0.5f;
                    var z0 = bottomZ + pathWidth * 0.5f;
                    var z1 = topZ - pathWidth * 0.5f;

                    // Alternating offsets create trapezoids and pentagons while keeping
                    // adjacent parcels aligned around the same narrow raised path.
                    var skew = Mathf.Sin((row + 1) * 1.71f + column * 2.13f) * 1.6f;
                    leftTop += skew * 0.35f;
                    rightTop += skew;
                    var points = (row + column) % 3 == 0
                        ? new[]
                        {
                            new Vector2(leftBottom, z0),
                            new Vector2(rightBottom, z0),
                            new Vector2(rightBottom + skew * 0.35f, Mathf.Lerp(z0, z1, 0.52f)),
                            new Vector2(rightTop, z1),
                            new Vector2(leftTop, z1)
                        }
                        : new[]
                        {
                            new Vector2(leftBottom, z0),
                            new Vector2(rightBottom, z0),
                            new Vector2(rightTop, z1),
                            new Vector2(leftTop, z1)
                        };

                    var index = patches.Count;
                    patches.Add(new RicePatchDefinition(
                        "Thua_Ruong_" + (index + 1).ToString("00"),
                        stageCycle[index % stageCycle.Length],
                        0.62f + (index % 4) * 0.035f,
                        points));
                }
            }

            return patches.ToArray();
        }

        private static float SmoothStep(float from, float to, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
        }

        private static void BuildRiceFields(Terrain terrain, Transform parent)
        {
            var library = AssetDatabase.LoadAssetAtPath<RiceFoliageLibrary>(RiceLibraryPath);
            if (library == null)
            {
                throw new InvalidOperationException("Could not load rice foliage library: " + RiceLibraryPath);
            }

            for (var i = 0; i < RicePatches.Length; i++)
            {
                var definition = RicePatches[i];
                var center = definition.points.Aggregate(Vector2.zero, (sum, point) => sum + point) / definition.points.Length;
                var localPolygon = definition.points.Select(point => point - center).ToArray();
                var field = new GameObject((i + 1).ToString("00") + "_" + definition.name);
                field.transform.SetParent(parent, false);
                field.transform.position = new Vector3(center.x, 0f, center.y);
                var renderer = field.AddComponent<SuburbanRicePatchRenderer>();
                renderer.Configure(library, definition.stage, localPolygon, definition.spacing, 3100 + i * 97, terrain);
            }

            BuildRiceWater(terrain, parent);
        }

        private static void BuildRiceWater(Terrain terrain, Transform parent)
        {
            var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(RiceWaterMaterialPath);
            if (waterMaterial == null)
            {
                throw new InvalidOperationException("Could not load rice water material: " + RiceWaterMaterialPath);
            }

            var waterRoot = CreateGroup("Water In Young Rice Paddies", parent);
            new GameObject(RiceWaterRuleMarker).transform.SetParent(waterRoot, false);
            for (var i = 0; i < RicePatches.Length; i++)
            {
                var patch = RicePatches[i];
                // Stages 3 and 4 are heading-to-yellow or already mature; keep
                // those fields dry. The listed paddies are intentionally kept dry.
                if (patch.stage >= 3 || IsRiceWaterExcluded(i + 1))
                {
                    continue;
                }

                var water = new GameObject("Water - " + patch.name);
                water.transform.SetParent(waterRoot, false);
                var meshFilter = water.AddComponent<MeshFilter>();
                var meshRenderer = water.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = waterMaterial;
                meshFilter.sharedMesh = CreateRiceWaterMesh(terrain, patch.points, patch.name);
                water.isStatic = true;
            }
        }

        private static bool IsRiceWaterExcluded(int oneBasedPatchNumber)
        {
            switch (oneBasedPatchNumber)
            {
                case 1:
                case 5:
                case 10:
                case 11:
                case 13:
                case 15:
                case 18:
                case 20:
                case 21:
                    return true;
                default:
                    return false;
            }
        }

        private static Mesh CreateRiceWaterMesh(Terrain terrain, IReadOnlyList<Vector2> polygon, string name)
        {
            var center = Vector2.zero;
            for (var i = 0; i < polygon.Count; i++)
            {
                center += polygon[i];
            }

            center /= polygon.Count;
            var waterLevel = PolygonWaterLevel(terrain, polygon);
            var vertices = new Vector3[polygon.Count + 1];
            vertices[0] = new Vector3(center.x, waterLevel, center.y);
            for (var i = 0; i < polygon.Count; i++)
            {
                var edgeGround = GroundHeight(terrain, polygon[i].x, polygon[i].y) - 0.015f;
                var edgeHeight = Mathf.Lerp(waterLevel, edgeGround, 0.25f);
                vertices[i + 1] = new Vector3(polygon[i].x, edgeHeight, polygon[i].y);
            }

            var triangles = new int[polygon.Count * 3];
            for (var i = 0; i < polygon.Count; i++)
            {
                var next = (i + 1) % polygon.Count;
                var triangle = i * 3;
                // Reverse winding so the top face points upward in Unity's XZ plane.
                triangles[triangle] = 0;
                triangles[triangle + 1] = next + 1;
                triangles[triangle + 2] = i + 1;
            }

            var mesh = new Mesh { name = "Water Surface - " + name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float PolygonWaterLevel(Terrain terrain, IReadOnlyList<Vector2> polygon)
        {
            var highestGround = float.MinValue;
            for (var i = 0; i < polygon.Count; i++)
            {
                highestGround = Mathf.Max(highestGround, GroundHeight(terrain, polygon[i].x, polygon[i].y));
            }

            // Flat water sits just below the highest edge, keeping the surface
            // visible across the paddy without covering the rice stalks.
            return highestGround - 0.03f;
        }

        private static void BuildBambooFieldBoundary(Terrain terrain, Transform parent)
        {
            var bambooRoot = CreateGroup("Sparse Bamboo Clumps By The Road", parent);
            var zPositions = new[] { 18f, 47f, 79f, 113f, 146f, 163f };
            var sideOffsets = new[] { -8.1f, -9.0f, -7.7f, -8.8f, -8.0f, -9.2f };
            var rotations = new[] { 18f, -11f, 31f, 7f, -24f, 15f };
            var heights = new[] { 8.4f, 10.2f, 7.8f, 9.5f, 8.8f, 10.5f };

            for (var i = 0; i < zPositions.Length; i++)
            {
                var z = zPositions[i];
                PlaceGrounded(
                    BambooPrefab,
                    bambooRoot,
                    terrain,
                    RoadCenterX(z) + sideOffsets[i],
                    z,
                    rotations[i],
                    heights[i],
                    "Bamboo Clump " + (i + 1).ToString("00"));
            }
        }

        [MenuItem("Tools/Tet Is Home/Build Scene/Add Village Houses To Open Suburban Scene")]
        public static void AddVillageHousesToOpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var environment = GameObject.Find("Suburban Environment") ?? GameObject.Find(GeneratedRootName);
            var terrain = environment != null ? environment.GetComponentInChildren<Terrain>(true) : null;
            if (terrain == null)
            {
                EditorUtility.DisplayDialog(
                    "Suburban environment not found",
                    "Open the 05_Suburban scene containing Suburban Environment, then try again.",
                    "OK");
                return;
            }

            var villageRoot = environment.transform.Find("01_Irregular_Rice_Fields_And_Bamboo");
            if (villageRoot == null)
            {
                villageRoot = CreateGroup("01_Irregular_Rice_Fields_And_Bamboo", environment.transform);
            }

            var previousHouses = villageRoot.Find("Village Houses");
            if (previousHouses != null)
            {
                UnityEngine.Object.DestroyImmediate(previousHouses.gameObject);
            }

            EnsureGeneratedFolder();
            BuildVillageHouses(terrain, villageRoot);
            EditorSceneManager.MarkSceneDirty(environment.scene);
            EditorSceneManager.SaveScene(environment.scene);
            Selection.activeGameObject = villageRoot.gameObject;
        }

        private static void BuildVillageHouses(Terrain terrain, Transform parent)
        {
            var housesRoot = CreateGroup("Village Houses", parent);
            var homes = new[]
            {
                // The houses sit on the forest-side verge of the boundary road,
                // keeping their footprints clear of the rice polygons.
                new { name = "Nha Ven Duong 01 Nho", z = 22f, offset = 9.0f, yaw = 78f, width = 3.7f, depth = 3.1f, wallHeight = 2.7f, variant = 0 },
                new { name = "Nha Ven Duong 02 Vua", z = 61f, offset = 11.0f, yaw = 103f, width = 5.2f, depth = 4.2f, wallHeight = 3.3f, variant = 1 },
                new { name = "Nha Ven Duong 03 Dai", z = 103f, offset = 8.5f, yaw = 66f, width = 6.6f, depth = 5.0f, wallHeight = 4.1f, variant = 2 },
                new { name = "Nha Ven Duong 04 Hep", z = 148f, offset = 10.0f, yaw = 121f, width = 4.3f, depth = 5.6f, wallHeight = 3.0f, variant = 1 }
            };

            foreach (var home in homes)
            {
                BuildVietnameseFarmhouse(
                    home.name,
                    housesRoot,
                    terrain,
                    RoadCenterX(home.z) + home.offset,
                    home.z,
                    home.yaw,
                    home.width,
                    home.depth,
                    home.wallHeight,
                    home.variant);
            }
        }

        private static void BuildVietnameseFarmhouse(
            string name,
            Transform parent,
            Terrain terrain,
            float x,
            float z,
            float yaw,
            float width,
            float depth,
            float wallHeight,
            int variant)
        {
            var wallMaterial = CreateOrUpdateRuinMaterial(
                variant == 2 ? "Vietnamese_House_Wall_PaleGreen" : "Vietnamese_House_Wall_WarmOchre",
                variant == 2 ? new Color(0.48f, 0.55f, 0.39f, 1f) : new Color(0.72f, 0.57f, 0.32f, 1f),
                0.08f);
            var roofMaterial = CreateOrUpdateRuinMaterial(
                "Vietnamese_House_Red_Clay_Tile",
                new Color(0.48f, 0.16f, 0.075f, 1f),
                0.12f);
            var woodMaterial = CreateOrUpdateRuinMaterial(
                "Vietnamese_House_Dark_Wood",
                new Color(0.20f, 0.095f, 0.038f, 1f),
                0.16f);
            var foundationMaterial = CreateOrUpdateRuinMaterial(
                "Vietnamese_House_Foundation",
                new Color(0.31f, 0.29f, 0.23f, 1f),
                0.06f);
            var windowMaterial = CreateOrUpdateRuinMaterial(
                "Vietnamese_House_Window_Dark",
                new Color(0.055f, 0.085f, 0.075f, 1f),
                0.30f);

            HouseGroundHeights(terrain, x, z, yaw, width, depth, out var lowestGround, out var highestGround);
            var house = CreateGroup(name, parent);
            house.position = new Vector3(x, highestGround, z);
            house.rotation = Quaternion.Euler(0f, yaw, 0f);

            const float roofPitch = 32f;
            var roofRadians = roofPitch * Mathf.Deg2Rad;
            var halfRoofRun = depth * 0.5f + 0.65f;
            var roofSlopeLength = halfRoofRun / Mathf.Cos(roofRadians);
            var roofRise = halfRoofRun * Mathf.Tan(roofRadians);

            var foundationDepth = Mathf.Max(0.36f, highestGround - lowestGround + 0.36f);
            CreateHousePart("Stone Foundation", house,
                new Vector3(0f, 0.18f - (foundationDepth - 0.36f) * 0.5f, 0f),
                new Vector3(width + 0.5f, foundationDepth, depth + 0.5f), Vector3.zero, foundationMaterial);
            CreateHousePart("Plastered Walls", house, new Vector3(0f, wallHeight * 0.5f + 0.35f, 0f),
                new Vector3(width, wallHeight, depth), Vector3.zero, wallMaterial);

            var roofY = wallHeight + 0.35f + roofRise * 0.5f;
            var roofZ = depth * 0.25f + 0.28f;
            CreateHousePart("Red Tile Roof Front", house, new Vector3(0f, roofY, -roofZ),
                new Vector3(width + 1.25f, 0.22f, roofSlopeLength), new Vector3(-roofPitch, 0f, 0f), roofMaterial);
            CreateHousePart("Red Tile Roof Back", house, new Vector3(0f, roofY, roofZ),
                new Vector3(width + 1.25f, 0.22f, roofSlopeLength), new Vector3(roofPitch, 0f, 0f), roofMaterial);
            CreateHousePart("Roof Ridge", house, new Vector3(0f, wallHeight + 0.38f + roofRise, 0f),
                new Vector3(width + 1.35f, 0.28f, 0.32f), Vector3.zero, roofMaterial);

            var frontZ = -depth * 0.5f - 0.07f;
            CreateHousePart("Wooden Front Door", house, new Vector3(0f, 1.48f, frontZ),
                new Vector3(1.55f, 2.55f, 0.16f), Vector3.zero, woodMaterial);
            CreateHousePart("Left Window", house, new Vector3(-width * 0.29f, 1.75f, frontZ - 0.01f),
                new Vector3(1.35f, 1.2f, 0.14f), Vector3.zero, windowMaterial);
            CreateHousePart("Right Window", house, new Vector3(width * 0.29f, 1.75f, frontZ - 0.01f),
                new Vector3(1.35f, 1.2f, 0.14f), Vector3.zero, windowMaterial);

            var porchZ = -depth * 0.5f - 1.0f;
            CreateHousePart("Front Porch", house, new Vector3(0f, 0.30f, porchZ),
                new Vector3(width * 0.78f, 0.24f, 1.75f), Vector3.zero, foundationMaterial);
            CreateHousePart("Porch Post Left", house, new Vector3(-width * 0.32f, 1.55f, porchZ - 0.52f),
                new Vector3(0.22f, 2.65f, 0.22f), Vector3.zero, woodMaterial);
            CreateHousePart("Porch Post Right", house, new Vector3(width * 0.32f, 1.55f, porchZ - 0.52f),
                new Vector3(0.22f, 2.65f, 0.22f), Vector3.zero, woodMaterial);

            var stepRise = Mathf.Max(0.12f, (highestGround - lowestGround + 0.18f) / 3f);
            for (var step = 0; step < 3; step++)
            {
                var stepTop = 0.42f - stepRise * step;
                CreateHousePart(
                    "Stone Porch Step " + (step + 1).ToString("00"),
                    house,
                    new Vector3(0f, stepTop - stepRise * 0.5f, porchZ - 0.58f - step * 0.52f),
                    new Vector3(width * 0.62f, stepRise, 0.52f),
                    Vector3.zero,
                    foundationMaterial);
            }

            if (variant == 1)
            {
                CreateHousePart("Side Kitchen", house, new Vector3(width * 0.57f, 1.35f, 0.45f),
                    new Vector3(2.4f, 2.35f, depth * 0.72f), Vector3.zero, wallMaterial);
                CreateHousePart("Kitchen Tile Roof", house, new Vector3(width * 0.57f, 2.72f, 0.45f),
                    new Vector3(2.85f, 0.20f, depth * 0.82f), new Vector3(0f, 0f, -9f), roofMaterial);
            }
        }

        private static void HouseGroundHeights(
            Terrain terrain,
            float x,
            float z,
            float yaw,
            float width,
            float depth,
            out float lowest,
            out float highest)
        {
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var halfWidth = width * 0.5f + 0.8f;
            var halfDepth = depth * 0.5f + 1.15f;
            var samples = new[]
            {
                Vector3.zero,
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth)
            };

            lowest = float.MaxValue;
            highest = float.MinValue;
            foreach (var sample in samples)
            {
                var worldPoint = new Vector3(x, 0f, z) + rotation * sample;
                var ground = GroundHeight(terrain, worldPoint.x, worldPoint.z);
                lowest = Mathf.Min(lowest, ground);
                highest = Mathf.Max(highest, ground);
            }
        }

        private static GameObject CreateHousePart(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles,
            Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles);
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            part.isStatic = true;
            return part;
        }

        private static void BuildBoundaryRoadSetDressing(Terrain terrain, Transform parent)
        {
            PlaceGrounded(EnvironmentPrefabRoot + "/Stone1.prefab", parent, terrain, RoadCenterX(42f) + 8f, 42f, 12f, 2.4f, "Boundary Stone South");
            PlaceGrounded(EnvironmentPrefabRoot + "/Stone2.prefab", parent, terrain, RoadCenterX(109f) - 8f, 109f, 80f, 2f, "Boundary Stone Center");

            var rng = new System.Random(905);
            for (var i = 0; i < 18; i++)
            {
                var z = Mathf.Lerp(5f, 165f, (float)rng.NextDouble());
                var side = i % 2 == 0 ? -1f : 1f;
                var x = RoadCenterX(z) + side * Mathf.Lerp(7f, 12f, (float)rng.NextDouble());
                var prefab = i % 3 == 0 ? EnvironmentPrefabRoot + "/Grass4.prefab" : EnvironmentPrefabRoot + "/Plant2.prefab";
                PlaceGrounded(prefab, parent, terrain, x, z, rng.Next(0, 360), 0.9f + (float)rng.NextDouble(), "Road Verge " + i);
            }
        }

        private static void BuildForest(Terrain terrain, Transform parent)
        {
            var treeRoot = CreateGroup("Canopy Trees", parent);
            var bambooRoot = CreateGroup("Bamboo Thickets", parent);
            var undergrowthRoot = CreateGroup("Undergrowth And Rocks", parent);
            var ruinsRoot = CreateGroup("Forgotten Ruins", parent);
            var rng = new System.Random(7319);

            for (var i = 0; i < 52; i++)
            {
                var z = (float)rng.NextDouble() * WorldLength;
                var minX = RoadCenterX(z) + 13f;
                var x = Mathf.Lerp(minX, WorldWidth - 5f, (float)rng.NextDouble());
                if (Vector2.Distance(new Vector2(x, z), new Vector2(198f, 119f)) < 19f)
                {
                    i--;
                    continue;
                }

                var prefab = TreePrefabs[rng.Next(TreePrefabs.Length)];
                var height = Mathf.Lerp(16f, 27f, (float)rng.NextDouble());
                PlaceGrounded(prefab, treeRoot, terrain, x, z, rng.Next(0, 360), height, "Forest Tree " + i);
            }

            for (var i = 0; i < 13; i++)
            {
                var z = Mathf.Lerp(8f, 164f, (float)rng.NextDouble());
                var minX = RoadCenterX(z) + (i < 7 ? 10f : 28f);
                var x = Mathf.Lerp(minX, WorldWidth - 8f, (float)rng.NextDouble());
                PlaceGrounded(BambooPrefab, bambooRoot, terrain, x, z, rng.Next(0, 360), Mathf.Lerp(12f, 18f, (float)rng.NextDouble()), "Bamboo Thicket " + i);
            }

            for (var i = 0; i < 46; i++)
            {
                var z = (float)rng.NextDouble() * WorldLength;
                var x = Mathf.Lerp(RoadCenterX(z) + 9f, WorldWidth - 4f, (float)rng.NextDouble());
                var prefab = GroundDetailPrefabs[rng.Next(GroundDetailPrefabs.Length)];
                PlaceGrounded(prefab, undergrowthRoot, terrain, x, z, rng.Next(0, 360), Mathf.Lerp(0.6f, 1.8f, (float)rng.NextDouble()), "Forest Detail " + i);
            }

            for (var i = 0; i < 16; i++)
            {
                var z = (float)rng.NextDouble() * WorldLength;
                var x = Mathf.Lerp(RoadCenterX(z) + 15f, WorldWidth - 3f, (float)rng.NextDouble());
                var prefab = EnvironmentPrefabRoot + "/Rock" + (1 + i % 3) + ".prefab";
                PlaceGrounded(prefab, undergrowthRoot, terrain, x, z, rng.Next(0, 360), Mathf.Lerp(2f, 5.5f, (float)rng.NextDouble()), "Mossy Rock " + i);
            }

            BuildVietnameseForestRuins(terrain, ruinsRoot);
            PlaceGrounded(TreePrefabRoot + "/Deciduous_tree_stump.prefab", ruinsRoot, terrain, 188f, 108f, 45f, 3.5f, "Ancient Stump");
        }

        private static void BuildVietnameseForestRuins(Terrain terrain, Transform parent)
        {
            var laterite = CreateOrUpdateRuinMaterial(
                "Vietnamese_Ruin_Laterite",
                new Color(0.34f, 0.19f, 0.10f, 1f),
                0.12f);
            var oldStone = CreateOrUpdateRuinMaterial(
                "Vietnamese_Ruin_OldStone",
                new Color(0.34f, 0.35f, 0.28f, 1f),
                0.18f);
            var mossStone = CreateOrUpdateRuinMaterial(
                "Vietnamese_Ruin_MossStone",
                new Color(0.20f, 0.28f, 0.14f, 1f),
                0.10f);

            var structure = CreateGroup("Ruined Vietnamese Forest Shrine", parent);
            const float centerX = 198f;
            const float centerZ = 119f;

            // Low rectangular shrine foundation and broken courtyard paving.
            CreateRuinBlock("Nen Mieu Da Ong", structure, terrain, centerX, centerZ + 3f, 3f,
                new Vector3(12f, 0.55f, 9f), laterite);
            CreateRuinBlock("San Da Vo A", structure, terrain, centerX - 4.1f, centerZ - 3.6f, -4f,
                new Vector3(5.6f, 0.28f, 3.8f), oldStone);
            CreateRuinBlock("San Da Vo B", structure, terrain, centerX + 3.1f, centerZ - 3.2f, 5f,
                new Vector3(4.5f, 0.24f, 3.2f), mossStone);

            // Three broad stone steps lead up from the forest path.
            CreateRuinBlock("Bac Da Duoi", structure, terrain, centerX, centerZ - 7.4f, 0f,
                new Vector3(6.8f, 0.34f, 1.5f), oldStone, 0.10f);
            CreateRuinBlock("Bac Da Giua", structure, terrain, centerX, centerZ - 6.5f, 0f,
                new Vector3(6.1f, 0.46f, 1.4f), mossStone, 0.34f);
            CreateRuinBlock("Bac Da Tren", structure, terrain, centerX, centerZ - 5.6f, 0f,
                new Vector3(5.4f, 0.58f, 1.3f), oldStone, 0.62f);

            // Square tam-quan-style gate piers replace the European arches/towers.
            CreateRuinBlock("Tru Cong Trai", structure, terrain, centerX - 3.6f, centerZ - 4.4f, -2f,
                new Vector3(1.45f, 4.8f, 1.45f), laterite, 0.45f);
            CreateRuinBlock("Tru Cong Phai", structure, terrain, centerX + 3.6f, centerZ - 4.4f, 2f,
                new Vector3(1.45f, 4.2f, 1.45f), mossStone, 0.45f);
            CreateRuinBlock("Xa Cong Gay", structure, terrain, centerX - 0.8f, centerZ - 4.4f, -3f,
                new Vector3(6.6f, 0.7f, 1.25f), laterite, 4.55f);

            // Collapsed low perimeter walls with deliberate gaps and uneven heights.
            CreateRuinBlock("Tuong Da Ong Trai A", structure, terrain, centerX - 6.1f, centerZ + 1.8f, 1f,
                new Vector3(0.75f, 2.1f, 5.4f), laterite, 0.28f);
            CreateRuinBlock("Tuong Da Ong Trai B", structure, terrain, centerX - 6.0f, centerZ + 6.1f, -4f,
                new Vector3(0.8f, 1.25f, 2.4f), mossStone, 0.28f);
            CreateRuinBlock("Tuong Da Ong Phai", structure, terrain, centerX + 6.0f, centerZ + 4.2f, -2f,
                new Vector3(0.8f, 1.65f, 5.8f), laterite, 0.28f);
            CreateRuinBlock("Tuong Hau Gay A", structure, terrain, centerX - 3.8f, centerZ + 7.2f, 2f,
                new Vector3(4.2f, 1.45f, 0.8f), laterite, 0.28f);
            CreateRuinBlock("Tuong Hau Gay B", structure, terrain, centerX + 3.5f, centerZ + 7.1f, -3f,
                new Vector3(3.1f, 1.05f, 0.75f), mossStone, 0.28f);

            // A simple altar and weathered stone stele make the ruin read as a shrine.
            CreateRuinBlock("Be Tho Da", structure, terrain, centerX, centerZ + 4.8f, 0f,
                new Vector3(3.2f, 1.15f, 1.55f), oldStone, 0.55f);
            CreateRuinBlock("Mat Be Tho", structure, terrain, centerX, centerZ + 4.7f, 0f,
                new Vector3(3.8f, 0.28f, 1.9f), mossStone, 1.72f);
            CreateRuinBlock("Bia Da Cu", structure, terrain, centerX + 3.2f, centerZ + 3.6f, -7f,
                new Vector3(1.15f, 2.65f, 0.42f), oldStone, 0.55f);

            // Scattered fallen laterite blocks soften the silhouette and imply collapse.
            CreateRuinBlock("Gach Da Ong Do 01", structure, terrain, centerX - 5.2f, centerZ - 1.2f, 23f,
                new Vector3(1.5f, 0.55f, 0.8f), laterite);
            CreateRuinBlock("Gach Da Ong Do 02", structure, terrain, centerX + 5.0f, centerZ - 2.1f, -31f,
                new Vector3(1.8f, 0.48f, 0.9f), mossStone);
            CreateRuinBlock("Gach Da Ong Do 03", structure, terrain, centerX - 1.8f, centerZ + 7.9f, 14f,
                new Vector3(1.2f, 0.42f, 0.75f), laterite);
        }

        private static Material CreateOrUpdateRuinMaterial(string name, Color color, float smoothness)
        {
            var path = GeneratedMaterialsFolder + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader was not found.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateRuinBlock(
            string name,
            Transform parent,
            Terrain terrain,
            float x,
            float z,
            float yaw,
            Vector3 size,
            Material material,
            float verticalOffset = 0f)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            block.transform.localScale = size;
            var ground = GroundHeight(terrain, x, z);
            block.transform.position = new Vector3(x, ground + size.y * 0.5f + verticalOffset, z);
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            block.isStatic = true;
            return block;
        }

        private static void BuildGameplayMarkers(Terrain terrain, Transform parent)
        {
            CreateMarker("ZONE_Continuous_Rice_Fields", parent, terrain, 48f, 85f, 88f, 166f);
            CreateMarker("ZONE_Boundary_Road", parent, terrain, RoadCenterX(86f), 86f, 12f, 160f);
            CreateMarker("ZONE_Dark_Forest", parent, terrain, 178f, 85f, 104f, 160f);
            CreateMarker("ENCOUNTER_Wild_Dog", parent, terrain, RoadCenterX(97f) + 13f, 97f, 7f, 7f);
            CreateMarker("ENCOUNTER_Bee_Hive", parent, terrain, RoadCenterX(116f) + 18f, 116f, 7f, 7f);
            CreateMarker("POI_Forgotten_Ruins", parent, terrain, 198f, 119f, 28f, 25f);
            CreateMarker("PLAYER_SPAWN_Suggested", parent, terrain, 54f, 79f, 2f, 2f);
        }

        private static void CreateMarker(string name, Transform parent, Terrain terrain, float x, float z, float width, float depth)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(x, GroundHeight(terrain, x, z), z);
            marker.transform.localScale = new Vector3(width, 1f, depth);
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not load prefab: " + path);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate prefab: " + path);
            }

            instance.name = name;
            instance.transform.SetParent(parent, true);
            return instance;
        }

        private static GameObject PlaceGrounded(
            string path,
            Transform parent,
            Terrain terrain,
            float x,
            float z,
            float yaw,
            float targetHeight,
            string name)
        {
            var instance = InstantiatePrefab(path, parent, name);
            instance.transform.position = new Vector3(x, 0f, z);
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (TryGetRendererBounds(instance, out var initialBounds) && initialBounds.size.y > 0.001f)
            {
                var factor = Mathf.Clamp(targetHeight / initialBounds.size.y, 0.03f, 20f);
                instance.transform.localScale *= factor;
            }

            var ground = GroundHeight(terrain, x, z);
            if (TryGetRendererBounds(instance, out var finalBounds))
            {
                instance.transform.position += Vector3.up * (ground - finalBounds.min.y);
            }
            else
            {
                instance.transform.position = new Vector3(x, ground, z);
            }

            return instance;
        }

        private static bool TryGetRendererBounds(GameObject gameObject, out Bounds bounds)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static float GroundHeight(Terrain terrain, float x, float z)
        {
            return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        }

        private static void ConvertPlacedMaterialsToUrp(GameObject generatedRoot)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader was not found.");
            }

            var converted = new Dictionary<Material, Material>();
            foreach (var renderer in generatedRoot.GetComponentsInChildren<Renderer>(true))
            {
                var sourceMaterials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < sourceMaterials.Length; i++)
                {
                    var source = sourceMaterials[i];
                    if (source == null || IsAlreadyUrpMaterial(source))
                    {
                        continue;
                    }

                    if (!converted.TryGetValue(source, out var replacement))
                    {
                        replacement = CreateOrUpdateUrpMaterialCopy(source, urpLit);
                        converted[source] = replacement;
                    }

                    sourceMaterials[i] = replacement;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = sourceMaterials;
                }
            }
        }

        private static bool IsAlreadyUrpMaterial(Material material)
        {
            if (material.shader == null)
            {
                return false;
            }

            var shaderName = material.shader.name;
            return shaderName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                   shaderName.IndexOf("Rice Foliage URP", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Material CreateOrUpdateUrpMaterialCopy(Material source, Shader urpLit)
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var guid = string.IsNullOrEmpty(sourcePath) ? "embedded" : AssetDatabase.AssetPathToGUID(sourcePath);
            var suffix = guid.Length >= 8 ? guid.Substring(0, 8) : guid;
            var safeName = SanitizeFileName(source.name) + "_" + suffix + "_URP";
            var outputPath = GeneratedMaterialsFolder + "/" + safeName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
            if (material == null)
            {
                material = new Material(urpLit) { name = safeName };
                AssetDatabase.CreateAsset(material, outputPath);
            }
            else
            {
                material.shader = urpLit;
            }

            var baseMap = GetFirstTexture(source, "_BaseMap", "_MainTex");
            var normalMap = GetFirstTexture(source, "_BumpMap", "_NormalMap");
            var color = GetFirstColor(source, Color.white, "_BaseColor", "_Color");
            var lowerName = source.name.ToLowerInvariant();
            var sourceShaderName = source.shader != null ? source.shader.name.ToLowerInvariant() : string.Empty;
            var cutout = lowerName.Contains("leav") || lowerName.Contains("grass") ||
                         lowerName.Contains("flower") || lowerName.Contains("fern") ||
                         lowerName.Contains("plant") || sourceShaderName.Contains("cutout") ||
                         sourceShaderName.Contains("leaves");

            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", source.HasProperty("_BumpScale") ? source.GetFloat("_BumpScale") : 1f);
            material.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0f);
            material.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0.18f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.enableInstancing = true;

            if (normalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            if (cutout)
            {
                var cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.4f;
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", Mathf.Clamp(cutoff, 0.25f, 0.65f));
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                material.doubleSidedGI = true;
            }
            else
            {
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_Cull", (float)CullMode.Back);
                material.DisableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
                material.doubleSidedGI = false;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture GetFirstTexture(Material material, params string[] properties)
        {
            foreach (var property in properties)
            {
                if (material.HasProperty(property))
                {
                    var texture = material.GetTexture(property);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Color GetFirstColor(Material material, Color fallback, params string[] properties)
        {
            foreach (var property in properties)
            {
                if (material.HasProperty(property))
                {
                    return material.GetColor(property);
                }
            }

            return fallback;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return value.Replace('/', '_').Replace('\\', '_');
        }

        private static void ConfigureSceneLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.32f, 0.38f, 0.29f, 1f);
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.53f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.38f, 0.27f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.10f, 1f);
            RenderSettings.reflectionIntensity = 0.65f;

            var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light.type == LightType.Directional);
            if (sun == null)
            {
                var sunObject = new GameObject("Directional Light");
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.name = "Directional Light - Warm Village Sun";
            sun.transform.rotation = Quaternion.Euler(46f, -118f, 0f);
            sun.color = new Color(1f, 0.83f, 0.61f, 1f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }

        private static void ConfigureCamera()
        {
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            var pathZ = 72f;
            var pathX = RoadCenterX(pathZ);
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            var pathGround = terrain != null ? GroundHeight(terrain, pathX, pathZ) : 3f;
            var eyeHeight = 1.65f;
            camera.transform.position = new Vector3(pathX - 1.2f, pathGround + eyeHeight, pathZ);
            camera.transform.rotation = Quaternion.LookRotation(
                new Vector3(RoadCenterX(pathZ + 28f), pathGround + eyeHeight, pathZ + 28f) - camera.transform.position);
            camera.name = "Main Camera - Boundary Path Eye Level";
            camera.fieldOfView = 44f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 650f;
            camera.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
