using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketCalligrapherScaleMatcher
{
    private const string RequestFileName = "MarketCalligrapherScaleMatcher.request";
    private const float NewCalligrapherVisualScale = 1.25f;

    [InitializeOnLoadMethod]
    private static void MatchWhenRequested()
    {
        string requestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + RequestFileName));
        if (!File.Exists(requestPath)) return;

        File.Delete(requestPath);
        EditorApplication.delayCall += () =>
        {
            try { MatchAndSave(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/Tet Is Home/Match Calligrapher Sizes")]
    public static void MatchAndSave()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("There is no loaded scene to update.");

        List<GameObject> calligraphers = FindCalligrapherRoots(scene);
        if (calligraphers.Count < 2)
            throw new InvalidOperationException(
                $"Expected at least two calligraphers in {scene.name}, but found {calligraphers.Count}.");

        var measured = calligraphers
            .Select(root => new { Root = root, Bounds = CalculateRendererBounds(root) })
            .Where(item => item.Bounds.size.y > 0.001f)
            .OrderByDescending(item => item.Bounds.size.y)
            .ToList();

        if (measured.Count < 2)
            throw new InvalidOperationException("Unity could not measure both calligrapher models.");

        var reference = measured.FirstOrDefault(item =>
            !item.Root.name.EndsWith("(1)", StringComparison.OrdinalIgnoreCase));
        if (reference == null)
            reference = measured[0];

        float targetHeight = reference.Bounds.size.y * NewCalligrapherVisualScale;
        int resizedCount = 0;

        foreach (var item in measured.Where(item => item.Root != reference.Root))
        {
            float difference = Mathf.Abs(item.Bounds.size.y - targetHeight);
            if (difference <= targetHeight * 0.005f) continue;

            float originalBottom = item.Bounds.min.y;
            float factor = targetHeight / item.Bounds.size.y;
            Undo.RecordObject(item.Root.transform, "Match Calligrapher Size");
            item.Root.transform.localScale *= factor;

            Bounds resizedBounds = CalculateRendererBounds(item.Root);
            Vector3 position = item.Root.transform.position;
            position.y += originalBottom - resizedBounds.min.y;
            item.Root.transform.position = position;
            EditorUtility.SetDirty(item.Root.transform);
            resizedCount++;
        }

        if (resizedCount == 0)
        {
            Debug.Log("[MarketCalligrapherScaleMatcher] The new calligrapher already has the compensated visual size.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unity could not save the scene after matching calligrapher sizes.");

        Debug.Log($"[MarketCalligrapherScaleMatcher] Enlarged {resizedCount} new calligrapher(s) to {targetHeight:F2} m and kept their feet grounded.");
    }

    private static List<GameObject> FindCalligrapherRoots(Scene scene)
    {
        HashSet<GameObject> roots = new();

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.scene != scene || !gameObject.scene.IsValid()) continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (root == null || root.scene != scene) continue;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            bool pathMatches = assetPath.IndexOf("calligrapher", StringComparison.OrdinalIgnoreCase) >= 0;
            bool nameMatches = root.name.IndexOf("calligrapher", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               root.name.IndexOf("ong do", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               root.name.IndexOf("ông đồ", StringComparison.OrdinalIgnoreCase) >= 0;

            if (pathMatches || nameMatches)
                roots.Add(root);
        }

        return roots.ToList();
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
