using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// Repairs external texture links supplied with imported Game FBX sources.
// Uses the FBX's imported embedded materials; creates no .mat assets.
public sealed class ResidentialSourceMaterialImporter : AssetPostprocessor
{
    const string Root="Assets/Project/Art/Environment/ResidentialArea/Models/";
    public override uint GetVersion()=>3;
    public override int GetPostprocessOrder()=>0;
    void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips) => LinkProvidedTextures(material);
    void OnPostprocessMaterial(Material material) => LinkProvidedTextures(material);
    void LinkProvidedTextures(Material material)
    {
        if(assetPath.StartsWith(Root+"ResidentialExpansion/") && assetPath.EndsWith(".fbx"))
        {
            string stem=assetPath.Substring(0,assetPath.Length-4);
            var color=AssetDatabase.LoadAssetAtPath<Texture2D>(stem+".png");
            if(!color)return;
            material.shader=Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap",color);
            material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Smoothness",.12f);
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(stem+"_normal.png");
            if(normal){material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");}
        }
        else if(assetPath==Root+"TropicalCottage/Meshy_AI_Tropical_Village_Cott_1005010039_texture.fbx")
        {
            string stem=Root+"TropicalCottage/Meshy_AI_Tropical_Village_Cott_1005010039_texture";
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(stem+".png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(stem+"_normal.png"));
            material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Smoothness",.12f);
            material.EnableKeyword("_NORMALMAP");
        }
        else if(assetPath==Root+"CoconutPalm/coconut.fbx" && material.name=="date palm leaf")
        {
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CoconutPalm/date_palm_leaf.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CoconutPalm/date_palm_leaf_n.png"));
            material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.25f);material.SetFloat("_Cull",0);
            material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            material.EnableKeyword("_ALPHATEST_ON");material.EnableKeyword("_NORMALMAP");
        }
    }
}
