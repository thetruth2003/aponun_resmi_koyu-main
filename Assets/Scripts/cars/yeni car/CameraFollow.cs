using UnityEngine;

/// <summary>
/// Basit arac takip kamerasini hedefin arkasinda yumusak sekilde konumlandirir.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float followSpeed = 5f;

    /// <summary>
    /// Kamerayi hedefin arkasindaki ofsete dogru kaydirir ve arac merkezine bakacak sekilde cevirir.
    /// </summary>
    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
