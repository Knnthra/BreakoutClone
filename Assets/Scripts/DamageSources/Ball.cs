using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : MonoBehaviour, IDamageSource
{
    /// <summary>
    /// The speed of the ball
    /// </summary>
    [field: SerializeField] public float Speed { get; set; }

    /// <summary>
    /// The default launch direction the ball will launch in when attached to the paddle
    /// </summary>
    private Vector3 launchDirection = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// A reference to the paddle
    /// </summary>
    [SerializeField] private Transform paddle;


    /// <summary>
    /// Minimum angle from horizontal in degrees, prevents the ball from bouncing too flat
    /// </summary>
    [SerializeField][Range(10f, 60f)] private float minAngle;

    /// <summary>
    /// Maximum angle from horizontal in degrees, prevents the ball from bouncing too steep
    /// </summary>
    [SerializeField][Range(60f, 89f)] private float maxAngle;

    /// <summary>
    /// The ball's rigidbody used for physics movement
    /// </summary>
    [SerializeField] private Rigidbody rigidBody;

    /// <summary>
    /// Input action for launching the ball from the paddle
    /// </summary>
    private InputAction launchAction;

    /// <summary>
    /// True once the ball has been launched, false while sitting on the paddle
    /// </summary>
    private bool isLaunched;

    /// <summary>
    /// Offset from paddle to ball at start, used to keep the ball positioned on the paddle before launch
    /// </summary>
    private Vector3 paddleOffset;

    /// <summary>
    /// Creates and enables the launch input action
    /// </summary>
    private void OnEnable()
    {
        launchAction = new InputAction("Launch", InputActionType.Button);
        launchAction.AddBinding("<Keyboard>/space");
        launchAction.Enable();
    }

    /// <summary>
    /// Disables and disposes the launch input action
    /// </summary>
    private void OnDisable()
    {
        launchAction.Disable();
        launchAction.Dispose();
    }

    /// <summary>
    /// Stores the initial offset from paddle to ball and sets the ball as not launched
    /// </summary>
    private void Start()
    {
        paddleOffset = transform.position - paddle.position;
        isLaunched = false;
    }

    /// <summary>
    /// Before launch, keeps the ball on the paddle and listens for the launch input
    /// </summary>
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

    /// <summary>
    /// Clamps the ball's angle to prevent too-flat or too-steep bounces, and enforces constant speed
    /// </summary>
    private void FixedUpdate()
    {
        if (!isLaunched) return;

        CalculateLinearVelocity();
    }

    /// <summary>
    /// Calculates the linear velocity of the ball
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
    /// Sets a the velocity to the min required angle
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
    private Vector3 SetVelocityToMinAngle(Vector3 velocity)
    {
        float dirY = velocity.y >= 0f ? 1f : -1f;

        float rad = minAngle * Mathf.Deg2Rad;
        velocity.x = Mathf.Cos(rad) * Mathf.Sign(velocity.x);
        velocity.y = Mathf.Sin(rad) * dirY;

        return velocity;
    }

    /// <summary>
    /// Sets a the velocity to the max allowed angle
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
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
    /// Launches the ball upward from the paddle at the configured speed
    /// </summary>
    private void Launch()
    {
        isLaunched = true;
        rigidBody.linearVelocity = launchDirection.normalized * Speed;
    }

    /// <summary>
    /// Overrides bounce direction when hitting the paddle based on where the ball lands
    /// </summary>
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
        }
    }

    /// <summary>
    /// Gets the normalized impact position  from -1 to 1 (left edge to right edge)
    /// </summary>
    /// <param name="paddle"></param>
    /// <returns></returns>
    private float GetImpactPosition(Paddle paddle)
    {   
        float paddleHalfWidth = paddle.Collider.bounds.extents.x;
        float hitOffset = transform.position.x - paddle.transform.position.x;


        return Mathf.Clamp(hitOffset / paddleHalfWidth, -1f, 1f);
    }

    /// <summary>
    /// Calculates the bounce angle in radians based on impact position
    /// </summary>
    /// <returns></returns>
    private float CalculateBounceAngleInRadians(float normalized)
    {
        float angle = Mathf.Lerp(90f, minAngle, Mathf.Abs(normalized));
        return angle * Mathf.Deg2Rad;
    }

    private void ApplyBounceVelocity (float normalized, float angle)
    {
        // Build velocity: negative normalized = leftward, positive = rightward
        float dirX = normalized >= 0f ? 1f : -1f;
        rigidBody.linearVelocity = new Vector3(Mathf.Cos(angle) * dirX, Mathf.Sin(angle), 0f).normalized * Speed;
    }

    /// <summary>
    /// Resets the ball back onto the paddle, ready to be launched again
    /// </summary>
    public void ResetBall()
    {
        isLaunched = false;
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.MovePosition(paddle.position + paddleOffset);
    }

    /// <summary>
    /// Always returns true since the ball damages anything it hits
    /// </summary>
    public bool CanDamage(Collision collision)
    {
        return true;
    }

}
