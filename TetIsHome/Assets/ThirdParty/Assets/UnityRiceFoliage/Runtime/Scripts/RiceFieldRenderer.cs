using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RiceFoliage
{
    [ExecuteAlways]
    [AddComponentMenu("Rice Foliage/Rice Field Renderer")]
    public sealed class RiceFieldRenderer : MonoBehaviour
    {
        private const int MaxBatchSize = 1023;

        [SerializeField] private RiceFoliageLibrary library;
        [SerializeField, Min(0)] private int stageIndex = 4;
        [SerializeField, Min(1)] private int count = 2500;
        [SerializeField] private Vector2 fieldSize = new Vector2(25f, 25f);
        [SerializeField] private Vector2 scaleRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private int seed = 1337;
        [SerializeField] private ShadowCastingMode castShadows = ShadowCastingMode.Off;
        [SerializeField] private bool receiveShadows = true;

        private readonly List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
        private int cachedHash;

        public int StageIndex
        {
            get => stageIndex;
            set => stageIndex = Mathf.Max(0, value);
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

        private void OnValidate() => Rebuild();

        private void Update()
        {
            int hash = ComputeHash();
            if (hash != cachedHash)
                Rebuild();
        }

        private void Render(ScriptableRenderContext context, Camera camera)
        {
            int hash = ComputeHash();
            if (hash != cachedHash)
                Rebuild();

            RiceFoliageLibrary.Stage stage = library != null ? library.GetStage(stageIndex) : null;
            if (stage?.mesh == null || stage.material == null)
                return;

            foreach (Matrix4x4[] batch in batches)
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

        [ContextMenu("Rebuild Field")]
        public void Rebuild()
        {
            batches.Clear();
            cachedHash = ComputeHash();

            if (count <= 0)
                return;

            var random = new System.Random(seed);
            int remaining = count;

            while (remaining > 0)
            {
                int batchCount = Mathf.Min(MaxBatchSize, remaining);
                var matrices = new Matrix4x4[batchCount];

                for (int i = 0; i < batchCount; i++)
                {
                    float x = ((float)random.NextDouble() - 0.5f) * fieldSize.x;
                    float z = ((float)random.NextDouble() - 0.5f) * fieldSize.y;
                    float yaw = (float)random.NextDouble() * 360f;
                    float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
                    Vector3 position = transform.TransformPoint(new Vector3(x, 0f, z));
                    Quaternion rotation = transform.rotation * Quaternion.Euler(0f, yaw, 0f);
                    matrices[i] = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
                }

                batches.Add(matrices);
                remaining -= batchCount;
            }
        }

        private int ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + count;
                hash = hash * 31 + seed;
                hash = hash * 31 + fieldSize.GetHashCode();
                hash = hash * 31 + scaleRange.GetHashCode();
                hash = hash * 31 + transform.position.GetHashCode();
                hash = hash * 31 + transform.rotation.GetHashCode();
                return hash;
            }
        }
    }
}
