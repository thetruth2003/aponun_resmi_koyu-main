using System.Collections.Generic;
using UnityEngine;
using static CarController;

public enum gamestate { player, Car }
public class StateManger : MonoBehaviour
{
    public Camera playerCamera;                 // Oyuncunun kamerası
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

    private void ExitCar()
    {
        CarController carController = car ? car.GetComponent<CarController>() : null;

        // >>> EK: VehicleController.exitPoint varsa önce onu kullan
        var vc2 = car ? car.GetComponent<VehicleController>() : null;
        if (vc2 && vc2.exitPoint && player)
        {
            player.transform.SetPositionAndRotation(vc2.exitPoint.position, vc2.exitPoint.rotation);
        }
        else if (carController != null && carController.playerpoint != null && player) // mevcut fallback
        {
            player.transform.position = carController.playerpoint.transform.position;
        }

        // (devamı senin mevcut kodun)
        player.transform.parent = null;
        state = gamestate.player;
        player.SetActive(true);
        Speedometer.SetActive(false);
        stamina.SetActive(true);
        SetActiveList(closeWhenInCar, true);
        var vc = car ? car.GetComponent<VehicleController>() : null;
        if (vc) vc.enabled = false;
        car = null;
    }

    private void EnterCar()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
        {
            if (hit.collider.CompareTag("Car"))
            {
                // Köke çık (tekerlek collerine vurabilir)
                GameObject root = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;

                car = root;
                if (player) player.SetActive(false);
                state = gamestate.Car;

                if (Speedometer) Speedometer.SetActive(true);
                if (stamina) stamina.SetActive(false);

                // Arabaya binince listede olanları KAPAT
                SetActiveList(closeWhenInCar, false);

                // >>> sadece sürüş scriptini AÇ
                var vc = root.GetComponent<VehicleController>();      // yeni scriptin
                if (vc) vc.enabled = true;

                // Eski isimli bir script kullanıyorsan opsiyonel:
                var vcOld = root.GetComponent<VehicleControl>();      // eski sürüm
                if (vcOld) vcOld.enabled = true;

                // (İstersen kullan)
                CarController carController = root.GetComponent<CarController>();
            }
        }
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
