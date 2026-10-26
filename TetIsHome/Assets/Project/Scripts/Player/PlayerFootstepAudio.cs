using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Surface Footsteps")]
    [SerializeField] private AudioClip[] grassClips;
    [SerializeField] private AudioClip[] dirtClips;
    [SerializeField] private AudioClip[] stoneClips;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float raycastDistance = 1.8f;

    [Header("Footstep Settings")]
    [SerializeField] private float metersPerStep = 1.1f;
    [SerializeField] private float minimumSpeed = 0.15f;
    [SerializeField] private float volume = 0.65f;

    [Header("Sound Variation")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private CharacterController controller;
    private AudioSource audioSource;

    private Vector3 previousPosition;
    private float distanceSinceLastStep;
    private int lastClipIndex = -1;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        distanceSinceLastStep = 0f;
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - previousPosition;

        previousPosition = currentPosition;
        movement.y = 0f;

        float horizontalSpeed = new Vector3(
            controller.velocity.x,
            0f,
            controller.velocity.z
        ).magnitude;

        // Khong phat khi dung yen hoac o tren khong
        if (!controller.isGrounded ||
            horizontalSpeed < minimumSpeed ||
            footstepClips == null ||
            footstepClips.Length == 0)
        {
            distanceSinceLastStep = 0f;
            return;
        }

        // Bo qua dich chuyen tuc thoi qua xa
        if (movement.magnitude > 2f)
        {
            distanceSinceLastStep = 0f;
            return;
        }

        distanceSinceLastStep += movement.magnitude;

        float stepDistance = Mathf.Max(0.1f, metersPerStep);

        if (distanceSinceLastStep >= stepDistance)
        {
            PlayFootstep();
            distanceSinceLastStep %= stepDistance;
        }
    }

    private void PlayFootstep()
    {
        AudioClip[] clips = GetSurfaceClips();

        if (clips == null || clips.Length == 0)
            return;

        int index = Random.Range(0, clips.Length);

        if (clips.Length > 1 && index == lastClipIndex)
        {
            index = (index + 1) % clips.Length;
        }

        lastClipIndex = index;

        AudioClip clip = clips[index];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.PlayOneShot(clip, volume);
    }

    private AudioClip[] GetSurfaceClips()
    {
        Vector3 origin = transform.position + Vector3.up * 0.6f;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return footstepClips;
        }

        FootstepSurface surface = hit.collider.GetComponentInParent<FootstepSurface>();

        if (surface == null)
            return footstepClips;

        FootstepSurfaceType type = surface.GetSurface(hit.point);

        switch (type)
        {
            case FootstepSurfaceType.Grass:
                if (grassClips != null && grassClips.Length > 0)
                    return grassClips;
                break;

            case FootstepSurfaceType.Dirt:
                if (dirtClips != null && dirtClips.Length > 0)
                    return dirtClips;
                break;

            case FootstepSurfaceType.Stone:
                if (stoneClips != null && stoneClips.Length > 0)
                    return stoneClips;
                break;
        }

        return footstepClips;
    }
}