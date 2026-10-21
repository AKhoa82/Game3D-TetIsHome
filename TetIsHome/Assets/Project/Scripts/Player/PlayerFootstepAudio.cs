using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

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
        int index = Random.Range(0, footstepClips.Length);

        // Han che lap lai cung mot am thanh
        if (footstepClips.Length > 1 && index == lastClipIndex)
        {
            index = (index + 1) % footstepClips.Length;
        }

        lastClipIndex = index;

        AudioClip clip = footstepClips[index];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(
            minPitch,
            maxPitch
        );

        audioSource.PlayOneShot(clip, volume);
    }
}