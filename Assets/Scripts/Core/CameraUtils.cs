using UnityEngine;

public static class CameraUtils
{
    /// <summary>
    /// Returns half the visible width at the given world Z depth.
    /// </summary>
    /// <param name="worldZ">Depth in world space at which to measure the visible width.</param>
    /// <returns>Half the camera's visible width in world units at that depth.</returns>
    public static float HalfWidth(float worldZ)
    {
        Camera cam = Camera.main;

        if (cam.orthographic)
            return cam.orthographicSize * cam.aspect;

        float zDist = Mathf.Abs(worldZ - cam.transform.position.z);
        float halfHeight = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * zDist;
        return halfHeight * cam.aspect;
    }

    /// <summary>
    /// Returns half the visible height at the given world Z depth.
    /// </summary>
    /// <param name="worldZ">Depth in world space at which to measure the visible height.</param>
    /// <returns>Half the camera's visible height in world units at that depth.</returns>
    public static float HalfHeight(float worldZ)
    {
         Camera cam = Camera.main;
        if (cam.orthographic)
            return cam.orthographicSize;

        float zDist = Mathf.Abs(worldZ - cam.transform.position.z);
        return Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * zDist;
    }

    /// <summary>
    /// Calculates horizontal min/max bounds so an object of a given half-width stays on screen.
    /// </summary>
    /// <param name="worldZ">Depth in world space at which to calculate bounds.</param>
    /// <param name="objectHalfWidth">Half the object's width, inset so it doesn't clip the screen edge.</param>
    /// <param name="padding">Extra inset from the screen edge; negative values allow overlap.</param>
    /// <returns>A tuple of (minX, maxX) the object can move between while staying on screen.</returns>
    public static (float minX, float maxX) HorizontalBounds(float worldZ, float objectHalfWidth, float padding = 0f)
    {
        Camera cam = Camera.main;
        float halfWidth = HalfWidth(worldZ);
        float camX = cam.transform.position.x;

        float minX = camX - halfWidth + objectHalfWidth + padding;
        float maxX = camX + halfWidth - objectHalfWidth - padding;

        return (minX, maxX);
    }

}
