using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mario : MonoBehaviour, IDamageSource
{

    /// <summary>
    /// A reference to Mario's meshfilter
    /// </summary>
    [SerializeField] private MeshFilter meshFilter;

    /// <summary>
    /// Array that contains all Mario's mesh animations
    /// </summary>
    [SerializeField] private MeshAnimation[] animations;

    /// <summary>
    /// A dictionary of all animations
    /// </summary>
    private Dictionary<string, MeshAnimation> dictAnimations = new Dictionary<string, MeshAnimation>();

    /// <summary>
    /// The animation we are currently playing
    /// </summary>
    private MeshAnimation currentAnimation;

    /// <summary>
    /// The current animation index
    /// </summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    /// time emaplsed since last animation frame change
    /// </summary>
    private float timeElapsed;

    /// <summary>
    /// Move action for mario
    /// </summary>
    private InputAction moveAction;

    /// <summary>
    /// Jump action for mario
    /// </summary>
    private InputAction jumpAction;

    /// <summary>
    /// Mario's move input
    /// </summary>
    private float moveInput;

    /// <summary>
    /// Min X and max X positon for preventing Mario, from running off map
    /// </summary>
    private float minX, maxX;

    /// <summary>
    /// How close Mario can walk to the dge
    /// </summary>
    [SerializeField] private float paddingFromEdge = 0;

    /// <summary>
    /// Mario's movement speed
    /// </summary>
    [SerializeField] private float speed;

    /// <summary>
    /// Mario's jumpforce
    /// </summary>
    [SerializeField] private float jumpForce = 8f;

    /// <summary>
    /// A referece to Mario's rigidbody
    /// </summary>
    [SerializeField] private Rigidbody rigidBody;

    /// <summary>
    /// What is ground
    /// </summary>
    [SerializeField] private LayerMask groundLayers;

    /// <summary>
    /// True if Mario is on the ground
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PlayAnimation("Idle");

        CreateBounds();

    }

    // Update is called once per frame
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
    /// Checks if Mario is standing on the ground
    /// </summary>
    /// <param name="collision"></param>
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
    /// Makes mario fall to the ground when he hits a block from below
    /// </summary>
    /// <param name="collision"></param>
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
    /// Makes Mario move
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 pos = rigidBody.position;
        pos.x += moveInput * speed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rigidBody.MovePosition(pos);
    }

    /// <summary>
    /// Creates all input actions for the player
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
    /// Disables input actions
    /// </summary>
    private void DisableInputActions()
    {
        moveAction.Disable();
        moveAction.Dispose();
        jumpAction.Disable();
        jumpAction.Dispose();
    }

    /// <summary>
    /// Reads moveInput and makes Mario react to it.
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
        }
    }

    /// <summary>
    /// Creates minX and maxX bounds
    /// </summary>
    private void CreateBounds()
    {
        float marioHalfWidth = GetComponent<Collider>().bounds.extents.x;

        (minX, maxX) = CameraUtils.HorizontalBounds(transform.position.z, marioHalfWidth);
    }

    /// <summary>
    /// Updates the animation
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
    /// Plays an animation based on a string
    /// </summary>
    /// <param name="animation">Animation to paly</param>
    private void PlayAnimation(string animation)
    {
        if (currentAnimation != null && currentAnimation.Name == animation)
            return;

        currentAnimation = dictAnimations[animation];
        timeElapsed = 0;
        CurrentIndex = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="collision"></param>
    /// <returns></returns>
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
    /// The animations framerate
    /// </summary>
    [field: SerializeField] public float FPS { get; set; }

    /// <summary>
    /// The meshes for this animation
    /// </summary>
    [field: SerializeField] public Mesh[] Meshes { get; set; }

    /// <summary>
    /// The animation's name
    /// </summary>
    [field: SerializeField] public string Name { get; set; }

}
