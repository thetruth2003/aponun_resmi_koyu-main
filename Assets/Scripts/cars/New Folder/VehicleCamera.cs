using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
/// <summary>
/// VehicleCamera sinifi, arac sistemindeki ilgili davranisi yonetir.
/// </summary>
public class VehicleCamera : MonoBehaviour
{

    public Transform target;

    public float smooth = 0.3f;
    public float distance = 5.0f;
    public float height = 1.0f;
    public float Angle = 20;

    public List<Transform> cameraSwitchView;
    public LayerMask lineOfSightMask = 0;

    public CarUIClass CarUI;
    private Animator _animator;

    private float yVelocity = 0.0f;
    private float xVelocity = 0.0f;
    [HideInInspector]
    public int Switch;

    private int gearst = 0;
    private float thisAngle = -150;
    private float restTime = 0.0f;

    private Rigidbody myRigidbody;

    public bool Isturning;
    public bool IsBraking;

    private VehicleControl carScript;

    private bool _isstart;

    /// <summary>
    /// CarU sinifi, arac sistemindeki ilgili davranisi yonetir.
    /// </summary>
    [System.Serializable]
    public class CarUIClass
    {

        public Image tachometerNeedle;
        public Image barShiftGUI;

        public Text speedText;
        public Text GearText;

    }

    private int PLValue = 0;

    public void AnimFalse()
    {
        _animator.enabled = false;
    }

    public void GetIsstart(bool isstart)
    {
        _isstart = isstart;
    }
    public void CameraSwitch()
    {
        Switch++;
        if (Switch > cameraSwitchView.Count) { Switch = 0; }
    }

    public void CarAccelForward(float amount)
    {
        if (_isstart)
            carScript.accelFwd = amount;
    }

    public void CarAccelBack(float amount)
    {
        if (_isstart)
            carScript.accelBack = amount;

        IsBraking = true;
    }

    public void CarSteer(float amount)
    {
        carScript.steerAmount = amount;
        Isturning = true;
    }

    public void IsTurningFalse()
    {
        Isturning = false;
    }
    public void IsBrakingFalse()
    {
        IsBraking = false;
    }

    public void CarHandBrake(bool HBrakeing)
    {
        carScript.brake = HBrakeing;
    }

    public void CarShift(bool Shifting)
    {
        carScript.shift = Shifting;
    }

    /// <summary>
    /// Arac yan yatmis ya da takla atmissa fizik kuvveti uygulayarak guvenli yone cekmeyi dener.
    /// </summary>
    public void RestCar()
    {

        if (restTime == 0)
        {
            myRigidbody.AddForce(Vector3.up * 500000);
            myRigidbody.MoveRotation(Quaternion.Euler(0, transform.eulerAngles.y, 0));
            restTime = 2.0f;
        }

    }

    void Start()
    {
        _animator = GetComponent<Animator>();
        carScript = (VehicleControl)target.GetComponent<VehicleControl>();

        myRigidbody = target.GetComponent<Rigidbody>();

        cameraSwitchView = carScript.carSetting.cameraSwitchView;

    }

    /// <summary>
    /// Arac kamera hedefini, kamera modunu ve gorus cizgisini her karede gunceller.
    /// </summary>
    void Update()
    {
        if (StateManger.Instance != null && StateManger.Instance.state == gamestate.Car)
        {
            if (target == null || target != StateManger.Instance.car.transform)
            {
                target = StateManger.Instance.car.transform;
            }
        }
        if (!target) return;

        carScript = (VehicleControl)target.GetComponent<VehicleControl>();

        if (Input.GetKeyDown(KeyCode.G))
        {
            RestCar();
        }

        if (restTime != 0.0f)
            restTime = Mathf.MoveTowards(restTime, 0.0f, Time.deltaTime);

        GetComponent<Camera>().fieldOfView = Mathf.Clamp(carScript.speed / 10.0f + 60.0f, 60, 90.0f);

        if (Input.GetKeyDown(KeyCode.C))
        {
            Switch++;
            if (Switch > cameraSwitchView.Count) { Switch = 0; }
        }

        if (Switch == 0)
        {

            float xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x,
           target.eulerAngles.x + Angle, ref xVelocity, smooth);

            float yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y,
            target.eulerAngles.y, ref yVelocity, smooth);

            transform.eulerAngles = new Vector3(xAngle, yAngle, 0.0f);

            var direction = transform.rotation * -Vector3.forward;
            var targetDistance = AdjustLineOfSight(target.position + new Vector3(0, height, 0), direction);

            transform.position = target.position + new Vector3(0, height, 0) + direction * targetDistance;

        }
        else
        {

            transform.position = cameraSwitchView[Switch - 1].position;
            transform.rotation = Quaternion.Lerp(transform.rotation, cameraSwitchView[Switch - 1].rotation, Time.deltaTime * 5.0f);

        }

    }

    /// <summary>
    /// Kamera ile arac arasina bir engel girerse kamerayi hedefe yaklastirarak duvar icine girmesini engeller.
    /// </summary>
    float AdjustLineOfSight(Vector3 target, Vector3 direction)
    {

        RaycastHit hit;

        if (Physics.Raycast(target, direction, out hit, distance, lineOfSightMask.value))
            return hit.distance;
        else
            return distance;

    }

}
