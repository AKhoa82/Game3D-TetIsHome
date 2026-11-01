using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    private static GameSession instance;

    public static GameSession Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GameSession");
                instance = obj.AddComponent<GameSession>();
            }

            return instance;
        }
    }

    // Du lieu Player duoc giu xuyen Scene
    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; } = 100;

    public int MaxStamina { get; private set; } = 100;
    public int Stamina { get; private set; } = 100;

    public int Coins { get; private set; } = 0;

    // Diem xuat hien trong Scene tiep theo
    public string NextSpawnId { get; private set; } = "";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    public void TakeDamage(int amount)
    {
        Health = Mathf.Clamp(
            Health - Mathf.Max(0, amount), 0, MaxHealth);
    }

    public void Heal(int amount)
    {
        Health = Mathf.Clamp(
            Health + Mathf.Max(0, amount), 0, MaxHealth);
    }

    public void UseStamina(int amount)
    {
        Stamina = Mathf.Clamp(
            Stamina - Mathf.Max(0, amount), 0, MaxStamina);
    }

    public void RestoreStamina(int amount)
    {
        Stamina = Mathf.Clamp(
            Stamina + Mathf.Max(0, amount), 0, MaxStamina);
    }

    public void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
    }

    public void SetNextSpawn(string spawnId)
    {
        NextSpawnId = spawnId;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(NextSpawnId))
            return;

        SceneSpawnPoint targetPoint = null;

        foreach (var point in FindObjectsByType<SceneSpawnPoint>(
                     FindObjectsSortMode.None))
        {
            if (point.gameObject.scene == scene &&
                point.SpawnId == NextSpawnId)
            {
                targetPoint = point;
                break;
            }
        }

        if (targetPoint == null)
        {
            Debug.LogWarning(
                "Khong tim thay SpawnPoint: " + NextSpawnId);
            NextSpawnId = "";
            return;
        }

        PlayerMovement player = null;

        foreach (var p in FindObjectsByType<PlayerMovement>(
                     FindObjectsSortMode.None))
        {
            if (p.gameObject.scene == scene)
            {
                player = p;
                break;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("Scene moi chua co PlayerRoot!");
            NextSpawnId = "";
            return;
        }

        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(
            targetPoint.transform.position,
            targetPoint.transform.rotation);

        if (controller != null)
            controller.enabled = true;

        Debug.Log("Da chuyen den: " + NextSpawnId);

        NextSpawnId = "";
    }
}