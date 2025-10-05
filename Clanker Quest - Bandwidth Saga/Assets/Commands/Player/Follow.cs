using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowTarget2D : MonoBehaviour
{
    public Transform target;
    [Header("Follow")]
    public Vector2 offsetXY = new Vector2(0f, 3f);   // +Y looks “further up” the map
    public float smooth = 8f;

    [Header("Zoom (Orthographic)")]
    public bool controlZoom = true;
    public float targetOrthoSize = 5.0f;  // smaller = closer
    public float zoomSmooth = 6f;

    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (!target) return;

        // follow with XY offset (keep camera’s current Z)
        var desired = (Vector3)( (Vector2)target.position + offsetXY );
        desired.z = transform.position.z;

        transform.position = Vector3.Lerp(
            transform.position, desired, 1 - Mathf.Exp(-smooth * Time.deltaTime)
        );

        // optional zoom
        if (controlZoom && cam.orthographic)
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize, targetOrthoSize, 1 - Mathf.Exp(-zoomSmooth * Time.deltaTime)
            );
    }
}
