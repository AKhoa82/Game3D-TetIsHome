using System;
using UnityEngine;

[Serializable]
public class ThirdPersonCamera
{
    [Header("Third Person Settings")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float height = 1.6f;
    [SerializeField] private float smoothSpeed = 12f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionPadding = 0.1f;

    private Transform target;
    private Camera playerCamera;

    private float originalFOV;
    private float originalNearClip;

    public void Initialize(Transform player, Camera camera)
    {
        target = player;
        playerCamera = camera;

        originalFOV = camera.fieldOfView;
        originalNearClip = camera.nearClipPlane;
    }

    public void UpdateView(
        float yaw,
        float pitch,
        bool instant = false)
    {
        if (target == null || playerCamera == null)
            return;

        RestoreCameraSettings();

        Quaternion rotation =
            Quaternion.Euler(pitch, yaw, 0f);

        Vector3 focusPoint =
            target.position + Vector3.up * height;

        Vector3 cameraDirection =
            -(rotation * Vector3.forward);

        float targetDistance = distance;

        // Kiem tra vat can giua nhan vat va camera
        bool isBlocked = Physics.SphereCast(
            focusPoint,
            collisionRadius,
            cameraDirection,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        // Dua camera lai gan neu gap vat can
        if (isBlocked)
        {
            targetDistance = Mathf.Max(
                0.02f,
                hit.distance - collisionPadding
            );
        }

        Vector3 desiredPosition =
            focusPoint + cameraDirection * targetDistance;

        // Tien gan nhanh khi gap vat can
        // Lui ra muot khi het vat can
        if (instant || isBlocked)
        {
            playerCamera.transform.position = desiredPosition;
        }
        else
        {
            float lerpFactor =
                1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

            playerCamera.transform.position = Vector3.Lerp(
                playerCamera.transform.position,
                desiredPosition,
                lerpFactor
            );
        }

        playerCamera.transform.rotation =
            Quaternion.LookRotation(
                focusPoint - playerCamera.transform.position,
                Vector3.up
            );
    }

    public void RestoreCameraSettings()
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView = originalFOV;
        playerCamera.nearClipPlane = originalNearClip;
    }
}