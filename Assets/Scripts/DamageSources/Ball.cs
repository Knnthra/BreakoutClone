using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : MonoBehaviour, IDamageSource
{
    /// <summary>
    /// Movement speed of the ball in units per second.
    /// </summary>
    [field: SerializeField] public float Speed { get; set; }

    /// <summary>
    /// Default upward direction the ball launches in from the paddle.
    /// </summary>
    private Vector3 launchDirection = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// Reference to the paddle transform, used to follow it before launch.
    /// </summary>
    [SerializeField] private Transform paddle;

    /// <summary>
    /// Minimum bounce angle from horizontal in degrees, prevents too-flat bounces.
    /// </summary>
    [SerializeField][Range(10f, 60f)] private float minAngle;

    /// <summary>
    /// Maximum bounce angle from horizontal in degrees, prevents too-steep bounces.
    /// </summary>
    [SerializeField][Range(60f, 89f)] private float maxAngle;

    /// <summary>
    /// The ball's rigidbody used for physics movement.
    /// </summary>
    [SerializeField] private Rigidbody rigidBody;

    /// <summary>
    /// Sound played when the ball hits the paddle.
    /// </summary>
    [SerializeField] private AudioClip paddleHitAudioClip;

    /// <summary>
    /// Input action for launching the ball from the paddle.
    /// </summary>
    private InputAction launchAction;

    /// <summary>
    /// True once launched, false while sitting on the paddle.
    /// </summary>
    private bool isLaunched;

    /// <summary>
    /// Positional offset from paddle to ball, used to keep the ball on the paddle before launch.
    /// </summary>
    private Vector3 paddleOffset;

    private void OnEnable()
    {
        launchAction = new InputAction("Launch", InputActionType.Button);
        launchAction.AddBinding("<Keyboard>/space");
        launchAction.Enable();
    }

    private void OnDisable()
    {
        launchAction.Disable();
        launchAction.Dispose();
    }

    private void Start()
    {
        Initializ();
    }

    private void Update()
    {
        if (!isLaunched)
        {
            transform.position = paddle.position + paddleOffset;

            if (launchAction.WasPressedThisFrame())
                Launch();

            return;
        }
    }

    private void FixedUpdate()
    {
        if (!isLaunched) return;

        CalculateLinearVelocity();
    }

    /// <summary>
    /// Initializes the ball position based on the paddle offset
    /// </summary>
    private void Initializ()
    {
        paddleOffset = transform.position - paddle.position;
        isLaunched = false;
    }

    /// <summary>
    /// Clamps the ball's velocity angle and enforces constant speed.
    /// </summary>
    private void CalculateLinearVelocity()
    {
        Vector3 velocity = rigidBody.linearVelocity;

        //How steep is the angle
        float angleFromHorizontal = Mathf.Atan2(Mathf.Abs(velocity.y), Mathf.Abs(velocity.x)) * Mathf.Rad2Deg;

        if (angleFromHorizontal < minAngle) //Are we traveling too horizontally?
        {
            velocity = SetVelocityToMinAngle(velocity);
        }
        else if (angleFromHorizontal > maxAngle) //Are we traveling too vertically?
        {
            velocity = SetVelocityToMaxAngle(velocity);
        }

        //Make sure we keep a constant speed
        rigidBody.linearVelocity = velocity.normalized * Speed;
    }

    /// <summary>
    /// Redirects velocity to the minimum allowed angle, preventing too-flat travel.
    /// </summary>
    /// <param name="velocity">Current travel direction; its sign is preserved while the angle is overridden.</param>
    /// <returns>New velocity aimed at exactly the minimum allowed angle.</returns>
    private Vector3 SetVelocityToMinAngle(Vector3 velocity)
    {
        float dirY = velocity.y >= 0f ? 1f : -1f;

        float rad = minAngle * Mathf.Deg2Rad;
        velocity.x = Mathf.Cos(rad) * Mathf.Sign(velocity.x);
        velocity.y = Mathf.Sin(rad) * dirY;

        return velocity;
    }

    /// <summary>
    /// Redirects velocity to the maximum allowed angle, preventing too-steep travel.
    /// </summary>
    /// <param name="velocity">Current travel direction; its sign is preserved while the angle is overridden.</param>
    /// <returns>New velocity aimed at exactly the maximum allowed angle.</returns>
    private Vector3 SetVelocityToMaxAngle(Vector3 velocity)
    {
        float dirX = velocity.x >= 0f ? 1f : -1f;
        float dirY = velocity.y >= 0f ? 1f : -1f;
        float rad = maxAngle * Mathf.Deg2Rad;
        velocity.x = Mathf.Cos(rad) * dirX;
        velocity.y = Mathf.Sin(rad) * dirY;

        return velocity;
    }

    /// <summary>
    /// Launches the ball upward from the paddle at the configured speed.
    /// </summary>
    private void Launch()
    {
        isLaunched = true;
        rigidBody.linearVelocity = launchDirection.normalized * Speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Paddle"))
            return;

        // Override bounce direction when hitting the paddle based on where the ball lands.
        // Center = straight up, edges = angled to the side.
        if (collision.gameObject.TryGetComponent(out Paddle paddle))
        {
            float normalized = GetImpactPosition(paddle);

            float angle = CalculateBounceAngleInRadians(normalized);

            ApplyBounceVelocity(normalized, angle);

            paddle.OnBallHit();

            AudioManager.Instance.PlaySFX(paddleHitAudioClip);
        }
    }

    /// <summary>
    /// Returns the normalized impact position from -1 (left edge) to 1 (right edge) on the paddle.
    /// </summary>
    /// <param name="paddle">Provides the collider bounds used to normalize the hit offset.</param>
    /// <returns>-1 at the left edge, 0 at center, 1 at the right edge.</returns>
    private float GetImpactPosition(Paddle paddle)
    {
        float paddleHalfWidth = paddle.Collider.bounds.extents.x;
        float hitOffset = transform.position.x - paddle.transform.position.x;


        return Mathf.Clamp(hitOffset / paddleHalfWidth, -1f, 1f);
    }

    /// <summary>
    /// Converts normalized paddle impact position to a bounce angle in radians.
    /// </summary>
    /// <param name="normalized">Paddle hit position: 0 = center (90 degrees), +/-1 = edge (minAngle).</param>
    /// <returns>Bounce angle in radians, steeper at center and shallower at edges.</returns>
    private float CalculateBounceAngleInRadians(float normalized)
    {
        float angle = Mathf.Lerp(90f, minAngle, Mathf.Abs(normalized));
        return angle * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Sets the ball's velocity based on the bounce angle and paddle hit direction.
    /// </summary>
    /// <param name="normalized">Sign determines horizontal direction: negative = left, positive = right.</param>
    /// <param name="angle">Bounce angle in radians, applied with cos/sin to build the velocity vector.</param>
    private void ApplyBounceVelocity(float normalized, float angle)
    {
        // Build velocity: negative normalized = leftward, positive = rightward
        float dirX = normalized >= 0f ? 1f : -1f;
        rigidBody.linearVelocity = new Vector3(Mathf.Cos(angle) * dirX, Mathf.Sin(angle), 0f).normalized * Speed;
    }

    /// <summary>
    /// Resets the ball back onto the paddle, ready to be launched again.
    /// </summary>
    public void ResetBall()
    {
        isLaunched = false;
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.MovePosition(paddle.position + paddleOffset);
    }

    /// <summary>
    /// Always returns true -- the ball damages anything it collides with.
    /// </summary>
    /// <param name="collision">Unused; the ball damages everything regardless of contact details.</param>
    /// <returns>Always true since the ball has no conditional damage logic.</returns>
    public bool CanDamage(Collision collision)
    {
        return true;
    }

}
