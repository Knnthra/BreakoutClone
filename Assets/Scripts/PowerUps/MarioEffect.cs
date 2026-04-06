using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Mario")]
public class MarioEffect : PowerUpEffect
{
    /// <summary>
    /// Prefab containing the full Mario mode setup (pipe, platforms, Mario character).
    /// </summary>
    [SerializeField] private GameObject marioModePrefab;

    /// <summary>
    /// Material used for wireframe build-in/out animations.
    /// </summary>
    [SerializeField] private Material wireframeMaterial;

    /// <summary>
    /// Duration of the wireframe build animation in seconds.
    /// </summary>
    [SerializeField] private float buildDuration = 1f;

    /// <summary>
    /// Spawned instance of the Mario mode prefab.
    /// </summary>
    private MarioMode marioModeInstance;

    /// <summary>
    /// True once the effect duration timer has started counting down.
    /// </summary>
    private bool timerStarted;

    /// <summary>
    /// Music clip played during Mario mode.
    /// </summary>
    [SerializeField] private AudioClip marioThemeAudioClip;

    /// <summary>
    /// The currently active MarioEffect instance, or null if not active.
    /// </summary>
    public static MarioEffect Active { get; private set; }

    /// <summary>
    /// Hides the paddle and ball, spawns Mario mode with wireframe build-in and pipe animation.
    /// </summary>
    /// <param name="handler">Provides paddle/ball access; its components are disabled during Mario mode.</param>
    public override void Apply(PowerUpEffectHandler handler)
    {
        AudioManager.Instance.PlayMusic(marioThemeAudioClip);
        effectHandler = handler;

        if (marioModePrefab == null) return;

        // Find and deactivate the ball
        GameManager.Instance.DisableBall();

        // Deactivate the paddle entirely
        handler.Paddle.gameObject.SetActive(false);

        // Instantiate Mario mode with wireframe build-in
        timerStarted = false;
        Active = this;

        marioModeInstance = Instantiate(marioModePrefab).GetComponent<MarioMode>();

        // Disable Mario until wireframe + pipe animation finish
        marioModeInstance.DisableMario();

        marioModeInstance.WireFrameBuildUp.Configure(wireframeMaterial, buildDuration);

        marioModeInstance.WireFrameBuildUp.StartBuildIn(() =>
        {
            if (marioModeInstance.Mario == null) return;


            if (marioModeInstance.PipeEntrance != null)
            {
                marioModeInstance.ShowMario();
                marioModeInstance.PipeEntrance.Rise(marioModeInstance.Mario.transform, () => marioModeInstance.EnableMario());
            }
            else
            {   
                marioModeInstance.ShowMario();
                marioModeInstance.EnableMario();
            }
        });
    }

    /// <summary>
    /// Begins counting down the effect duration. Called after Mario is fully set up.
    /// </summary>
    public void StartTimer() => timerStarted = true;

    /// <summary>
    /// Only ticks the base timer after StartTimer has been called.
    /// </summary>
    public override void Execute()
    {
        if (timerStarted)
            base.Execute();
    }

    /// <summary>
    /// Disables Mario, builds out all spawned objects, and restores the paddle and ball.
    /// </summary>
    public override void Remove()
    {
        if (marioModeInstance != null)
        {
            // Build-out the spawned steps
            if (marioModeInstance.QuestionBlock != null)
            {
                foreach (var step in marioModeInstance.QuestionBlock.SpawnedSteps)
                {
                    if (step == null) continue;
                    var stepWireframe = step.AddComponent<WireframeBuildUp>();
                    stepWireframe.Configure(wireframeMaterial, buildDuration);
                    GameObject s = step;
                    stepWireframe.StartBuildOut(() => Destroy(s));
                }
                marioModeInstance.QuestionBlock.SpawnedSteps.Clear();
            }

            // Disable Mario input immediately
            marioModeInstance.DisableMario();

            // Reset wireframe so it re-scans renderers (now including Mario)
            marioModeInstance.WireFrameBuildUp.Reset();

            marioModeInstance.WireFrameBuildUp.Configure(wireframeMaterial, buildDuration);
            
            GameObject instance = marioModeInstance.gameObject;

            marioModeInstance.WireFrameBuildUp.StartBuildOut(() =>
            {
                Destroy(instance);
                RestorePaddleAndBall(effectHandler?.Paddle);
            });
        }
        else
        {
            RestorePaddleAndBall(effectHandler?.Paddle);
        }

        Active = null;
        marioModeInstance = null;
        base.Remove();
        AudioManager.Instance.PlayIngameMusic();
    }

    /// <summary>
    /// Re-enables the paddle and resets the ball.
    /// </summary>
    /// <param name="paddle">Reactivated so the player can resume playing.</param>
    private static void RestorePaddleAndBall(Paddle paddle)
    {
        if (paddle != null)
            paddle.gameObject.SetActive(true);

        GameManager.Instance.EnableAndResetBall();
    }
}
