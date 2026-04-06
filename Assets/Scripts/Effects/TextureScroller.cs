using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    /// <summary>
    /// Renderer whose material texture offset is scrolled.
    /// </summary>
    [SerializeField] private Renderer renderer;

    /// <summary>
    /// Scroll speed in UV units per second.
    /// </summary>
    [SerializeField] private float scrollSpeed = 1f;

    /// <summary>
    /// Instanced material to avoid modifying shared assets.
    /// </summary>
    private Material material;

    private void Start()
    {
        material = renderer.material;
    }

    private void Update()
    {
        ScrollVertical();
    }

    /// <summary>
    /// Makes the texture scroll vertically
    /// </summary>
    private void ScrollVertical()
    {
        float offset = Time.time * scrollSpeed;
        material.SetTextureOffset("_BaseMap", new Vector2(0f, offset));
    }
}
