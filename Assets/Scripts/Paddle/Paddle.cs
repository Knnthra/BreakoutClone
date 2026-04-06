using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Paddle : MonoBehaviour
{
    /// <summary>
    /// Movement speed of the paddle in units per second.
    /// </summary>
    [field: SerializeField] public float Speed { get; set; } = 15f;

    /// <summary>
    /// The paddle's collider, used for ball bounce detection.
    /// </summary>
    [field: SerializeField] public Collider Collider { get; private set; }

    /// <summary>
    /// Horizontal padding from the screen edge. Negative allows wall overlap.
    /// </summary>
    [SerializeField] private float paddingFromEdge = -0.5f;

    /// <summary>
    /// Maximum tilt angle in degrees when the paddle is moving.
    /// </summary>
    [SerializeField] private float moveTiltAngle = 10f;

    /// <summary>
    /// How quickly the tilt lerps toward the target angle.
    /// </summary>
    [SerializeField] private float tiltSmoothing = 8f;

    /// <summary>
    /// Child transform that receives the tilt animation.
    /// </summary>
    [SerializeField] private Transform visual;

    /// <summary>
    /// Reference to the ball. Paddle movement is blocked while the ball is inactive.
    /// </summary>
    [SerializeField] private Ball ball;

    /// <summary>
    /// Invoked when the ball hits the paddle.
    /// </summary>
    public Action BallHit;

    /// <summary>
    /// Rigidbody used for kinematic movement.
    /// </summary>
    private Rigidbody rb;

    /// <summary>
    /// Input action for horizontal paddle movement.
    /// </summary>
    private InputAction moveAction;

    /// <summary>
    /// Left horizontal bound.
    /// </summary>
    private float minX;

    /// <summary>
    /// Right horizontal bound.
    /// </summary>
    private float maxX;

    /// <summary>
    /// Current frame's horizontal input value (-1 to 1).
    /// </summary>
    private float moveInput;

    /// <summary>
    /// Current smoothed tilt angle.
    /// </summary>
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
        if (ball != null && !ball.gameObject.activeInHierarchy) return;

        moveInput = moveAction.ReadValue<float>();

        TiltPaddle();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.IsGameOver) return;
        if (ball != null && !ball.gameObject.activeInHierarchy) return;

        Move();
    }

    /// <summary>
    /// Tilt the paddle towards the moving direction
    /// </summary>
    private void TiltPaddle()
    {
        if (visual != null)
        {
            float targetTilt = -moveInput * moveTiltAngle;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothing);
            visual.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
        }
    }

    /// <summary>
    /// Moves the paddle based on moveinput
    /// </summary>
    private void Move()
    {
        Vector3 pos = rb.position;
        pos.x += moveInput * Speed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.MovePosition(pos);
    }

    /// <summary>
    /// Invokes the BallHit callback. Called by the ball on paddle collision.
    /// </summary>
    public void OnBallHit()
    {
        BallHit?.Invoke();
    }
}
