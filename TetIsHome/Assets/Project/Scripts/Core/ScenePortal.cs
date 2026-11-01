using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;

    private PlayerMovement player;
    private bool isLoading;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
    }

    private bool IsPlayerNear()
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(
            player.transform.position,
            transform.position);

        return distance <= interactDistance;
    }

    private void Update()
    {
        if (isLoading)
            return;

        if (!IsPlayerNear())
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.eKey.wasPressedThisFrame)
            return;

        if (!Application.CanStreamedLevelBeLoaded(
                targetSceneName))
        {
            Debug.LogError(
                "Scene chua duoc them vao Scene List: "
                + targetSceneName);
            return;
        }

        GameSession.Instance.SetNextSpawn(targetSpawnId);

        isLoading = true;

        SceneManager.LoadScene(
            targetSceneName,
            LoadSceneMode.Single);
    }

    private void OnGUI()
    {
        if (!isLoading && IsPlayerNear())
        {
            GUI.Box(
                new Rect(Screen.width / 2 - 130,
                         Screen.height - 90, 260, 35),
                "Nhan E de chuyen khu vuc");
        }
    }
}