using System;
using System.Collections.Generic;
using RiceFoliage;
using UnityEngine;
using UnityEngine.Rendering;

namespace TetIsHome.Environment
{
    /// <summary>
    /// Draws a rice field inside an arbitrary polygon using GPU instancing.
    /// This keeps irregular Vietnamese paddy shapes inexpensive at runtime.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Tet Is Home/Environment/Suburban Rice Patch Renderer")]
    public sealed class SuburbanRicePatchRenderer : MonoBehaviour
    {
        private const int MaxBatchSize = 1023;

        [SerializeField] private RiceFoliageLibrary library;
        [SerializeField, Min(0)] private int stageIndex = 4;
        [SerializeField] private Vector2[] polygon = Array.Empty<Vector2>();
        [SerializeField, Min(0.2f)] private float spacing = 0.72f;
        [SerializeField, Range(0f, 0.45f)] private float jitter = 0.28f;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.88f, 1.12f);
        [SerializeField] private int seed = 1337;
        [SerializeField] private Terrain terrain;
        [SerializeField] private ShadowCastingMode castShadows = ShadowCastingMode.Off;
        [SerializeField] private bool receiveShadows = true;

        private readonly List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
        private int cachedHash;

        public void Configure(
            RiceFoliageLibrary foliageLibrary,
            int growthStage,
            Vector2[] localPolygon,
            float plantSpacing,
            int randomSeed,
            Terrain ground)
        {
            library = foliageLibrary;
            stageIndex = Mathf.Max(0, growthStage);
            polygon = localPolygon ?? Array.Empty<Vector2>();
            spacing = Mathf.Max(0.2f, plantSpacing);
            seed = randomSeed;
            terrain = ground;
            Rebuild();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering -= Render;
            RenderPipelineManager.beginCameraRendering += Render;
            Rebuild();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Render;
        }

        private void OnValidate()
        {
            spacing = Mathf.Max(0.2f, spacing);
            if (scaleRange.x > scaleRange.y)
            {
                scaleRange = new Vector2(scaleRange.y, scaleRange.x);
            }

            Rebuild();
        }

        private void Update()
        {
            var hash = ComputeHash();
            if (hash != cachedHash)
            {
                Rebuild();
            }
        }

        private void Render(ScriptableRenderContext context, Camera camera)
        {
            var stage = library != null ? library.GetStage(stageIndex) : null;
            if (stage?.mesh == null || stage.material == null)
            {
                return;
            }

            foreach (var batch in batches)
            {
                Graphics.DrawMeshInstanced(
                    stage.mesh,
                    0,
                    stage.material,
                    batch,
                    batch.Length,
                    null,
                    castShadows,
                    receiveShadows,
                    gameObject.layer,
                    camera,
                    LightProbeUsage.Off,
                    null);
            }
        }

        [ContextMenu("Rebuild Rice Patch")]
        public void Rebuild()
        {
            batches.Clear();
            cachedHash = ComputeHash();
            if (library == null || polygon == null || polygon.Length < 3)
            {
                return;
            }

            GetBounds(out var minimum, out var maximum);
            var random = new System.Random(seed);
            var matrices = new List<Matrix4x4>();

            for (var z = minimum.y; z <= maximum.y; z += spacing)
            {
                for (var x = minimum.x; x <= maximum.x; x += spacing)
                {
                    var offset = new Vector2(
                        ((float)random.NextDouble() * 2f - 1f) * spacing * jitter,
                        ((float)random.NextDouble() * 2f - 1f) * spacing * jitter);
                    var point = new Vector2(x, z) + offset;
                    if (!ContainsPoint(point) || DistanceToBoundary(point) < 0.62f)
                    {
                        continue;
                    }

                    var world = transform.TransformPoint(new Vector3(point.x, 0f, point.y));
                    if (terrain != null)
                    {
                        world.y = terrain.SampleHeight(world) + terrain.transform.position.y + 0.025f;
                    }

                    var yaw = (float)random.NextDouble() * 360f;
                    var scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
                    matrices.Add(Matrix4x4.TRS(world, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale));
                }
            }

            for (var start = 0; start < matrices.Count; start += MaxBatchSize)
            {
                var count = Mathf.Min(MaxBatchSize, matrices.Count - start);
                var batch = new Matrix4x4[count];
                matrices.CopyTo(start, batch, 0, count);
                batches.Add(batch);
            }
        }

        private bool ContainsPoint(Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
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

        private float DistanceToBoundary(Vector2 point)
        {
            var minimum = float.MaxValue;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var segment = b - a;
                var lengthSquared = segment.sqrMagnitude;
                var distance = lengthSquared < 0.0001f
                    ? Vector2.Distance(point, a)
                    : Vector2.Distance(
                        point,
                        a + segment * Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared));
                minimum = Mathf.Min(minimum, distance);
            }

            return minimum;
        }

        private void GetBounds(out Vector2 minimum, out Vector2 maximum)
        {
            minimum = polygon[0];
            maximum = polygon[0];
            for (var i = 1; i < polygon.Length; i++)
            {
                minimum = Vector2.Min(minimum, polygon[i]);
                maximum = Vector2.Max(maximum, polygon[i]);
            }
        }

        private int ComputeHash()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (library != null ? library.GetInstanceID() : 0);
                hash = hash * 31 + stageIndex;
                hash = hash * 31 + spacing.GetHashCode();
                hash = hash * 31 + jitter.GetHashCode();
                hash = hash * 31 + scaleRange.GetHashCode();
                hash = hash * 31 + seed;
                hash = hash * 31 + transform.localToWorldMatrix.GetHashCode();
                hash = hash * 31 + (terrain != null ? terrain.GetInstanceID() : 0);
                if (polygon != null)
                {
                    foreach (var point in polygon)
                    {
                        hash = hash * 31 + point.GetHashCode();
                    }
                }

                return hash;
            }
        }
    }
}
