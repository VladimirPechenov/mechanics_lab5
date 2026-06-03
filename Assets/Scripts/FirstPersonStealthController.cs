using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStealth))]
public class FirstPersonStealthController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4.4f;
    [SerializeField] private float runSpeed = 7.2f;
    [SerializeField] private float crouchSpeed = 2.1f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private Transform cameraRoot;

    private CharacterController controller;
    private PlayerStealth stealth;
    private Vector3 verticalVelocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stealth = GetComponent<PlayerStealth>();

        if (cameraRoot == null)
        {
            cameraRoot = GetComponentInChildren<Camera>()?.transform;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        HandleLook();
        HandleMovement(keyboard);
    }

    private void HandleLook()
    {
        var mouse = Mouse.current;
        if (mouse == null || cameraRoot == null)
        {
            return;
        }

        var delta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * delta.x);
        pitch = Mathf.Clamp(pitch - delta.y, -75f, 75f);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement(Keyboard keyboard)
    {
        var input = Vector2.zero;
        input.x += keyboard.dKey.isPressed ? 1f : 0f;
        input.x -= keyboard.aKey.isPressed ? 1f : 0f;
        input.y += keyboard.wKey.isPressed ? 1f : 0f;
        input.y -= keyboard.sKey.isPressed ? 1f : 0f;
        input = Vector2.ClampMagnitude(input, 1f);

        var isCrouching = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed || keyboard.cKey.isPressed;
        var isRunning = keyboard.leftShiftKey.isPressed && !isCrouching && input.sqrMagnitude > 0.01f;
        stealth.SetCrouching(isCrouching);

        var speed = isCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;
        var move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -1f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);

        stealth.UpdateStealth(input.magnitude, isRunning);
    }
}
