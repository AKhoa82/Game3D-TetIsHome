using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField]
    private string spawnId;

    public string SpawnId => spawnId;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position, 0.5f);

        Gizmos.DrawRay(
            transform.position,
            transform.forward * 1.5f);
    }
}