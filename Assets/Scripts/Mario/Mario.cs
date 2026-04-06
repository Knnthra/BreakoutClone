using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mario : MonoBehaviour, IDamageSource
{

    /// <summary>
    /// MeshFilter used to swap meshes for frame-by-frame animation.
    /// </summary>
    [SerializeField] private MeshFilter meshFilter;

    /// <summary>
    /// Array of all mesh-based animations (Idle, Walk, Jump).
    /// </summary>
    [SerializeField] private MeshAnimation[] animations;

    /// <summary>
    /// Dictionary lookup for animations by name.
    /// </summary>
    private Dictionary<string, MeshAnimation> dictAnimations = new Dictionary<string, MeshAnimation>();

    /// <summary>
    /// The animation currently being played.
    /// </summary>
    private MeshAnimation currentAnimation;

    /// <summary>
    /// Current frame index in the active animation.
    /// </summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    /// Time elapsed since the last animation frame change.
    /// </summary>
    private float timeElapsed;

    /// <summary>
    /// Input action for horizontal movement.
    /// </summary>
    private InputAction moveAction;

    /// <summary>
    /// Input action for jumping.
    /// </summary>
    private InputAction jumpAction;

    /// <summary>
    /// Current horizontal input value (-1 to 1).
    /// </summary>
    private float moveInput;

    /// <summary>
    /// Left horizontal movement bound.
    /// </summary>
    /// <summary>
    /// Right horizontal movement bound.
    /// </summary>
    private float minX, maxX;

    /// <summary>
    /// How close Mario can walk to the screen edge.
    /// </summary>
    [SerializeField] private float paddingFromEdge = 0;

    /// <summary>
    /// Horizontal movement speed in units per second.
    /// </summary>
    [SerializeField] private float speed;

    /// <summary>
    /// Upward impulse force applied when jumping.
    /// </summary>
    [SerializeField] private float jumpForce = 8f;

    /// <summary>
    /// Sound played when Mario jumps.
    /// </summary>
    [SerializeField] private AudioClip jumpAudioClip;

    /// <summary>
    /// Mario's rigidbody for physics-based movement and jumping.
    /// </summary>
    [SerializeField] private Rigidbody rigidBody;

    /// <summary>
    /// Layer mask defining which layers count as ground for jump detection.
    /// </summary>
    [SerializeField] private LayerMask groundLayers;

    /// <summary>
    /// True when Mario is standing on a ground-layer surface.
    /// </summary>
    private bool isGrounded;

    private void OnEnable()
    {
        CreateInputActions();
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    private void Awake()
    {
        //Copy animations into a dictionary for easier access
        dictAnimations = animations.ToDictionary(a => a.Name);
    }

    private void Start()
    {
        PlayAnimation("Idle");

        CreateBounds();

    }

    private void Update()
    {
        HandleInput();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        GroundCheck(collision);

        BounceDown(collision);
    }

    /// <summary>
    /// Checks collision contacts to determine if Mario landed on a ground-layer surface.
    /// </summary>
    /// <param name="collision">Checked for upward-facing normals on a ground-layer object.</param>
    private void GroundCheck(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayers) != 0)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Zeroes vertical velocity when Mario hits something from below (head bump).
    /// </summary>
    /// <param name="collision">Checked for downward-facing normals indicating Mario hit from below.</param>
    private void BounceDown(Collision collision)
    {
                // Bounce down when hitting something from below (head bump)
        if (rigidBody.linearVelocity.y > 0)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, 0f, rigidBody.linearVelocity.z);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Moves Mario horizontally using the rigidbody, clamped to screen bounds.
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 pos = rigidBody.position;
        pos.x += moveInput * speed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rigidBody.MovePosition(pos);
    }

    /// <summary>
    /// Creates and enables the move and jump input actions.
    /// </summary>
    private void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        moveAction.Enable();

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.Enable();
    }

    /// <summary>
    /// Disables and disposes the move and jump input actions.
    /// </summary>
    private void DisableInputActions()
    {
        moveAction.Disable();
        moveAction.Dispose();
        jumpAction.Disable();
        jumpAction.Dispose();
    }

    /// <summary>
    /// Reads input, flips sprite direction, switches walk/idle animation, and handles jumping.
    /// </summary>
    private void HandleInput()
    {
        moveInput = moveAction.ReadValue<float>();

        if (moveInput < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (moveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        // Switch between Walk and Idle based on movement
        if (isGrounded)
        {
            if (Mathf.Abs(moveInput) > 0.01f)
                PlayAnimation("Walk");
            else
                PlayAnimation("Idle");
        }

        // Jump
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            PlayAnimation("Jump");
            if (jumpAudioClip != null)
                AudioManager.Instance.PlaySFX(jumpAudioClip);
        }
    }

    /// <summary>
    /// Calculates horizontal movement bounds based on Mario's collider and screen width.
    /// </summary>
    private void CreateBounds()
    {
        float marioHalfWidth = GetComponent<Collider>().bounds.extents.x;

        (minX, maxX) = CameraUtils.HorizontalBounds(transform.position.z, marioHalfWidth);
    }

    /// <summary>
    /// Advances the current animation frame based on elapsed time and FPS.
    /// </summary>
    private void UpdateAnimation()
    {
        timeElapsed += Time.deltaTime;

        CurrentIndex = (int)(timeElapsed * currentAnimation.FPS);

        if (CurrentIndex > currentAnimation.Meshes.Length - 1)
        {
            timeElapsed = 0;
            CurrentIndex = 0;
        }

        meshFilter.mesh = currentAnimation.Meshes[CurrentIndex];
    }

    /// <summary>
    /// Switches to a named animation, resetting the frame counter.
    /// </summary>
    /// <param name="animation">Key into the animation dictionary (e.g. "Idle", "Walk", "Jump").</param>
    private void PlayAnimation(string animation)
    {
        if (currentAnimation != null && currentAnimation.Name == animation)
            return;

        currentAnimation = dictAnimations[animation];
        timeElapsed = 0;
        CurrentIndex = 0;
    }

    /// <summary>
    /// Returns true only if Mario is hitting the object from above (stomping).
    /// </summary>
    /// <param name="collision">Contacts are scanned for upward normals to confirm a stomp.</param>
    /// <returns>True if Mario landed on top of the object; false for side or bottom hits.</returns>
    public bool CanDamage(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }
        return false;
    }
}

[System.Serializable]
public class MeshAnimation
{
    /// <summary>
    /// Playback speed in frames per second.
    /// </summary>
    [field: SerializeField] public float FPS { get; set; }

    /// <summary>
    /// Ordered array of meshes representing each animation frame.
    /// </summary>
    [field: SerializeField] public Mesh[] Meshes { get; set; }

    /// <summary>
    /// Name used to look up this animation (e.g. "Idle", "Walk", "Jump").
    /// </summary>
    [field: SerializeField] public string Name { get; set; }

}
