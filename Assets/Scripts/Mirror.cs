using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mirror : MonoBehaviour
{
    public Camera mainCamera;
    public Camera mirrorCamera;
    public RenderTexture target;
    public Transform mirrorSurface; // forward'ı ayna yüzeyine dik olmalı (odaya doğru)

    void OnEnable()
    {
        if (mirrorCamera && target) mirrorCamera.targetTexture = target;
    }

    void LateUpdate()
    {
        if (!mainCamera || !mirrorCamera || !mirrorSurface) return;

        // Optik parametreleri eşitle
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
        mirrorCamera.aspect = (float)target.width / target.height;
        mirrorCamera.nearClipPlane = 0.05f;
        mirrorCamera.farClipPlane = mainCamera.farClipPlane;
        mirrorCamera.orthographic = false;

        // Düzlem (ayna)
        Vector3 p = mirrorSurface.position;
        Vector3 n = mirrorSurface.forward.normalized; // ayna yüzeyine dik

        // Konumu yansıt
        Vector3 camPos = mainCamera.transform.position;
        float dist = Vector3.Dot(camPos - p, n);
        Vector3 reflPos = camPos - 2f * dist * n;

        // Yönleri yansıt
        Vector3 fwd = Vector3.Reflect(mainCamera.transform.forward, n);
        Vector3 up  = Vector3.Reflect(mainCamera.transform.up,      n);
        mirrorCamera.transform.SetPositionAndRotation(reflPos, Quaternion.LookRotation(fwd, up));

        // Oblique clip plane: aynanın arkasını kırp
        Vector4 planeWorld = new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, p));
        Vector4 planeCam = CameraSpacePlane(mirrorCamera, planeWorld);
        mirrorCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(planeCam);
    }

    static Vector4 CameraSpacePlane(Camera cam, Vector4 planeWorld)
    {
        Matrix4x4 view = cam.worldToCameraMatrix;
        Vector3 n = new Vector3(planeWorld.x, planeWorld.y, planeWorld.z);
        float d  = planeWorld.w;
        Vector3 cpn = view.MultiplyVector(n).normalized;
        float cpd = d + Vector3.Dot(cpn, view.MultiplyPoint(Vector3.zero));
        return new Vector4(cpn.x, cpn.y, cpn.z, cpd);
    }
}
