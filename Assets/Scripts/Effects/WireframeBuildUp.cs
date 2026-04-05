using System;
using UnityEngine;


public class WireframeBuildUp : MonoBehaviour
{
    /// <summary>
    /// Cached shader property ID for _BuildUpProgress, controls how much of the mesh is visible (0 = invisible, 1 = fully visible)
    /// </summary>
    private static readonly int BuildUpProgressID = Shader.PropertyToID("_BuildUpProgress");

    /// <summary>
    /// Cached shader property ID for _WorldMinY, the lowest Y position of the combined bounds
    /// </summary>
    private static readonly int WorldMinYID = Shader.PropertyToID("_WorldMinY");

    /// <summary>
    /// Cached shader property ID for _WorldMaxY, the highest Y position of the combined bounds
    /// </summary>
    private static readonly int WorldMaxYID = Shader.PropertyToID("_WorldMaxY");

    /// <summary>
    /// The wireframe material used as a template for all renderer slots during the animation
    /// </summary>
    private Material wireframeMaterial;

    /// <summary>
    /// How long the build-in or build-out animation takes in seconds
    /// </summary>
    private float buildDuration = 1f;

    /// <summary>
    /// All renderers found in this object and its children
    /// </summary>
    private Renderer[] renderers;

    /// <summary>
    /// Backup of each renderer's original materials, restored after build-in completes.
    /// First index = renderer, second index = material slot.
    /// </summary>
    private Material[][] originalMaterials;

    /// <summary>
    /// Instanced wireframe materials applied during the animation.
    /// First index = renderer, second index = material slot.
    /// </summary>
    private Material[][] wireframeMaterials;

    /// <summary>
    /// Current animation progress from 0 (invisible) to 1 (fully visible)
    /// </summary>
    private float progress;

    /// <summary>
    /// True while the build animation is actively running
    /// </summary>
    private bool isAnimating;

    /// <summary>
    /// True for build-out (1 to 0), false for build-in (0 to 1)
    /// </summary>
    private bool isReversing;

    /// <summary>
    /// Optional callback invoked when the animation finishes
    /// </summary>
    private Action onComplete;

    /// <summary>
    /// Sets the wireframe material template and animation duration
    /// </summary>
    public void Configure(Material material, float duration)
    {
        wireframeMaterial = material;
        buildDuration = duration;
    }

    /// <summary>
    /// Starts the build-in animation: wireframe appears from bottom to top, then restores original materials
    /// </summary>
    public void StartBuildIn(Action onComplete = null)
    {
        this.onComplete = onComplete;
        CacheOriginalMaterials();
        ApplyWireframeMaterials();
        progress = 0f;
        isReversing = false;
        isAnimating = true;
    }

    /// <summary>
    /// Starts the build-out animation: wireframe dissolves from top to bottom, then destroys wireframe materials
    /// </summary>
    public void StartBuildOut(Action onComplete = null)
    {
        this.onComplete = onComplete;

        if (originalMaterials == null)
            CacheOriginalMaterials();

        ApplyWireframeMaterials();
        progress = 1f;
        isReversing = true;
        isAnimating = true;
    }

    /// <summary>
    /// Advances the animation progress each frame and triggers completion when done
    /// </summary>
    private void Update()
    {
        if (!isAnimating)
            return;

        if (!isReversing)
        {
            progress += Time.deltaTime / buildDuration;
            progress = Mathf.Clamp01(progress);
            SetWireframeProgress(progress);

            if (progress >= 1f)
            {
                isAnimating = false;
                RestoreOriginalMaterials();
                onComplete?.Invoke();
            }
        }
        else
        {
            progress -= Time.deltaTime / buildDuration;
            progress = Mathf.Clamp01(progress);
            SetWireframeProgress(progress);

            if (progress <= 0f)
            {
                isAnimating = false;
                DestroyWireframeMaterials();
                onComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// Finds all child renderers and stores a copy of their current materials for later restoration
    /// </summary>
    private void CacheOriginalMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].materials;
    }

    /// <summary>
    /// Replaces every material slot on every renderer with an instanced wireframe material,
    /// configured with the combined bounds so the shader knows the object's vertical extent
    /// </summary>
    private void ApplyWireframeMaterials()
    {
        Bounds bounds = CalculateCombinedBounds();
        wireframeMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            int slotCount = renderers[i].materials.Length;
            wireframeMaterials[i] = new Material[slotCount];

            for (int j = 0; j < slotCount; j++)
            {
                wireframeMaterials[i][j] = new Material(wireframeMaterial);
                wireframeMaterials[i][j].SetFloat(WorldMinYID, bounds.min.y);
                wireframeMaterials[i][j].SetFloat(WorldMaxYID, bounds.max.y);
                wireframeMaterials[i][j].SetFloat(BuildUpProgressID, 0f);
            }

            renderers[i].materials = wireframeMaterials[i];
        }
    }

    /// <summary>
    /// Puts the original materials back on all renderers and cleans up wireframe material instances
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].materials = originalMaterials[i];

        DestroyWireframeMaterials();
    }

    /// <summary>
    /// Updates the _BuildUpProgress property on all wireframe material instances
    /// </summary>
    private void SetWireframeProgress(float t)
    {
        for (int i = 0; i < wireframeMaterials.Length; i++)
        {
            for (int j = 0; j < wireframeMaterials[i].Length; j++)
            {
                if (wireframeMaterials[i][j] != null)
                    wireframeMaterials[i][j].SetFloat(BuildUpProgressID, t);
            }
        }
    }

    /// <summary>
    /// Calculates a bounding box that encapsulates all child renderers,
    /// used to set _WorldMinY and _WorldMaxY so the shader knows where bottom and top are
    /// </summary>
    private Bounds CalculateCombinedBounds()
    {
        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    /// <summary>
    /// Destroys all instanced wireframe materials to prevent memory leaks
    /// </summary>
    private void DestroyWireframeMaterials()
    {
        if (wireframeMaterials == null)
            return;

        for (int i = 0; i < wireframeMaterials.Length; i++)
        {
            for (int j = 0; j < wireframeMaterials[i].Length; j++)
            {
                if (wireframeMaterials[i][j] != null)
                    Destroy(wireframeMaterials[i][j]);
            }
        }

        wireframeMaterials = null;
    }

    /// <summary>
    /// Cleans up any wireframe material instances when this component is destroyed
    /// </summary>
    private void OnDestroy()
    {
        DestroyWireframeMaterials();
    }
}
