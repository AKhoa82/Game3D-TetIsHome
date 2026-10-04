using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TetIsHome.EditorTools
{
    /// <summary>
    /// Builds URP materials and a prefab for the third-party bamboo model without
    /// modifying any source image or FBX bytes. The command is safe to run again.
    /// </summary>
    public static class BambooAssetSetup
    {
        private const string SourceRoot = "Assets/ThirdParty/Assets/bamboo";
        private const string ModelPath = SourceRoot + "/BambusaVulgaris-Master.fbx";

        private const string OutputRoot = "Assets/ThirdParty/Bamboo";
        private const string MaterialsRoot = OutputRoot + "/Materials";
        private const string TexturesRoot = OutputRoot + "/Textures";
        private const string PrefabsRoot = OutputRoot + "/Prefabs";
        private const string PrefabPath = PrefabsRoot + "/BambusaVulgaris-Master.prefab";

        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const float LeafCutoff = 0.45f;

        private static readonly string[] LeafIds = { "01", "02", "03", "04" };

        [MenuItem("Tools/Tet Is Home/Setup Bamboo Asset (URP)")]
        public static void SetupFromMenu()
        {
            try
            {
                Setup();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Bamboo setup failed", exception.Message, "OK");
            }
        }

        /// <summary>Entry point used for headless verification.</summary>
        public static void SetupBatch()
        {
            try
            {
                Setup();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void Setup()
        {
            ValidateEnvironment();
            EnsureOutputFolders();

            ConfigureSourceTextureImporters();
            GenerateDerivedTextures();

            var materials = CreateOrUpdateMaterials();
            RemapModelMaterials(materials);
            CreateOrUpdatePrefab();
            ValidateGeneratedAssets(materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Bamboo Setup] Complete: 6 URP materials, 4 leaf alpha textures, " +
                "6 packed smoothness textures, FBX remaps, and prefab created at " + PrefabPath + ".");
        }

        private static void ValidateEnvironment()
        {
            if (!File.Exists(ToAbsolutePath(ModelPath)))
            {
                throw new FileNotFoundException("Bamboo FBX was not found.", ModelPath);
            }

            foreach (var path in RequiredSourceTextures())
            {
                if (!File.Exists(ToAbsolutePath(path)))
                {
                    throw new FileNotFoundException("Required bamboo texture was not found.", path);
                }
            }

            if (GraphicsSettings.currentRenderPipeline == null ||
                GraphicsSettings.currentRenderPipeline.GetType().Name.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("The active render pipeline is not URP.");
            }

            if (Shader.Find(UrpLitShaderName) == null)
            {
                throw new InvalidOperationException("URP Lit shader was not found: " + UrpLitShaderName);
            }
        }

        private static IEnumerable<string> RequiredSourceTextures()
        {
            yield return SourceTexture("Bark01", "dif");
            yield return SourceTexture("Bark01", "nml");
            yield return SourceTexture("Bark01", "gls");
            yield return SourceTexture("Branch01", "dif");
            yield return SourceTexture("Branch01", "nml");
            yield return SourceTexture("Branch01", "gls");

            foreach (var leafId in LeafIds)
            {
                yield return LeafTexture(leafId, "Front_dif");
                yield return LeafTexture(leafId, "nml");
                yield return LeafTexture(leafId, "gls");
                yield return LeafTexture(leafId, "alp");
            }
        }

        private static void EnsureOutputFolders()
        {
            EnsureFolder("Assets/ThirdParty", "Bamboo");
            EnsureFolder(OutputRoot, "Editor");
            EnsureFolder(OutputRoot, "Materials");
            EnsureFolder(OutputRoot, "Textures");
            EnsureFolder(OutputRoot, "Prefabs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void ConfigureSourceTextureImporters()
        {
            ConfigureTexture(SourceTexture("Bark01", "dif"), true, TextureImporterType.Default, false);
            ConfigureTexture(SourceTexture("Bark01", "nml"), false, TextureImporterType.NormalMap, false);
            ConfigureTexture(SourceTexture("Bark01", "gls"), false, TextureImporterType.Default, false);
            ConfigureTexture(SourceTexture("Branch01", "dif"), true, TextureImporterType.Default, false);
            ConfigureTexture(SourceTexture("Branch01", "nml"), false, TextureImporterType.NormalMap, false);
            ConfigureTexture(SourceTexture("Branch01", "gls"), false, TextureImporterType.Default, false);

            foreach (var leafId in LeafIds)
            {
                ConfigureTexture(LeafTexture(leafId, "Front_dif"), true, TextureImporterType.Default, false);
                ConfigureTexture(LeafTexture(leafId, "nml"), false, TextureImporterType.NormalMap, false);
                ConfigureTexture(LeafTexture(leafId, "gls"), false, TextureImporterType.Default, false);
                ConfigureTexture(LeafTexture(leafId, "alp"), false, TextureImporterType.Default, false);
            }
        }

        private static void ConfigureTexture(
            string assetPath,
            bool sRgb,
            TextureImporterType textureType,
            bool alphaIsTransparency)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture importer is unavailable for " + assetPath);
            }

            var changed = importer.textureType != textureType ||
                          importer.sRGBTexture != sRgb ||
                          importer.alphaIsTransparency != alphaIsTransparency;

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.alphaIsTransparency = alphaIsTransparency;

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void GenerateDerivedTextures()
        {
            GeneratePackedSmoothness(SourceTexture("Bark01", "gls"), SmoothnessPath("Bark"));
            GeneratePackedSmoothness(SourceTexture("Branch01", "gls"), SmoothnessPath("Branch"));

            foreach (var leafId in LeafIds)
            {
                GenerateBaseAlpha(
                    LeafTexture(leafId, "Front_dif"),
                    LeafTexture(leafId, "alp"),
                    LeafBaseAlphaPath(leafId));
                GeneratePackedSmoothness(LeafTexture(leafId, "gls"), SmoothnessPath("Leaf" + leafId));
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var leafId in LeafIds)
            {
                ConfigureTexture(LeafBaseAlphaPath(leafId), true, TextureImporterType.Default, false);
            }

            ConfigureTexture(SmoothnessPath("Bark"), false, TextureImporterType.Default, false);
            ConfigureTexture(SmoothnessPath("Branch"), false, TextureImporterType.Default, false);
            foreach (var leafId in LeafIds)
            {
                ConfigureTexture(SmoothnessPath("Leaf" + leafId), false, TextureImporterType.Default, false);
            }
        }

        private static void GenerateBaseAlpha(string diffusePath, string alphaPath, string outputPath)
        {
            var diffuse = LoadPng(diffusePath, false);
            var alpha = LoadPng(alphaPath, true);

            try
            {
                ValidateSameSize(diffuse, alpha, diffusePath, alphaPath);
                var colors = diffuse.GetPixels32();
                var masks = alpha.GetPixels32();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i].a = masks[i].r;
                }

                var output = new Texture2D(diffuse.width, diffuse.height, TextureFormat.RGBA32, false, false);
                try
                {
                    output.SetPixels32(colors);
                    output.Apply(false, false);
                    WriteIfChanged(outputPath, output.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(output);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(diffuse);
                UnityEngine.Object.DestroyImmediate(alpha);
            }
        }

        private static void GeneratePackedSmoothness(string glossPath, string outputPath)
        {
            var gloss = LoadPng(glossPath, true);
            try
            {
                var source = gloss.GetPixels32();
                var packed = new Color32[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    packed[i] = new Color32(0, 0, 0, source[i].r);
                }

                var output = new Texture2D(gloss.width, gloss.height, TextureFormat.RGBA32, false, true);
                try
                {
                    output.SetPixels32(packed);
                    output.Apply(false, false);
                    WriteIfChanged(outputPath, output.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(output);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gloss);
            }
        }

        private static Texture2D LoadPng(string assetPath, bool linear)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
            if (!texture.LoadImage(File.ReadAllBytes(ToAbsolutePath(assetPath)), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not decode PNG: " + assetPath);
            }

            texture.name = Path.GetFileNameWithoutExtension(assetPath);
            return texture;
        }

        private static void ValidateSameSize(Texture2D first, Texture2D second, string firstPath, string secondPath)
        {
            if (first.width != second.width || first.height != second.height)
            {
                throw new InvalidOperationException(
                    "Texture sizes do not match: " + firstPath + " and " + secondPath);
            }
        }

        private static void WriteIfChanged(string assetPath, byte[] bytes)
        {
            var absolutePath = ToAbsolutePath(assetPath);
            if (File.Exists(absolutePath) && File.ReadAllBytes(absolutePath).SequenceEqual(bytes))
            {
                return;
            }

            File.WriteAllBytes(absolutePath, bytes);
        }

        private static Dictionary<string, Material> CreateOrUpdateMaterials()
        {
            var materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bark"] = CreateOrUpdateMaterial(
                    "Bamboo_Bark",
                    SourceTexture("Bark01", "dif"),
                    SourceTexture("Bark01", "nml"),
                    SmoothnessPath("Bark"),
                    false),
                ["Branch"] = CreateOrUpdateMaterial(
                    "Bamboo_Branch",
                    SourceTexture("Branch01", "dif"),
                    SourceTexture("Branch01", "nml"),
                    SmoothnessPath("Branch"),
                    false)
            };

            foreach (var leafId in LeafIds)
            {
                materials["Leaf" + leafId] = CreateOrUpdateMaterial(
                    "Bamboo_Leaf" + leafId,
                    LeafBaseAlphaPath(leafId),
                    LeafTexture(leafId, "nml"),
                    SmoothnessPath("Leaf" + leafId),
                    true);
            }

            return materials;
        }

        private static Material CreateOrUpdateMaterial(
            string materialName,
            string baseMapPath,
            string normalMapPath,
            string smoothnessMapPath,
            bool leaf)
        {
            var path = MaterialsRoot + "/" + materialName + ".mat";
            var shader = Shader.Find(UrpLitShaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", LoadTexture(baseMapPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", LoadTexture(normalMapPath));
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", LoadTexture(smoothnessMapPath));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.SetFloat("_QueueOffset", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true;

            if (leaf)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", LeafCutoff);
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

        private static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException("Could not load texture asset: " + path);
            }

            return texture;
        }

        private static void RemapModelMaterials(IReadOnlyDictionary<string, Material> materials)
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Could not load ModelImporter for " + ModelPath);
            }

            var slotNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in importer.GetExternalObjectMap())
            {
                if (pair.Key.type == typeof(Material))
                {
                    slotNames.Add(pair.Key.name);
                }
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new InvalidOperationException("Could not load imported bamboo model.");
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        var materialPath = AssetDatabase.GetAssetPath(material);
                        if (!materialPath.StartsWith(MaterialsRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            slotNames.Add(material.name);
                        }
                    }
                }
            }

            var remapped = 0;
            foreach (var slotName in slotNames)
            {
                var material = MaterialForSlot(slotName, materials);
                if (material == null)
                {
                    continue;
                }

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slotName), material);
                remapped++;
            }

            if (remapped < 6)
            {
                throw new InvalidOperationException(
                    "Expected to remap 6 bamboo material slots, but found " + remapped +
                    ". Found slots: " + string.Join(", ", slotNames.OrderBy(name => name)));
            }

            importer.SaveAndReimport();
        }

        private static Material MaterialForSlot(string slotName, IReadOnlyDictionary<string, Material> materials)
        {
            var normalized = slotName.Replace("_", string.Empty).Replace(" ", string.Empty);
            if (normalized.IndexOf("Bark", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials["Bark"];
            }

            if (normalized.IndexOf("Branch", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials["Branch"];
            }

            foreach (var leafId in LeafIds)
            {
                if (normalized.IndexOf("Leaf" + leafId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return materials["Leaf" + leafId];
                }
            }

            return null;
        }

        private static void CreateOrUpdatePrefab()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate bamboo FBX.");
            }

            try
            {
                instance.name = "BambusaVulgaris-Master";
                PrefabUtility.UnpackPrefabInstance(
                    instance,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                if (PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath) == null)
                {
                    throw new InvalidOperationException("Could not save bamboo prefab at " + PrefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ValidateGeneratedAssets(IReadOnlyDictionary<string, Material> materials)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Generated bamboo prefab could not be loaded.");
            }

            var expected = new HashSet<Material>(materials.Values);
            var assigned = new HashSet<Material>();
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        throw new InvalidOperationException("The generated prefab contains an empty material slot.");
                    }

                    assigned.Add(material);
                }
            }

            if (!expected.IsSubsetOf(assigned))
            {
                var missing = expected.Where(material => !assigned.Contains(material)).Select(material => material.name);
                throw new InvalidOperationException(
                    "The prefab does not use every generated material. Missing: " + string.Join(", ", missing));
            }

            foreach (var leafId in LeafIds)
            {
                var leaf = materials["Leaf" + leafId];
                if (!leaf.IsKeywordEnabled("_ALPHATEST_ON") ||
                    leaf.GetFloat("_Cull") != (float)CullMode.Off ||
                    leaf.GetTexture("_BaseMap") == null ||
                    leaf.GetTexture("_BumpMap") == null)
                {
                    throw new InvalidOperationException("Leaf material is not configured correctly: " + leaf.name);
                }
            }
        }

        private static string SourceTexture(string part, string kind)
        {
            return SourceRoot + "/AS01_" + part + "_" + kind + "_su.png";
        }

        private static string LeafTexture(string leafId, string kind)
        {
            var separator = kind.StartsWith("Front", StringComparison.Ordinal) ||
                            kind.StartsWith("Back", StringComparison.Ordinal)
                ? string.Empty
                : "_";
            return SourceRoot + "/AS01_Leaf" + leafId + separator + kind + "_su.png";
        }

        private static string LeafBaseAlphaPath(string leafId)
        {
            return TexturesRoot + "/Bamboo_Leaf" + leafId + "_BaseAlpha.png";
        }

        private static string SmoothnessPath(string part)
        {
            return TexturesRoot + "/Bamboo_" + part + "_MetallicSmoothness.png";
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Could not resolve the Unity project root.");
            }

            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
