using System.Collections.Generic;
using UnityEngine;

public class PowerUpEffectHandler : MonoBehaviour
{
    /// <summary>
    /// Singleton instance accessible from anywhere.
    /// </summary>
    public static PowerUpEffectHandler Instance { get; private set; }

    /// <summary>
    /// Currently active power-up effects being ticked each frame.
    /// </summary>
    private List<PowerUpEffect> powerUpEffects = new List<PowerUpEffect>();

    /// <summary>
    /// Child transform used as the parent for visual power-up attachments.
    /// </summary>
    [field: SerializeField] public Transform VisualTransform { get; set; }

    /// <summary>
    /// Reference to the paddle, assigned in the inspector.
    /// </summary>
    [field: SerializeField] public Paddle Paddle { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Applies a power-up effect, removing any existing effect of the same type first.
    /// </summary>
    /// <param name="powerUpEffect">Initialized and activated; any existing effect of the same type is removed first.</param>
    public void Apply(PowerUpEffect powerUpEffect)
    {
        if(powerUpEffect == null)
            return;

        for (int i = powerUpEffects.Count - 1; i >= 0; i--)
        {
            if (powerUpEffects[i].GetType() == powerUpEffect.GetType())
            {
                powerUpEffects[i].Remove();
                break;
            }
        }

        powerUpEffect.Initialize();
        powerUpEffect.Apply(this);
        powerUpEffects.Add(powerUpEffect);
    }

    private void Update()
    {
        for (int i = powerUpEffects.Count - 1; i >= 0; i--)
        {
            powerUpEffects[i].Execute();
        }
    }

    /// <summary>
    /// Removes a completed or expired effect from the active list.
    /// </summary>
    /// <param name="effect">Stopped ticking and dropped from the active list.</param>
    public void Remove(PowerUpEffect effect)
    {
        powerUpEffects.Remove(effect);
    }
}
