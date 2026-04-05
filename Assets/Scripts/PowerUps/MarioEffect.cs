using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Mario")]
public class MarioEffect : PowerUpEffect
{
    [SerializeField] private GameObject marioModePrefab;
    [SerializeField] private Material wireframeMaterial;
    [SerializeField] private float buildDuration = 1f;
    private GameObject marioModeInstance;
    private bool timerStarted;
    private Ball ball;
    private Renderer[] paddleRenderers;
    private Collider[] paddleColliders;

    [SerializeField]private AudioClip marioThemeAudioClip;

    public static MarioEffect Active { get; private set; }

    public override void Apply(PowerUpEffectHandler handler)
    {   

        AudioManager.Instance.PlayMusic(marioThemeAudioClip);
        effectHandler = handler;

        if (marioModePrefab == null) return;

        // Find and deactivate the ball
        ball = Object.FindFirstObjectByType<Ball>();
        if (ball != null)
            ball.gameObject.SetActive(false);

        // Disable paddle components but keep GameObject active for the effect handler timer
        Paddle paddle = handler.Paddle;
        paddle.enabled = false;
        paddleColliders = paddle.GetComponentsInChildren<Collider>();
        foreach (var c in paddleColliders)
            c.enabled = false;
        paddleRenderers = paddle.GetComponentsInChildren<Renderer>();
        foreach (var r in paddleRenderers)
            r.enabled = false;

        // Instantiate Mario mode with wireframe build-in
        timerStarted = false;
        Active = this;
        marioModeInstance = Instantiate(marioModePrefab);

            // Disable Mario until wireframe + pipe animation finish
        var mario = marioModeInstance.GetComponentInChildren<Mario>();
        if (mario != null)
        {
            mario.enabled = false;
            mario.gameObject.SetActive(false);
        }

        var wireframe = marioModeInstance.AddComponent<WireframeBuildUp>();
        wireframe.Configure(wireframeMaterial, buildDuration);
        wireframe.StartBuildIn(() =>
        {
            var pipe = marioModeInstance.GetComponentInChildren<PipeEntrance>();
            if (pipe != null && mario != null)
            {
                mario.gameObject.SetActive(true);
                pipe.Rise(mario.transform, () => mario.enabled = true);
            }
            else if (mario != null)
            {
                mario.gameObject.SetActive(true);
                mario.enabled = true;
            }
        });
    }

    public void StartTimer() => timerStarted = true;

    public override void Execute()
    {
        if (timerStarted)
            base.Execute();
    }

    public override void Remove()
    {
        if (marioModeInstance != null)
        {
            // Build-out the spawned steps
            var questionBlock = marioModeInstance.GetComponentInChildren<QuestionBlock>();
            if (questionBlock != null)
            {
                foreach (var step in questionBlock.SpawnedSteps)
                {
                    if (step == null) continue;
                    var stepWireframe = step.AddComponent<WireframeBuildUp>();
                    stepWireframe.Configure(wireframeMaterial, buildDuration);
                    GameObject s = step;
                    stepWireframe.StartBuildOut(() => Destroy(s));
                }
                questionBlock.SpawnedSteps.Clear();
            }

            // Disable Mario input immediately
            var mario = marioModeInstance.GetComponentInChildren<Mario>();
            if (mario != null)
                mario.enabled = false;

            // Fresh wireframe for build-out so it captures Mario's renderers
            var oldWireframe = marioModeInstance.GetComponent<WireframeBuildUp>();
            if (oldWireframe != null)
                Destroy(oldWireframe);

            var wireframe = marioModeInstance.AddComponent<WireframeBuildUp>();
            wireframe.Configure(wireframeMaterial, buildDuration);

            GameObject instance = marioModeInstance;
            Paddle paddle = effectHandler?.Paddle;
            Collider[] cols = paddleColliders;
            Renderer[] rends = paddleRenderers;
            Ball b = ball;

            wireframe.StartBuildOut(() =>
            {
                Destroy(instance);
                RestorePaddleAndBall(paddle, cols, rends, b);
            });
        }
        else
        {
            RestorePaddleAndBall(effectHandler?.Paddle, paddleColliders, paddleRenderers, ball);
        }

        Active = null;
        marioModeInstance = null;
        ball = null;
        paddleRenderers = null;
        paddleColliders = null;
        base.Remove();
        AudioManager.Instance.PlayIngameMusic();
    }

    private static void RestorePaddleAndBall(Paddle paddle, Collider[] cols, Renderer[] rends, Ball b)
    {
        if (paddle != null)
        {
            paddle.enabled = true;
            if (cols != null)
                foreach (var c in cols)
                    c.enabled = true;
            if (rends != null)
                foreach (var r in rends)
                    r.enabled = true;
        }

        if (b != null)
        {
            b.gameObject.SetActive(true);
            b.ResetBall();
        }
    }
}
