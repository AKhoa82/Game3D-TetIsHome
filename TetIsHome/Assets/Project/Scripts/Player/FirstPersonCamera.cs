using System;
using UnityEngine;

[Serializable]
public class FirstPersonCamera
{
    [Header("Full Body Camera")]
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float eyeForwardOffset = 0.30f;
    [SerializeField] private float eyeBoneUpOffset = 0.15f;
    [SerializeField] private float firstPersonFOV = 80f;
    [SerializeField] private float nearClipPlane = 0.12f;

    [Header("Head Visibility")]
    [SerializeField] private bool hideHeadInFirstPerson = false;

    [SerializeField, Range(0.001f, 0.15f)]
    private float hiddenHeadScale = 0.01f;

    private Transform target;
    private Camera playerCamera;
    private Transform headBone;
    private Vector3 originalHeadScale;

    public void Initialize(Transform player, Camera camera)
    {
        target = player;
        playerCamera = camera;

        Animator animator =
            target.GetComponentInChildren<Animator>(true);

        if (animator != null &&
            animator.isHuman &&
            animator.avatar != null &&
            animator.avatar.isValid)
        {
            headBone = animator.GetBoneTransform(
                HumanBodyBones.Head
            );

            if (headBone != null)
            {
                originalHeadScale = headBone.localScale;
            }
        }
    }

    public void UpdateView(float yaw, float pitch)
    {
        if (target == null || playerCamera == null)
            return;

        Quaternion rotation =
            Quaternion.Euler(pitch, yaw, 0f);

        // Chi xu ly xuong dau, khong tat mesh co the
        if (headBone != null)
        {
            headBone.localScale = hideHeadInFirstPerson
                ? originalHeadScale * hiddenHeadScale
                : originalHeadScale;
        }

        Vector3 viewForward =
            Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

        Vector3 eyePosition;

        // Camera theo xuong dau khi Jump
        if (headBone != null)
        {
            eyePosition =
                headBone.position + Vector3.up * eyeBoneUpOffset;
        }
        else
        {
            eyePosition =
                target.position + Vector3.up * eyeHeight;
        }

        eyePosition += viewForward * eyeForwardOffset;

        playerCamera.fieldOfView = firstPersonFOV;
        playerCamera.nearClipPlane = nearClipPlane;

        playerCamera.transform.position = eyePosition;
        playerCamera.transform.rotation = rotation;
    }

    public void RestoreHead()
    {
        if (headBone != null)
        {
            headBone.localScale = originalHeadScale;
        }
    }
}