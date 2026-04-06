using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance accessible from anywhere.
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// GameObject shown when the player loses all lives.
    /// </summary>
    [SerializeField] private GameObject gameOverText;

    /// <summary>
    /// GameObject shown when all bricks are cleared.
    /// </summary>
    [SerializeField] private GameObject youWinText;

    /// <summary>
    /// Post-processing volume used for the grayscale effect on game end.
    /// </summary>
    [SerializeField] private Volume globalVolume;

    /// <summary>
    /// Meshes for digits 0-9 used by the score display.
    /// </summary>
    [SerializeField] private Mesh[] numberMeshes;

    /// <summary>
    /// MeshFilters for each digit slot in the score display.
    /// </summary>
    [SerializeField] private MeshFilter[] numberMeshFilters;

    /// <summary>
    /// Rotation speed in degrees per second for the score spin animation.
    /// </summary>
    [SerializeField] private float spinSpeed = 720f;

    /// <summary>
    /// A reference to the ball object
    /// </summary>
    [SerializeField]private Ball ballObject;

    [Header("Intro Camera")]

    /// <summary>
    /// The camera that flies in at the start of the game.
    /// </summary>
    [SerializeField] private Camera introCamera;

    /// <summary>
    /// World position the camera starts at before flying to (0,0,0).
    /// </summary>
    [SerializeField] private Vector3 introStartPosition;

    /// <summary>
    /// How long the fly-in takes in seconds.
    /// </summary>
    [SerializeField] private float introDuration = 2f;

    /// <summary>
    /// True once the game has ended, either by winning or losing.
    /// </summary>
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// Current player score.
    /// </summary>
    private int score = 0;

    /// <summary>
    /// Digit values from the previous frame, used to detect changes for spin animation.
    /// </summary>
    private int[] previousDigits;

    /// <summary>
    /// Accumulated spin angle per digit slot.
    /// </summary>
    private float[] spinAngles;

    /// <summary>
    /// Original rotations of each digit slot, restored after spin completes.
    /// </summary>
    private Quaternion[] originalRotations;

    /// <summary>
    /// True while any digit is still spinning.
    /// </summary>
    private bool isSpinning;

    /// <summary>
    /// Number of bricks still alive. Triggers win when it reaches zero.
    /// </summary>
    private int activeBrickCount;



    private void Awake()
    {
        Instance = this;
        InitializeScoreDisplay();
    }

    private void Start()
    {
        gameOverText.SetActive(false);
        youWinText.SetActive(false);
        LifeManager.Instance.OnGameOver += GameOver;
        AudioManager.Instance.PlayIngameMusic();

        if (introCamera != null)
            StartCoroutine(IntroCameraFlyIn());
    }

    /// <summary>
    /// Flies a separate intro camera from introStartPosition to the main camera's position,
    /// then disables it so the main camera takes over. The main camera never moves,
    /// so brick spawning and other systems that depend on it are unaffected.
    /// </summary>
    private IEnumerator IntroCameraFlyIn()
    {
        DisableBall();

        Camera mainCam = Camera.main;
        Vector3 targetPosition = mainCam.transform.position;
        Quaternion targetRotation = mainCam.transform.rotation;

        introCamera.transform.position = introStartPosition;
        introCamera.transform.rotation = targetRotation;
        introCamera.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / introDuration);
            introCamera.transform.position = Vector3.Lerp(introStartPosition, targetPosition, t);
            yield return null;
        }

        introCamera.gameObject.SetActive(false);
        EnableAndResetBall();
    }

    /// <summary>
    /// Initializes the score display so that we can make a spin effect when scoring points
    /// </summary>
    private void InitializeScoreDisplay()
    {
        int length = numberMeshFilters.Length;
        spinAngles = new float[length];
        previousDigits = new int[length];
        originalRotations = new Quaternion[length];

        for (int i = 0; i < length; i++)
        {
            originalRotations[i] = numberMeshFilters[i].transform.localRotation;
            spinAngles[i] = 360f;
        }
    }

    /// <summary>
    /// Activates the game-over text and ends the game.
    /// </summary>
    private void GameOver()
    {
        gameOverText.SetActive(true);
        EndGame();
    }

    /// <summary>
    /// Activates the you-win text and ends the game.
    /// </summary>
    private void Win()
    {
        youWinText.SetActive(true);
        EndGame();
    }

    /// <summary>
    /// Shared end-game logic: flags game over, applies grayscale, and loads the menu.
    /// </summary>
    private void EndGame()
    {
        IsGameOver = true;

        if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments.saturation.value = -100f;

        StartCoroutine(LoadMenu());
    }

    /// <summary>
    /// Waits a few seconds then loads the main menu scene.
    /// </summary>
    /// <returns>Coroutine that waits then transitions to the menu scene.</returns>
    private IEnumerator LoadMenu()
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene(0);
    }

    private void Update()
    {
        SpinNumber();
    }

    /// <summary>
    /// Makes the score number spin when the score updates
    /// </summary>
    private void SpinNumber()
    {
        if (!isSpinning) return;

        bool allDone = true;

        for (int i = 0; i < numberMeshFilters.Length; i++)
        {
            if (spinAngles[i] < 360f)
            {
                spinAngles[i] += spinSpeed * Time.deltaTime;
                numberMeshFilters[i].transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
                allDone = false;

                if (spinAngles[i] >= 360f)
                {
                    numberMeshFilters[i].transform.localRotation = originalRotations[i];
                }
            }
        }

        if (allDone)
            isSpinning = false;
    }

    /// <summary>
    /// Increments the active brick counter. Called by each brick on Start.
    /// </summary>
    public void RegisterBrick() => activeBrickCount++;

    /// <summary>
    /// Decrements the active brick counter and triggers a win if all bricks are cleared.
    /// </summary>
    public void RemoveBrick()
    {
        activeBrickCount--;

        if (activeBrickCount <= 0)
            Win();
    }

    /// <summary>
    /// Adds one point to the score and refreshes the mesh-based score display.
    /// </summary>
    public void ScorePoint()
    {
        score++;
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Updates each digit slot mesh and triggers a spin animation for changed digits.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        for (int i = 0; i < numberMeshFilters.Length; i++)
        {
            int digit = (score / (int)Mathf.Pow(10, i)) % 10;
            numberMeshFilters[i].mesh = numberMeshes[digit];

            if (digit != previousDigits[i])
            {
                spinAngles[i] = 0f;
                isSpinning = true;
            }

            previousDigits[i] = digit;
        }
    }

    /// <summary>
    /// Enables and resets the the ball
    /// </summary>
    public void EnableAndResetBall()
    {
        if (ballObject != null)
        {
            ballObject.gameObject.SetActive(true);
            ballObject.ResetBall();
        }
    }

    /// <summary>
    /// Disables the ball
    /// </summary>
    public void DisableBall()
    {
        // Find and deactivate the ball
        if (ballObject != null)
            ballObject.gameObject.SetActive(false);
    }
}
