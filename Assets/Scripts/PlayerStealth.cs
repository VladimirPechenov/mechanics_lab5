using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerStealth : MonoBehaviour
{
    [Header("Noise")]
    [SerializeField] private float runNoise = 1f;
    [SerializeField] private float walkNoise = 0.55f;
    [SerializeField] private float crouchNoise = 0.16f;
    [SerializeField] private float noiseEmitInterval = 0.18f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.85f;
    [SerializeField] private float crouchingHeight = 1.05f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private CharacterController controller;
    private Transform cameraRoot;
    private Vector3 standingCameraLocalPosition;
    private float nextNoiseTime;

    public bool IsCrouching { get; private set; }
    public float CurrentNoise { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraRoot = GetComponentInChildren<Camera>()?.transform;

        if (cameraRoot != null)
        {
            standingCameraLocalPosition = cameraRoot.localPosition;
        }

        controller.height = standingHeight;
    }

    public void SetCrouching(bool isCrouching)
    {
        IsCrouching = isCrouching;
    }

    public void UpdateStealth(float normalizedMovementSpeed, bool isRunning)
    {
        UpdateCrouchPose();
        UpdateNoise(normalizedMovementSpeed, isRunning);
    }

    private void UpdateCrouchPose()
    {
        var targetHeight = IsCrouching ? crouchingHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        if (cameraRoot == null)
        {
            return;
        }

        var crouchOffset = standingHeight - crouchingHeight;
        var targetCameraPosition = IsCrouching
            ? standingCameraLocalPosition + Vector3.down * crouchOffset
            : standingCameraLocalPosition;
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetCameraPosition, crouchTransitionSpeed * Time.deltaTime);
    }

    private void UpdateNoise(float normalizedMovementSpeed, bool isRunning)
    {
        if (normalizedMovementSpeed < 0.05f)
        {
            CurrentNoise = 0f;
            return;
        }

        if (IsCrouching)
        {
            CurrentNoise = crouchNoise;
        }
        else
        {
            CurrentNoise = isRunning ? runNoise : walkNoise;
        }

        if (Time.time < nextNoiseTime)
        {
            return;
        }

        NoiseManager.EmitNoise(transform.position, CurrentNoise);
        nextNoiseTime = Time.time + noiseEmitInterval;
    }
}
