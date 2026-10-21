using System;
using UnityEngine;

[Serializable]
public class ThirdPersonCamera
{
    [Header("Third Person Settings")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float height = 1.6f;
    [SerializeField] private float smoothSpeed = 12f;

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

        Vector3 desiredPosition =
            focusPoint - rotation * Vector3.forward * distance;

        if (instant)
        {
            playerCamera.transform.position = desiredPosition;
        }
        else
        {
            float lerpFactor = 1f - Mathf.Exp(
                -smoothSpeed * Time.deltaTime
            );

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