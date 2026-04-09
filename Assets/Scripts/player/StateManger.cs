using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// gamestate sinifi, oyuncu tarafindaki ilgili davranis veya veriyi yonetir.
/// </summary>
public enum gamestate
{
    player,
    Car
}

/// <summary>
/// Oyuncunun yaya ve arac durumlari arasindaki gecisi yonetir.
/// </summary>
public class StateManger : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 100f;
    public LayerMask interactableLayer;
    public GameObject player;
    public GameObject Speedometer;
    public static StateManger Instance;
    public GameObject car;
    public gamestate state;
    public GameObject stamina;

    [Header("Arabaya binince KAPANACAKLAR (HUD vb.)")]
    [SerializeField] private List<GameObject> closeWhenInCar = new();

    private Behaviour _activeDriveScript;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (Speedometer) Speedometer.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && state == gamestate.player)
        {
            EnterCar();
        }
        else if (Input.GetKeyDown(KeyCode.E) && state == gamestate.Car)
        {
            ExitCar();
        }
    }

    private void EnterCar()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer)) return;
        if (!hit.collider.CompareTag("Car")) return;

        GameObject root = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.transform.root.gameObject;
        CarEnterable enterable = root.GetComponentInParent<CarEnterable>() ?? root.GetComponentInChildren<CarEnterable>(true);
        if (!enterable)
        {
            Debug.LogWarning($"[StateManger] CarEnterable yok: {root.name}");
            return;
        }

        bool ok = enterable.Enter(player, playerCamera);
        if (!ok) return;

        car = root;
        if (player) player.SetActive(false);
        state = gamestate.Car;
        if (Speedometer) Speedometer.SetActive(true);
        if (stamina) stamina.SetActive(false);
        SetActiveList(closeWhenInCar, false);
    }

    private void ExitCar()
    {
        if (!car) return;

        CarEnterable enterable = car.GetComponentInParent<CarEnterable>() ?? car.GetComponentInChildren<CarEnterable>(true);
        if (enterable) enterable.Exit();

        state = gamestate.player;
        if (player) player.SetActive(true);
        if (Speedometer) Speedometer.SetActive(false);
        if (stamina) stamina.SetActive(true);
        SetActiveList(closeWhenInCar, true);

        car = null;
    }

    private Behaviour PickDriveScript(GameObject root)
    {
        VehicleController vc = root.GetComponent<VehicleController>();
        if (vc) return vc;

        CarController cc = root.GetComponent<CarController>();
        if (cc) return cc;

        return null;
    }

    private void SetActiveList(List<GameObject> list, bool active)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go) go.SetActive(active);
        }
    }
}
