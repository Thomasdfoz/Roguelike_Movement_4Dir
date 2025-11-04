using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // player transform
    public Vector3 offset = new Vector3(0f, 12f, -6f); // tweak for desired top-down view
    public float followSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        // Keep camera's rotation fixed; only interpolate position
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
    }
}
