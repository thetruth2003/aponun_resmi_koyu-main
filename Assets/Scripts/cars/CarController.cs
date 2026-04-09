using UnityEngine;

/// <summary>
/// CarController sinifi, ilgili nesnenin kontrol ve davranis akislarini yonetir.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    /// <summary>
    /// VehicleType sinifi, arac sistemindeki ilgili davranisi yonetir.
    /// </summary>
    public enum VehicleType { Harvester, Excavator, Tractor, Wheelbarrow }
    public VehicleType vehicleType;
    public GameObject playerpoint;
    private bool isInVehicle = false;
    public float rotationSpeedHarvester = 100f;
    public Transform blade;
    public bool isRotating = false;
    public Transform arm;
    public Transform bucket;
    public float rotationSpeed = 10f;
    public float minRotation = -50f;
    public float maxRotation = 30f;
    private float currentRotationX = 0;
    public Transform attachmentPoint;
    private HingeJoint currentJoint;
    private GameObject attachedTrailer;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Attachable"))
        {

            if (vehicleType == VehicleType.Tractor)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (attachedTrailer != null)
                    {
                    }
                    else
                    {
                        AttachTrailer(other.gameObject);
                    }
                }
            }
        }
    }

    private void AttachTrailer(GameObject trailer)
    {
        attachedTrailer = trailer;

        Rigidbody tractorRb = GetComponentInParent<Rigidbody>();

        if (tractorRb != null)
        {
            HingeJoint hingeJoint = trailer.AddComponent<HingeJoint>();

            hingeJoint.connectedBody = tractorRb;

            hingeJoint.anchor = attachmentPoint.localPosition;

            hingeJoint.axis = Vector3.up;

            currentJoint = hingeJoint;

            Debug.Log("Römork baðlandý!");
            Debug.Log("Traktör Rigidbody: " + tractorRb);
            Debug.Log("Hinge Joint baðlandý. Connected Body: " + hingeJoint.connectedBody);
        }
        else
        {
            Debug.LogWarning("Traktör Rigidbody'si bulunamadý!");
        }
    }

    public void DetachTrailer()
    {
        if (attachedTrailer != null)
        {
            Destroy(currentJoint);
            currentJoint = null;

            attachedTrailer = null;

            Debug.Log("Römork baðlantýsý kesildi!");
        }
    }

    void SetArmRotation(float rotationX)
    {
        arm.localRotation = Quaternion.Euler(rotationX, arm.localRotation.eulerAngles.y, arm.localRotation.eulerAngles.z);
    }

    void SetBucketRotation(float rotationX)
    {
        bucket.localRotation = Quaternion.Euler(rotationX, bucket.localRotation.eulerAngles.y, bucket.localRotation.eulerAngles.z);
    }

    private void RotateBlade()
    {
        blade.Rotate(Vector3.right * rotationSpeedHarvester * Time.deltaTime, Space.Self);
    }

    void Update()
    {
        if (StateManger.Instance.state == gamestate.Car && StateManger.Instance.car == gameObject)
        {

            if (vehicleType == VehicleType.Harvester)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    isRotating = !isRotating;
                }

                if (isRotating)
                {
                    RotateBlade();
                }
            }
            else if (vehicleType == VehicleType.Excavator)
            {
                if (Input.GetKey(KeyCode.F))
                {
                    if (currentRotationX > minRotation)
                    {
                        currentRotationX -= rotationSpeed * Time.deltaTime;
                        SetArmRotation(currentRotationX);
                    }
                }

                if (Input.GetKey(KeyCode.R))
                {
                    if (currentRotationX < maxRotation)
                    {
                        currentRotationX += rotationSpeed * Time.deltaTime;
                        SetArmRotation(currentRotationX);
                    }
                }

                if (Input.GetKey(KeyCode.T))
                {
                    if (currentRotationX > minRotation)
                    {
                        currentRotationX -= rotationSpeed * Time.deltaTime;
                        SetBucketRotation(currentRotationX);
                    }
                }

                if (Input.GetKey(KeyCode.G))
                {
                    if (currentRotationX < maxRotation)
                    {
                        currentRotationX += rotationSpeed * Time.deltaTime;
                        SetBucketRotation(currentRotationX);
                    }
                }
            }
        }
    }
}
