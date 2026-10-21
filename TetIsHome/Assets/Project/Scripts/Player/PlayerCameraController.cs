using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Input")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -65f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Camera Views")]
    [SerializeField]
    private FirstPersonCamera firstPerson =
        new FirstPersonCamera();

    [SerializeField]
    private ThirdPersonCamera thirdPerson =
        new ThirdPersonCamera();

    public bool IsFirstPerson { get; private set; }

    private Camera playerCamera;

    private float yaw;
    private float pitch = 15f;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Chua gan PlayerRoot cho camera!");
            enabled = false;
            return;
        }

        if (firstPerson == null)
            firstPerson = new FirstPersonCamera();

        if (thirdPerson == null)
            thirdPerson = new ThirdPersonCamera();

        firstPerson.Initialize(target, playerCamera);
        thirdPerson.Initialize(target, playerCamera);

        yaw = target.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Mac dinh bat dau bang goc nhin thu ba
        thirdPerson.UpdateView(yaw, pitch, true);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        // Chuyen doi goc nhin bang phim V
        if (keyboard != null &&
            keyboard.vKey.wasPressedThisFrame)
        {
            IsFirstPerson = !IsFirstPerson;

            if (!IsFirstPerson)
            {
                firstPerson.RestoreHead();
            }
        }

        // Mo khoa con tro chuot
        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse != null &&
                 mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Xoay huong nhin
        if (mouse != null &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();

            yaw += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;

            pitch = Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
        }

        // Chi goi goc nhin tuong ung
        if (IsFirstPerson)
        {
            firstPerson.UpdateView(yaw, pitch);
        }
        else
        {
            thirdPerson.UpdateView(yaw, pitch);
        }
    }

    private void OnDisable()
    {
        firstPerson?.RestoreHead();
        thirdPerson?.RestoreCameraSettings();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}