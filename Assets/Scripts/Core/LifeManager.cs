using System;
using System.Collections.Generic;
using UnityEngine;

public class LifeManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance accessible from anywhere.
    /// </summary>
    public static LifeManager Instance { get; private set; }

    /// <summary>
    /// UI parent transform that holds the life icons.
    /// </summary>
    [SerializeField] private RectTransform parent;

    /// <summary>
    /// Prefab instantiated for each life icon in the UI.
    /// </summary>
    [SerializeField] private GameObject prefab;

    /// <summary>
    /// Starting number of lives.
    /// </summary>
    [SerializeField] private int lives;

    /// <summary>
    /// Invoked when all lives are lost.
    /// </summary>
    public Action OnGameOver;

    /// <summary>
    /// Stack of life icon GameObjects for easy removal.
    /// </summary>
    private Stack<GameObject> lifeContainer = new Stack<GameObject>();

    private void Awake()
    {
        Instance = this;

        CreateLives();

    }

    /// <summary>
    /// Creates a visual UI hearth for each life the player has
    /// </summary>
    private void CreateLives()
    {
        for (int i = 0; i < lives; i++)
        {
            lifeContainer.Push(Instantiate(prefab, parent));
        }
    }

    /// <summary>
    /// Removes one life and its UI icon. Invokes OnGameOver if no lives remain.
    /// </summary>
    public void RemoveLife()
    {
        if (lifeContainer.Count > 0)
        {
            lives--;
            Destroy(lifeContainer.Pop());
        }
        if(lives <= 0)
        {
            OnGameOver?.Invoke();
        }
    }
}
