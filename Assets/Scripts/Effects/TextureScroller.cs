using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [SerializeField] private Renderer renderer;
    [SerializeField] private float scrollSpeed = 1f;

    private Material material;

    private void Start()
    {
        material = renderer.material;
    }

    private void Update()
    {
        float offset = Time.time * scrollSpeed;
        material.SetTextureOffset("_BaseMap", new Vector2(0f, offset));
    }
}
