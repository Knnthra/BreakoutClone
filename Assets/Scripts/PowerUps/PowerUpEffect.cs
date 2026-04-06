using UnityEngine;

public abstract class PowerUpEffect : ScriptableObject
{
    /// <summary>
    /// How long the effect lasts in seconds. Zero or negative means infinite.
    /// </summary>
    [SerializeField] private float duration;

    /// <summary>
    /// Seconds remaining before the effect expires.
    /// </summary>
    private float remainingTime;

    /// <summary>
    /// Reference to the handler managing this effect on the paddle.
    /// </summary>
    protected PowerUpEffectHandler effectHandler;

    /// <summary>
    /// Activates the effect on the given handler.
    /// </summary>
    /// <param name="effectHandler">Provides paddle access and manages the effect's lifecycle.</param>
    public abstract void Apply(PowerUpEffectHandler effectHandler);

    /// <summary>
    /// Resets the remaining time to the full duration.
    /// </summary>
    public void Initialize()
    {
        remainingTime = duration;
    }

    /// <summary>
    /// Ticks the timer each frame and removes the effect when time runs out.
    /// </summary>
    public virtual void Execute()
    {
        if (duration <= 0) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
            Remove();
    }

    /// <summary>
    /// Cleans up and unregisters this effect from the handler.
    /// </summary>
    public virtual void Remove()
    {
         if(effectHandler != null)
            effectHandler.Remove(this);
    }
}
