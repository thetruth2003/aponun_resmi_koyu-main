using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum VehicleType { Harvester, Excavator, Tractor, Wheelbarrow }  // Araç türünü tanımlıyoruz
    public VehicleType vehicleType;  // Bu araç hangi türde? (Harvester ya da Excavator)
    public GameObject playerpoint;
    private bool isInVehicle = false; // Player'ın araçta olup olmadığını kontrol eder.
    public float rotationSpeedHarvester = 100f; // Dönüş hızı
    public Transform blade; // Biçer döverin ucu
    public bool isRotating = false; // Dönme durumu
    public Transform arm; // Kepçenin kolu
    public Transform bucket; // Kepçenin kolu
    public float rotationSpeed = 10f; // Kolun hareket hızı
    public float minRotation = -50f; // X eksenindeki minimum dönüş açısı
    public float maxRotation = 30f;  // X eksenindeki maksimum dönüş açısı
    private float currentRotationX = 0; // Kolun mevcut X rotasyonu
    public Transform attachmentPoint; // Çeki demirinin bağlanma noktası
    private HingeJoint currentJoint; // Aktif hinge joint
    private GameObject attachedTrailer; // Bağlanan römork

    private void OnTriggerStay(Collider other)
    {
        // Sadece "Attachable" tag'ine sahip nesneler için
        if (other.CompareTag("Attachable"))
        {
            Debug.Log("Attachable nesne algılandı: " + other.name);

            // Traktör tipi kontrolü ve bağlı römork yoksa işlem yap
            if (vehicleType == VehicleType.Tractor)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    // Eğer römork zaten bağlıysa, bağlantıyı kes
                    if (attachedTrailer != null)
                    {
                        //DetachTrailer();
                    }
                    else
                    {
                        // Bağlı değilse, römorku bağla
                        AttachTrailer(other.gameObject);
                    }
                }
            }
        }
    }

    private void AttachTrailer(GameObject trailer)
    {
        // Bağlama işlemini başlat
        attachedTrailer = trailer;

        // Traktörün Rigidbody'sini parent'tan al
        Rigidbody tractorRb = GetComponentInParent<Rigidbody>();  // Traktörün Rigidbody'sini bul

        if (tractorRb != null)
        {
            // Römorka Hinge Joint ekle ve traktöre bağla
            HingeJoint hingeJoint = trailer.AddComponent<HingeJoint>();

            // Hinge Joint'i traktörün Rigidbody'sine bağla
            hingeJoint.connectedBody = tractorRb;

            // Bağlanma noktası olarak çekiş demirini kullan
            hingeJoint.anchor = attachmentPoint.localPosition;

            // Bağlantı ekseni (genellikle Y ekseni)
            hingeJoint.axis = Vector3.up;

            // Ekleme sonrası römorku doğru şekilde bağla
            currentJoint = hingeJoint;

            Debug.Log("Römork bağlandı!");
            Debug.Log("Traktör Rigidbody: " + tractorRb);
            Debug.Log("Hinge Joint bağlandı. Connected Body: " + hingeJoint.connectedBody);
        }
        else
        {
            Debug.LogWarning("Traktör Rigidbody'si bulunamadı!");
        }
    }

    public void DetachTrailer()
    {
        if (attachedTrailer != null)
        {
            // Hinge Joint'i silerek bağlantıyı kopar
            Destroy(currentJoint);
            currentJoint = null;

            // Bağlı römork referansını temizle
            attachedTrailer = null;

            Debug.Log("Römork bağlantısı kesildi!");
        }
    }


    void SetArmRotation(float rotationX)
    {
        arm.localRotation = Quaternion.Euler(rotationX, arm.localRotation.eulerAngles.y, arm.localRotation.eulerAngles.z);
    }

    // Kolun X rotasyonunu ayarlama
    void SetBucketRotation(float rotationX)
    {
        bucket.localRotation = Quaternion.Euler(rotationX, bucket.localRotation.eulerAngles.y, bucket.localRotation.eulerAngles.z);
    }

    // Harvester'ı döndürme fonksiyonu
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
                // Harvester için F tuşu ile bıçak döndürme
                if (Input.GetKeyDown(KeyCode.F))
                {
                    isRotating = !isRotating; // Dönmeyi başlat/durdur
                }

                // Harvester bıçaklarını döndürme
                if (isRotating)
                {
                    RotateBlade();
                }
            }
            else if (vehicleType == VehicleType.Excavator)
            {
                // Kepçe için F tuşu ile kolu aşağı indir
                if (Input.GetKey(KeyCode.F))
                {
                    if (currentRotationX > minRotation)
                    {
                        currentRotationX -= rotationSpeed * Time.deltaTime; // X rotasyonunu azalt
                        SetArmRotation(currentRotationX);
                    }
                }

                // "R" tuşuna basıldığında kolu yukarı kaldır
                if (Input.GetKey(KeyCode.R))
                {
                    if (currentRotationX < maxRotation)
                    {
                        currentRotationX += rotationSpeed * Time.deltaTime; // X rotasyonunu artır
                        SetArmRotation(currentRotationX);
                    }
                }

                // "T" tuşuna basıldığında kolu aşağı indir
                if (Input.GetKey(KeyCode.T))
                {
                    if (currentRotationX > minRotation)
                    {
                        currentRotationX -= rotationSpeed * Time.deltaTime; // X rotasyonunu azalt
                        SetBucketRotation(currentRotationX);
                    }
                }

                // "G" tuşuna basıldığında kolu yukarı kaldır
                if (Input.GetKey(KeyCode.G))
                {
                    if (currentRotationX < maxRotation)
                    {
                        currentRotationX += rotationSpeed * Time.deltaTime; // X rotasyonunu artır
                        SetBucketRotation(currentRotationX);
                    }
                }
            }
        }
    }
}
