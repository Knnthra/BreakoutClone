using System.Collections.Generic;
using UnityEngine;

public class PowerUpEffectHandler : MonoBehaviour
{
    private List<PowerUpEffect> powerUpEffects = new List<PowerUpEffect>();

    [field: SerializeField] public Transform VisualTransform { get; set; }

    public Paddle Paddle { get; private set; }

    private void Awake()
    {
        Paddle = GetComponent<Paddle>();
    }

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

    public void Remove(PowerUpEffect effect)
    {
        powerUpEffects.Remove(effect);
    }
}
