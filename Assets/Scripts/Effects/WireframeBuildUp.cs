using System;
using UnityEngine;


public class WireframeBuildUp : MonoBehaviour
{
    /// <summary>
    /// Cached shader property ID for _BuildUpProgress (0 = invisible, 1 = fully visible).
    /// </summary>
    private static readonly int BuildUpProgressID = Shader.PropertyToID("_BuildUpProgress");

    /// <summary>
    /// Cached shader property ID for _WorldMinY, the lowest Y of the combined bounds.
    /// </summary>
    private static readonly int WorldMinYID = Shader.PropertyToID("_WorldMinY");

    /// <summary>
    /// Cached shader property ID for _WorldMaxY, the highest Y of the combined bounds.
    /// </summary>
    private static readonly int WorldMaxYID = Shader.PropertyToID("_WorldMaxY");

    /// <summary>
    /// Template wireframe material instanced for each renderer slot during animation.
    /// </summary>
    private Material wireframeMaterial;

    /// <summary>
    /// Duration of the build-in or build-out animation in seconds.
    /// </summary>
    private float buildDuration = 1f;

    /// <summary>
    /// All renderers found in this object and its children.
    /// </summary>
    private Renderer[] renderers;

    /// <summary>
    /// Backup of each renderer's original materials, restored after build-in completes.
    /// </summary>
    private Material[][] originalMaterials;

    /// <summary>
    /// Instanced wireframe materials applied during the animation.
    /// </summary>
    private Material[][] wireframeMaterials;

    /// <summary>
    /// Current animation progress from 0 (invisible) to 1 (fully visible).
    /// </summary>
    private float progress;

    /// <summary>
    /// True while the build animation is actively running.
    /// </summary>
    private bool isAnimating;

    /// <summary>
    /// True for build-out (1 to 0), false for build-in (0 to 1).
    /// </summary>
    private bool isReversing;

    /// <summary>
    /// Optional callback invoked when the animation finishes.
    /// </summary>
    private Action onComplete;

    /// <summary>
    /// Sets the wireframe material template and animation duration.
    /// </summary>
    /// <param name="material">Cloned per renderer slot; the original asset stays unmodified.</param>
    /// <param name="duration">How many seconds the build-in or build-out sweep takes.</param>
    public void Configure(Material material, float duration)
    {
        wireframeMaterial = material;
        buildDuration = duration;
    }

    /// <summary>
    /// Clears cached renderers and materials so the next animation re-scans all children.
    /// </summary>
    public void Reset()
    {
        DestroyWireframeMaterials();
        renderers = null;
        originalMaterials = null;
        isAnimating = false;
    }

    /// <summary>
    /// Starts the build-in animation: wireframe appears bottom to top, then restores original materials.
    /// </summary>
    /// <param name="onComplete">Fired after progress reaches 1 and original materials are restored.</param>
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
    /// Starts the build-out animation: wireframe dissolves top to bottom, then destroys materials.
    /// </summary>
    /// <param name="onComplete">Fired after progress reaches 0 and wireframe materials are destroyed.</param>
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

    private void Update()
    {
        Animate();
    }

    /// <summary>
    /// Animates the build up or brakdown wireframe effect
    /// </summary>
    private void Animate()
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
    /// Caches all child renderers and stores copies of their current materials for restoration.
    /// </summary>
    private void CacheOriginalMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].materials;
    }

    /// <summary>
    /// Replaces every material slot with an instanced wireframe material configured with combined bounds.
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
    /// Restores the original materials on all renderers and cleans up wireframe instances.
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].materials = originalMaterials[i];

        DestroyWireframeMaterials();
    }

    /// <summary>
    /// Updates the _BuildUpProgress shader property on all wireframe material instances.
    /// </summary>
    /// <param name="t">0 = fully invisible, 1 = fully visible; written to every wireframe instance.</param>
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
    /// Calculates a bounding box encapsulating all child renderers for shader Y bounds.
    /// </summary>
    /// <returns>Enclosing bounds whose min/max Y are written to the shader for the sweep range.</returns>
    private Bounds CalculateCombinedBounds()
    {
        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    /// <summary>
    /// Destroys all instanced wireframe materials to prevent memory leaks.
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

    private void OnDestroy()
    {
        DestroyWireframeMaterials();
    }
}
