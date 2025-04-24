using UnityEngine;

public class CullingManager : MonoBehaviour
{
    public Transform player;
    public GameObject[] gridCells; // Hücrelerin referansları
    public float cullingDistance = 50f;

    private CullingGroup cullingGroup;
    private BoundingSphere[] boundingSpheres;

    void Start()
    {
        InitializeCulling();
    }

    void OnDestroy()
    {
        if (cullingGroup != null)
        {
            cullingGroup.Dispose();
        }
    }

    void InitializeCulling()
    {
        int gridSize = gridCells.Length;
        boundingSpheres = new BoundingSphere[gridSize];

        for (int i = 0; i < gridSize; i++)
        {
            boundingSpheres[i] = new BoundingSphere(gridCells[i].transform.position, cullingDistance);
        }

        cullingGroup = new CullingGroup();
        cullingGroup.targetCamera = Camera.main;
        cullingGroup.SetBoundingSpheres(boundingSpheres);
        cullingGroup.SetBoundingSphereCount(gridSize);

        cullingGroup.onStateChanged = OnCullingStateChanged;
    }

    void OnCullingStateChanged(CullingGroupEvent cullingEvent)
    {
        gridCells[cullingEvent.index].SetActive(cullingEvent.isVisible);
    }
}
