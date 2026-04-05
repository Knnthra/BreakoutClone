using UnityEngine;

// Shared camera utility methods. Works with both perspective and orthographic cameras.
public static class CameraUtils
{
    public static float HalfWidth(float worldZ)
    {
        Camera cam = Camera.main;

        if (cam.orthographic)
            return cam.orthographicSize * cam.aspect;

        float zDist = Mathf.Abs(worldZ - cam.transform.position.z);
        float halfHeight = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * zDist;
        return halfHeight * cam.aspect;
    }

    public static float HalfHeight(float worldZ)
    {
         Camera cam = Camera.main;
        if (cam.orthographic)
            return cam.orthographicSize;

        float zDist = Mathf.Abs(worldZ - cam.transform.position.z);
        return Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * zDist;
    }

    /// <summary>
    /// Calculates horizontal movement bounds so an object of a given half-width stays on screen
    /// </summary>
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
