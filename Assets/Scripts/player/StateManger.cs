using System.Collections.Generic;
using UnityEngine;

public enum gamestate { player, Car }

public class StateManger : MonoBehaviour
{
    public Camera playerCamera;                 // Oyuncu kamerası
    public float maxDistance = 100f;            // Maksimum atış mesafesi
    public LayerMask interactableLayer;         // Etkileşim katmanı
    public GameObject player;                   // Oyuncu karakteri
    public GameObject Speedometer;
    public static StateManger Instance;
    public GameObject car;
    public gamestate state;
    public GameObject stamina;

    [Header("Arabaya binince KAPANACAKLAR (HUD vb.)")]
    [SerializeField] private List<GameObject> closeWhenInCar = new();

    // Seçtiğimiz aktif sürüş scriptini tutalım (VehicleController veya CarController olabilir)
    private Behaviour _activeDriveScript;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (Speedometer) Speedometer.SetActive(false);
    }

    void Update()
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
        if (!Physics.Raycast(ray, out var hit, maxDistance, interactableLayer)) return;
        if (!hit.collider.CompareTag("Car")) return;

        GameObject root = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.transform.root.gameObject;

        // 1) CarEnterable bul
        var enterable = root.GetComponentInParent<CarEnterable>() ?? root.GetComponentInChildren<CarEnterable>(true);
        if (!enterable)
        {
            Debug.LogWarning($"[StateManger] CarEnterable yok: {root.name}");
            return;
        }

        // 2) Enter
        bool ok = enterable.Enter(player, playerCamera);
        if (!ok) return;

        // 3) Senin HUD/state
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

        // 1) CarEnterable bul
        var enterable = car.GetComponentInParent<CarEnterable>() ?? car.GetComponentInChildren<CarEnterable>(true);
        if (enterable) enterable.Exit();

        // 2) Senin HUD/state
        state = gamestate.player;
        if (player) player.SetActive(true);
        if (Speedometer) Speedometer.SetActive(false);
        if (stamina) stamina.SetActive(true);
        SetActiveList(closeWhenInCar, true);

        car = null;
    }
    // Hangi sürüş scriptini kullanacağımıza karar ver (öncelik VehicleController)
    private Behaviour PickDriveScript(GameObject root)
    {
        var vc = root.GetComponent<VehicleController>();
        if (vc) return vc;

        var cc = root.GetComponent<CarController>();
        if (cc) return cc;

        return null;
    }

    // === helper ===
    private void SetActiveList(List<GameObject> list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (go) go.SetActive(active);
        }
    }
}
