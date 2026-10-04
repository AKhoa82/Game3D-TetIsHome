using System;
using UnityEngine;

namespace RiceFoliage
{
    [CreateAssetMenu(menuName = "Rice Foliage/Library", fileName = "RiceFoliageLibrary")]
    public sealed class RiceFoliageLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Stage
        {
            public string name;
            public Mesh mesh;
            public Mesh lodMesh;
            public Material material;
            public GameObject prefab;
        }

        [SerializeField] private Stage[] stages = Array.Empty<Stage>();

        public int StageCount => stages?.Length ?? 0;
        public Stage[] Stages => stages;

        public Stage GetStage(int index)
        {
            if (StageCount == 0)
                return null;

            return stages[Mathf.Clamp(index, 0, StageCount - 1)];
        }

#if UNITY_EDITOR
        public void SetStages(Stage[] value) => stages = value;
#endif
    }
}
