using UnityEngine;

/// <summary>
/// Follow + clamp kamera. Mode baru memakai ortho target tetap agar kamera
/// benar-benar mengikuti pemain; mode lama (tanpa ortho target) tetap fit peta.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Batas peta (unit dunia)")]
    [SerializeField] private Vector2 mapSize = new Vector2(13.58f, 11.58f);
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private float targetOrthoSize;

    [Header("Rasa kamera")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float focusYOffset = 0.35f;
    [SerializeField] private float cameraZ = -10f;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Bind(Transform followTarget, Vector2 size, Vector2 center = default)
    {
        target = followTarget;
        mapSize = size;
        mapCenter = center == default ? Vector2.zero : center;
        targetOrthoSize = 0f;
    }

    public void Bind(Transform followTarget, Vector2 size, Vector2 center, float orthoSize)
    {
        target = followTarget;
        mapSize = size;
        mapCenter = center == default ? Vector2.zero : center;
        targetOrthoSize = Mathf.Max(0.5f, orthoSize);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float aspect = Mathf.Max(cam.aspect, 0.01f);

        if (targetOrthoSize > 0f)
        {
            cam.orthographicSize = targetOrthoSize;
        }
        else
        {
            float sizeForWidth = mapSize.x / (2f * aspect);
            float sizeForHeight = mapSize.y * 0.5f;
            cam.orthographicSize = Mathf.Min(sizeForWidth, sizeForHeight);
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * aspect;

        Vector2 min = mapCenter - mapSize * 0.5f;
        Vector2 max = mapCenter + mapSize * 0.5f;

        Vector3 desired = new Vector3(
            target.position.x,
            target.position.y + focusYOffset,
            cameraZ);

        float x = halfWidth * 2f >= mapSize.x
            ? mapCenter.x
            : Mathf.Clamp(desired.x, min.x + halfWidth, max.x - halfWidth);

        float y = halfHeight * 2f >= mapSize.y
            ? mapCenter.y
            : Mathf.Clamp(desired.y, min.y + halfHeight, max.y - halfHeight);

        Vector3 goal = new Vector3(x, y, cameraZ);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
