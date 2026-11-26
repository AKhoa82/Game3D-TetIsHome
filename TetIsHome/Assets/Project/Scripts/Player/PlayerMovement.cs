using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float rotationSpeed = 12f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.3f;
    public float gravity = -20f;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private PlayerStamina playerStamina;
    private PlayerCombat playerCombat;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerStamina = GetComponent<PlayerStamina>();
        playerCombat = GetComponent<PlayerCombat>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        Vector3 direction = GetWorldDirection(input);

        bool isDodging = playerCombat != null && playerCombat.IsDodging;
        bool isBlocking = playerCombat != null && playerCombat.IsBlocking;

        if (isDodging)
        {
            direction = playerCombat.DodgeDirection;
        }

        PlayerCameraController viewCamera = cameraTransform != null
    ? cameraTransform.GetComponent<PlayerCameraController>()
    : null;

        if (viewCamera != null && viewCamera.IsFirstPerson)
        {
            // Goc nhin thu nhat: quay nhan vat theo camera
            Vector3 lookDirection = cameraTransform.forward;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    lookDirection.normalized
                );
            }
        }
        else if (direction.sqrMagnitude > 0.001f)
        {
            // Goc nhin thu ba: quay theo huong di chuyen
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && JumpPressed() && !isDodging && !isBlocking)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        bool isMoving = direction.sqrMagnitude > 0.001f;
        bool wantsToSprint = SprintPressed() && !isDodging && !isBlocking;

        bool isRunning = isMoving && wantsToSprint;

        // Kiem tra va tieu hao Stamina
        if (playerStamina != null && !isDodging && !isBlocking)
        {
            isRunning = playerStamina.TickSprint(
                wantsToSprint,
                isMoving,
                Time.deltaTime
            );
        }

        float speed = isRunning ? runSpeed : walkSpeed;

        if (isDodging)
        {
            speed = playerCombat.DodgeSpeed;
        }
        else if (isBlocking)
        {
            speed = walkSpeed * 0.5f;
        }

        // Cap nhat Animation
        if (animator != null)
        {
            float animationSpeed =
                !isMoving ? 0f :
                isRunning ? 1f : 0.5f;

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.15f,
                Time.deltaTime
            );
        }
        Vector3 movement = direction * speed;
        movement.y = verticalVelocity;

        CollisionFlags collision = controller.Move(movement * Time.deltaTime);
        if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            Vector2 keyboardInput = new Vector2(horizontal, vertical);
            if (keyboardInput.sqrMagnitude > 0f)
                return Vector2.ClampMagnitude(keyboardInput, 1f);
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null ? gamepad.leftStick.ReadValue() : Vector2.zero;
    }

    private Vector3 GetWorldDirection(Vector2 input)
    {
        if (cameraTransform == null)
            return new Vector3(input.x, 0f, input.y);

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    private static bool SprintPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            || (gamepad != null && gamepad.leftStickButton.isPressed);
    }

    private static bool JumpPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = -2f;
    }

    public Vector3 GetMoveDirection()
    {
        return GetWorldDirection(ReadMoveInput());
    }
}
