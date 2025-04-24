using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static CarController;

public enum gamestate { player, Car}
public class StateManger : MonoBehaviour
{
    public Camera playerCamera; // Oyuncunun kamerası
    public float maxDistance = 100f; // Maksimum atış mesafesi
    public LayerMask interactableLayer; // Etkileşimde bulunulacak katman
    public GameObject player; // Oyuncu karakteri
    public GameObject Speedometer;
    public static StateManger Instance;
    public GameObject car;
    public gamestate state;
    public GameObject stamina;
    private void Awake()
    {
        if (Instance == null)  
            Instance = this; 
        else
            Destroy(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        Speedometer.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && state == gamestate.player)
        {
            EnterCar();
        }
        // E tuşuna basıldığında aracın kontrolünü al
        else if (Input.GetKeyDown(KeyCode.E) && state == gamestate.Car)
        {
            ExitCar();
        }
    }
    private void ExitCar()
    {
        CarController carController = car.GetComponent<CarController>();
        if (carController != null && carController.playerpoint != null)
        {
            player.transform.position = carController.playerpoint.transform.position;
        }

        player.transform.parent = null;
        state = gamestate.player;
        player.SetActive(true); // Bu en sonda olacak
        Speedometer.SetActive(false);
        stamina.SetActive(true);
        car.GetComponent<VehicleControl>().enabled = false;
        car = null;

    }
    private void EnterCar()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
        {
            // E tuşuna basıldığında oyuncuyu devre dışı bırak ve araca geç
            if (hit.collider.CompareTag("Car"))
            {
                car = hit.collider.gameObject;
                player.SetActive(false);
                state = gamestate.Car;
                Speedometer.SetActive(true);
                stamina.SetActive(false);
                hit.collider.GetComponent<VehicleControl>().enabled = true;
                // Araç türünü belirle
                CarController carController = hit.collider.GetComponent<CarController>();
            }
        }
    }

}
