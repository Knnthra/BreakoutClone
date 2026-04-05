using UnityEngine;

public abstract class PowerUpEffect : ScriptableObject
{
    [SerializeField] private float duration;

    private float remainingTime;

    protected PowerUpEffectHandler effectHandler;

    public abstract void Apply(PowerUpEffectHandler effectHandler);

    public void Initialize()
    {
        remainingTime = duration;
    }

    public virtual void Execute()
    {
        if (duration <= 0) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
            Remove();
    }

    public virtual void Remove()
    {
         if(effectHandler != null)
            effectHandler.Remove(this);
    }
}