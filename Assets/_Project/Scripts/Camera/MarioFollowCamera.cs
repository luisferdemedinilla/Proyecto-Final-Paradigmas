using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class MarioFollowCamera : MonoBehaviour
{
    const float MinimumSmoothTime = 0.01f;
    const float MinimumOrthographicSize = 1f;

    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(2, 1.8f, -25);
    [SerializeField, Min(MinimumSmoothTime)] float smoothTime = 0.18f;
    [SerializeField] bool followVertical = true;
    [SerializeField] float minimumY = 1.8f;
    [SerializeField, Min(MinimumOrthographicSize)] float orthographicSize = 4.5f;

    Vector3 velocity;
    public Transform Target => target;

    void Start()
    {
        Camera camera = GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;

        if (TryResolveTarget())
            transform.position = Desired();

        transform.rotation = Quaternion.identity;
    }

    bool TryResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return true;

        MarioPlayerController player = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (MarioPlayerController candidate in root.GetComponentsInChildren<MarioPlayerController>())
            {
                if (!candidate.isActiveAndEnabled)
                    continue;

                // A scene with several active players needs an explicit target.
                if (player != null)
                    return false;

                player = candidate;
            }
        }

        if (player == null)
            return false;

        target = player.transform;
        velocity = Vector3.zero;
        return true;
    }

    Vector3 Desired()
    {
        Vector3 desired = target.position + offset;
        desired.y = followVertical ? Mathf.Max(minimumY, desired.y) : transform.position.y;
        desired.z = offset.z;
        return desired;
    }

    void LateUpdate()
    {
        if (!TryResolveTarget())
            return;

        transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
    }
}
