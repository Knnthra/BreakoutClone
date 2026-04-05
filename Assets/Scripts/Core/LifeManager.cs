using System;
using System.Collections.Generic;
using UnityEngine;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance { get; private set; }

    [SerializeField] private RectTransform parent;

    [SerializeField] private GameObject prefab;

    [SerializeField] private int lifes;

    public Action OnGameOver;

    private Stack<GameObject> lifeContainer = new Stack<GameObject>();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < lifes; i++)
        {
            lifeContainer.Push(Instantiate(prefab, parent));
        }
    }

    public void RemoveLife()
    {
        if (lifeContainer.Count > 0)
        {
            lifes--;
            Destroy(lifeContainer.Pop());
        }
        if(lifes <= 0)
        {
            OnGameOver?.Invoke();
        }
    }
}