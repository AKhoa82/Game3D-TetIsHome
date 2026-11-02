using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketMoatToGroveConverter
{
    private const string ScenePath = "Assets/Project/Scenes/04_Market/MapCho.unity";
    private const string RequestFileName = "MarketMoatToGroveConverter.request";
    private const string GroveName = "Tet Tree Grove - Former Moat";
    private const float GroundY = 1.85f;

    private static readonly string[] TreePrefabs =
    {
        "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Deciduous_tree2.prefab",
        "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Willow_tree1.prefab",
        "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Oak_tree2.prefab"
    };

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

    [MenuItem("Tools/Tet Is Home/Replace Market Moat With Trees")]
    public static void BuildAndPlace()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Save the current scene before converting the market moat.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (terrain == null) throw new InvalidOperationException("The market terrain was not found.");

        FillFormerMoat(terrain);
        RemoveSceneObject(scene, "Moat Water");
        RemoveSceneObject(scene, "Stone Moat Banks");
        RemoveSceneObject(scene, "South Bridge");
        RemoveSceneObject(scene, "North Bridge");
        RemoveSceneObject(scene, "Bridge Plank");

        GameObject decorationRoot = GameObject.Find("05_Decoration");
        if (decorationRoot == null)
            throw new InvalidOperationException("The market decoration root was not found.");

        Transform previousGrove = decorationRoot.transform.Find(GroveName);
        if (previousGrove != null) UnityEngine.Object.DestroyImmediate(previousGrove.gameObject);

        GameObject grove = new(GroveName);
        grove.transform.SetParent(decorationRoot.transform, false);
        CreateTreeRing(scene, grove.transform, terrain);

        EditorUtility.SetDirty(terrain.terrainData);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the market after replacing the moat.");

        Debug.Log("[MarketMoatToGroveConverter] Replaced the moat with 24 perimeter trees and removed both wooden bridges.");
    }

    private static void FillFormerMoat(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);

        for (int z = 0; z < resolution; z++)
        {
            float worldZ = terrain.transform.position.z + z / (float)(resolution - 1) * data.size.z;
            for (int x = 0; x < resolution; x++)
            {
                float worldX = terrain.transform.position.x + x / (float)(resolution - 1) * data.size.x;
                float ax = Mathf.Abs(worldX);
                float az = Mathf.Abs(worldZ);
                bool formerMoat =
                    (ax >= 60f && ax <= 71f && az <= 53f) ||
                    (az >= 42f && az <= 53f && ax <= 71f);
                if (formerMoat) heights[z, x] = GroundY / data.size.y;
            }
        }

        data.SetHeights(0, 0, heights);
    }

    private static void CreateTreeRing(Scene scene, Transform parent, Terrain terrain)
    {
        Vector3[] positions =
        {
            new(-65.5f, 0f, -34f), new(-65.5f, 0f, -20f), new(-65.5f, 0f, -6f),
            new(-65.5f, 0f,   8f), new(-65.5f, 0f,  22f), new(-65.5f, 0f, 36f),
            new( 65.5f, 0f, -34f), new( 65.5f, 0f, -20f), new( 65.5f, 0f, -6f),
            new( 65.5f, 0f,   8f), new( 65.5f, 0f,  22f), new( 65.5f, 0f, 36f),
            new(-50f, 0f, -47.5f), new(-36f, 0f, -47.5f), new(-22f, 0f, -47.5f),
            new( 22f, 0f, -47.5f), new( 36f, 0f, -47.5f), new( 50f, 0f, -47.5f),
            new(-50f, 0f,  47.5f), new(-36f, 0f,  47.5f), new(-22f, 0f,  47.5f),
            new( 22f, 0f,  47.5f), new( 36f, 0f,  47.5f), new( 50f, 0f,  47.5f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabs[i % TreePrefabs.Length]);
            if (prefab == null) throw new InvalidOperationException("Missing tree prefab: " + TreePrefabs[i % TreePrefabs.Length]);

            GameObject tree = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (tree == null) throw new InvalidOperationException("Unity could not instantiate a perimeter tree.");
            tree.name = $"Tet Grove Tree {i + 1:00}";
            tree.transform.SetParent(parent, true);

            Vector3 position = positions[i];
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, (i * 47f + 19f) % 360f, 0f);

            Bounds bounds = CalculateBounds(tree);
            float targetHeight = 7.0f + (i % 4) * .65f;
            if (bounds.size.y > .001f)
                tree.transform.localScale *= targetHeight / bounds.size.y;

            bounds = CalculateBounds(tree);
            tree.transform.position += Vector3.up * (position.y - bounds.min.y);
            GameObjectUtility.SetStaticEditorFlags(tree, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void RemoveSceneObject(Scene scene, string objectName)
    {
        List<GameObject> matches = new();
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject == null) continue;
            if (gameObject.scene == scene && gameObject.name == objectName)
                matches.Add(gameObject);
        }

        foreach (GameObject gameObject in matches)
            if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
    }
}
