using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private Volume globalVolume;

    [SerializeField] private Mesh[] numberMeshes;
    [SerializeField] private MeshFilter[] numberMeshFilters;
    [SerializeField] private float spinSpeed = 720f;

    public bool IsGameOver { get; private set; }

    private int score = 0;
    private int[] previousDigits;
    private float[] spinAngles;
    private Quaternion[] originalRotations;
    private bool isSpinning;


    private void Awake()
    {
          Instance = this;
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

    private void Start()
    {
        gameOverText.SetActive(false);
        LifeManager.Instance.OnGameOver += GameOver;
    }

    private void GameOver()
    {
        gameOverText.SetActive(true);

        IsGameOver = true;

        if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments.saturation.value = -100f;

        StartCoroutine(LoadMenu());
    }

    private IEnumerator LoadMenu()
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene(0);
    }

    private void Update()
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

    public void ScorePoint()
    {
        score++;
        UpdateScoreDisplay();
    }

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
}
