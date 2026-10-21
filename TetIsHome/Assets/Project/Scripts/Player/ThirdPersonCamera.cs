using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera")]
    public float distance = 4f;
    public float height = 1.6f;
    public float mouseSensitivity = 0.12f;
    public float smoothSpeed = 12f;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Chua gan PlayerRoot!");
            enabled = false;
            return;
        }

        yaw = target.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;

        transform.position =
            focus - rotation * Vector3.forward * distance;

        transform.rotation = rotation;
    }

    private void LateUpdate()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Mouse.current != null &&
                 Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Mouse.current != null &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            yaw += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, -25f, 65f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 focus = target.position + Vector3.up * height;

        Vector3 desiredPosition =
            focus - rotation * Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );

        transform.rotation = Quaternion.LookRotation(
            focus - transform.position
        );
    }
}