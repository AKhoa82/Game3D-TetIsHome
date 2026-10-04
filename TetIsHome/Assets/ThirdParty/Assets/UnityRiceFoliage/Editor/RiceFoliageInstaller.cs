using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiceFoliage.Editor
{
    public static class RiceFoliageInstaller
    {
        private const string AssetRoot = "Assets/ThirdParty/UnityRiceFoliage";
        private const string OutputRoot = "Assets/_Project/Art/Vegetation/Rice";

        private static readonly StageDefinition[] Definitions =
        {
            new StageDefinition("01", 0.38f, 0.34f),
            new StageDefinition("02", 0.58f, 0.46f),
            new StageDefinition("03", 0.82f, 0.55f),
            new StageDefinition("04", 1.02f, 0.62f),
            new StageDefinition("06", 1.18f, 0.68f)
        };

        [MenuItem("Tools/Rice Foliage/Generate Editable Assets")]
        public static void Generate()
        {
            EnsureFolders();
            ValidateRuntimeScripts();

            string shaderPath = $"{AssetRoot}/Runtime/Shaders/RiceFoliageURP.shader";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
                throw new InvalidOperationException($"Rice URP shader was not found at {shaderPath}. Ensure URP is installed and the asset imported without shader errors.");

            var stages = new RiceFoliageLibrary.Stage[Definitions.Length];

            try
            {
                for (int i = 0; i < Definitions.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Rice Foliage", $"Generating stage {Definitions[i].Id}", i / (float)Definitions.Length);
                    stages[i] = GenerateStage(Definitions[i], shader);
                }

                RiceFoliageLibrary library = CreateOrLoadAsset<RiceFoliageLibrary>($"{OutputRoot}/RiceFoliageLibrary.asset");
                library.name = "RiceFoliageLibrary";
                library.SetStages(stages);
                EditorUtility.SetDirty(library);
                CreateDemoField(library);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = library;
                EditorGUIUtility.PingObject(library);
                Debug.Log($"Rice Foliage generated successfully in {OutputRoot}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static RiceFoliageLibrary.Stage GenerateStage(StageDefinition definition, Shader shader)
        {
            string texturePath = $"{AssetRoot}/Runtime/Textures/Stages/Rice_Stage{definition.Id}.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new InvalidOperationException($"Missing rice texture: {texturePath}");

            Mesh mesh = CreateCrossedCards($"Rice_Stage{definition.Id}_LOD0", definition.Width, definition.Height, 3);
            Mesh lodMesh = CreateCrossedCards($"Rice_Stage{definition.Id}_LOD1", definition.Width, definition.Height, 2);
            mesh = SaveOrReplaceMesh(mesh, $"{OutputRoot}/Meshes/{mesh.name}.asset");
            lodMesh = SaveOrReplaceMesh(lodMesh, $"{OutputRoot}/Meshes/{lodMesh.name}.asset");

            string materialPath = $"{OutputRoot}/Materials/Rice_Stage{definition.Id}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"Rice_Stage{definition.Id}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.name = $"Rice_Stage{definition.Id}";
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cutoff", 0.34f);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_WindStrength", Mathf.Lerp(0.035f, 0.09f, definition.Height / 1.18f));
            material.SetFloat("_WindSpeed", 1.5f);
            material.SetFloat("_WindScale", 1.2f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            string prefabPath = $"{OutputRoot}/Prefabs/Rice_Stage{definition.Id}.prefab";
            GameObject prefab = CreateStagePrefab(definition, mesh, lodMesh, material, prefabPath);

            return new RiceFoliageLibrary.Stage
            {
                name = $"Stage {definition.Id}",
                mesh = mesh,
                lodMesh = lodMesh,
                material = material,
                prefab = prefab
            };
        }

        private static GameObject CreateStagePrefab(StageDefinition definition, Mesh mesh, Mesh lodMesh, Material material, string path)
        {
            var root = new GameObject($"Rice_Stage{definition.Id}");
            try
            {
                GameObject lod0 = CreateRendererChild("LOD0", root.transform, mesh, material);
                GameObject lod1 = CreateRendererChild("LOD1", root.transform, lodMesh, material);
                var group = root.AddComponent<LODGroup>();
                group.SetLODs(new[]
                {
                    new LOD(0.42f, new Renderer[] { lod0.GetComponent<MeshRenderer>() }),
                    new LOD(0.12f, new Renderer[] { lod1.GetComponent<MeshRenderer>() })
                });
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.RecalculateBounds();
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRendererChild(string name, Transform parent, Mesh mesh, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            return child;
        }

        private static void CreateDemoField(RiceFoliageLibrary library)
        {
            var root = new GameObject("Rice_DemoField");
            try
            {
                RiceFieldRenderer renderer = root.AddComponent<RiceFieldRenderer>();
                var serialized = new SerializedObject(renderer);
                serialized.FindProperty("library").objectReferenceValue = library;
                serialized.FindProperty("stageIndex").intValue = 4;
                serialized.FindProperty("count").intValue = 2500;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<RiceWindController>();
                PrefabUtility.SaveAsPrefabAsset(root, $"{OutputRoot}/Prefabs/Rice_DemoField.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Mesh CreateCrossedCards(string name, float width, float height, int cardCount)
        {
            var vertices = new List<Vector3>(cardCount * 4);
            var normals = new List<Vector3>(cardCount * 4);
            var uvs = new List<Vector2>(cardCount * 4);
            var triangles = new List<int>(cardCount * 6);

            for (int card = 0; card < cardCount; card++)
            {
                float angle = card * Mathf.PI / cardCount;
                Vector3 horizontal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (width * 0.5f);
                Vector3 normal = Vector3.Cross(Vector3.up, horizontal).normalized;
                int start = vertices.Count;

                vertices.Add(-horizontal);
                vertices.Add(horizontal);
                vertices.Add(-horizontal + Vector3.up * height);
                vertices.Add(horizontal + Vector3.up * height);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                triangles.Add(start + 0);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
                triangles.Add(start + 1);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh SaveOrReplaceMesh(Mesh source, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                return source;
            }

            EditorUtility.CopySerialized(source, existing);
            UnityEngine.Object.DestroyImmediate(source);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolderPath(OutputRoot);
            EnsureFolderPath($"{OutputRoot}/Meshes");
            EnsureFolderPath($"{OutputRoot}/Materials");
            EnsureFolderPath($"{OutputRoot}/Prefabs");
        }

        private static void EnsureFolderPath(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);

                current = next;
            }
        }

        private static void ValidateRuntimeScripts()
        {
            ValidateRuntimeScript<RiceFoliageLibrary>("RiceFoliageLibrary.cs");
            ValidateRuntimeScript<RiceFieldRenderer>("RiceFieldRenderer.cs");
            ValidateRuntimeScript<RiceWindController>("RiceWindController.cs");
        }

        private static void ValidateRuntimeScript<T>(string fileName) where T : UnityEngine.Object
        {
            string scriptPath = $"{AssetRoot}/Runtime/Scripts/{fileName}";
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null || script.GetClass() != typeof(T))
                throw new InvalidOperationException($"Rice runtime script was not found or could not be loaded at {scriptPath}.");
        }

        private readonly struct StageDefinition
        {
            public readonly string Id;
            public readonly float Height;
            public readonly float Width;

            public StageDefinition(string id, float height, float width)
            {
                Id = id;
                Height = height;
                Width = width;
            }
        }
    }
}
