using UnityEngine;

namespace RiceFoliage
{
    [ExecuteAlways]
    [AddComponentMenu("Rice Foliage/Rice Wind Controller")]
    public sealed class RiceWindController : MonoBehaviour
    {
        private static readonly int WindDirectionId = Shader.PropertyToID("_RiceGlobalWindDirection");
        private static readonly int WindParametersId = Shader.PropertyToID("_RiceGlobalWindParameters");

        [SerializeField] private Vector2 direction = new Vector2(1f, 0.35f);
        [SerializeField, Min(0f)] private float strength = 0.08f;
        [SerializeField, Min(0f)] private float speed = 1.5f;
        [SerializeField, Min(0.01f)] private float scale = 1.2f;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();
        private void Update() => Apply();

        private void Apply()
        {
            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Shader.SetGlobalVector(WindDirectionId, new Vector4(normalized.x, normalized.y, 0f, 0f));
            Shader.SetGlobalVector(WindParametersId, new Vector4(strength, speed, scale, 1f));
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(WindParametersId, Vector4.zero);
        }
    }
}
