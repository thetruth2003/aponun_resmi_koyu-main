using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;         // Araç
    public Vector3 offset = new Vector3(0, 5, -10);
    public float followSpeed = 5f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
