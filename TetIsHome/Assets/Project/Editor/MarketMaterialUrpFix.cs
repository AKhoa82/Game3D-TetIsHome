using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MarketMaterialUrpFix
{
    private static readonly string[] Folders =
    {
        "Assets/Project/Scenes/04_Market/Generated",
        "Assets/FantasyEnvironments/Environments"
    };

    [MenuItem("Tools/Tet Is Home/Fix Market Materials For URP")]
    public static void FixMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            throw new InvalidOperationException("URP/Lit shader was not found. Check the active render pipeline.");

        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == urpLit) continue;

            Texture baseTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : material.mainTexture;
            Vector2 textureScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 textureOffset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            Color baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : material.color;
            Texture normalTexture = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.18f;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;

            material.shader = urpLit;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetTexture("_BaseMap", baseTexture);
            material.SetTexture("_MainTex", baseTexture);
            material.SetTextureScale("_BaseMap", textureScale);
            material.SetTextureOffset("_BaseMap", textureOffset);
            material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (normalTexture != null)
            {
                material.SetTexture("_BumpMap", normalTexture);
                material.EnableKeyword("_NORMALMAP");
            }

            bool foliage = IsFoliage(path, material.name);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", 1f);
            material.SetFloat("_DstBlend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_AlphaClip", foliage ? 1f : 0f);
            material.SetFloat("_Cutoff", foliage ? 0.32f : 0.5f);
            material.SetFloat("_Cull", foliage ? 0f : 2f);
            material.SetOverrideTag("RenderType", foliage ? "TransparentCutout" : "Opaque");
            material.renderQueue = foliage ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            if (foliage) material.EnableKeyword("_ALPHATEST_ON");
            else material.DisableKeyword("_ALPHATEST_ON");

            EditorUtility.SetDirty(material);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MarketMaterialUrpFix] Converted {converted} market materials to URP/Lit.");
    }

    [InitializeOnLoadMethod]
    private static void FixWhenRequested()
    {
        string requestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../MarketMaterialUrpFix.request"));
        if (!File.Exists(requestPath)) return;
        File.Delete(requestPath);
        EditorApplication.delayCall += () =>
        {
            try { FixMaterials(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    private static bool IsFoliage(string path, string name)
    {
        string value = (path + "/" + name).ToLowerInvariant();
        return value.Contains("leaf") || value.Contains("leaves") || value.Contains("flower") ||
               value.Contains("plant") || value.Contains("grass") || value.Contains("fern") ||
               value.Contains("bush") || value.Contains("rye") || value.Contains("mushroom");
    }
}
