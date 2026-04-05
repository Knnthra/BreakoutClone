using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Paddle : MonoBehaviour
{
    [field: SerializeField] public float Speed { get; set; } = 15f;
    [field: SerializeField] public Collider Collider { get; private set; }

    // Negative value lets the paddle overlap the invisible wall, preventing perspective gaps

    [SerializeField] private float paddingFromEdge = -0.5f;
    [SerializeField] private float moveTiltAngle = 10f;
    [SerializeField] private float tiltSmoothing = 8f;
    [SerializeField] private Transform visual;

    public Action BallHit;

    private Rigidbody rb;
    private InputAction moveAction;
    private float minX;
    private float maxX;
    private float moveInput;
    private float currentTilt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        moveAction.Dispose();
    }

    private void Start()
    {
        float paddleHalfWidth = GetComponent<Collider>().bounds.extents.x;

        (minX, maxX) = CameraUtils.HorizontalBounds(transform.position.z, paddleHalfWidth, paddingFromEdge);
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        moveInput = moveAction.ReadValue<float>();

        // Smoothly tilt the visual child toward the move direction, eases back to 0 when idle
        if (visual != null)
        {
            float targetTilt = -moveInput * moveTiltAngle;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothing);
            visual.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.IsGameOver) return;

        Vector3 pos = rb.position;
        pos.x += moveInput * Speed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.MovePosition(pos);
    }

    public void OnBallHit()
    {
        BallHit?.Invoke();
    }
}
