    using UnityEditor.UIElements;
    using UnityEngine;
    using System.Collections; // IEnumerator kullanabilmek için gerekli namespace
    using TMPro;
    using System.Text.RegularExpressions; // En üste
    public class Crosshair : MonoBehaviour
{
    public AnimationController animController;
    public Money money; // Para yönetimi için Money script referansı
    public Camera playerCamera; // Oyuncunun kamerası
    public float maxDistance = 100f; // Maksimum atış mesafesi
    public LayerMask interactableLayer; // Etkileşimde bulunulacak katman
    public GameObject player; // Oyuncu karakteri
    public DynamicGridManager gridManager;
    public GameObject replacementPrefab; // Yerine geçecek prefab
    public UI_Manager manager;
    public static bool dragSingle;
    public TreeFall TreeFall;
    public Toolbar_UI toolbar;
    public TextMeshProUGUI itemNameText; // UI - Eşya adı
    public TextMeshProUGUI itemPriceText; // UI - Eşya fiyatı
    public TextMeshProUGUI Npcname; // UI - Eşya adı
    public TextMeshProUGUI Npcetkileşim; // UI - Eşya fiyatı
    public Tools currentItem; // Şu an baktığın eşya
    public Inventory_UI inventory_uı; // Envanter sistemi
    public GameObject itemInfoPanel; // UI Panel
    public GameObject NpcInfoPanel; // UI Panel

    public void Update()
    {
        UpdateItemInfo();
        Updateinfo();

        if (Input.GetMouseButtonDown(0))
        {
            ShootRay();
            HitTree();
            AddSeed();
            Watering();
            animController.PlayInteractAnimation();
        }
        if (Input.GetMouseButtonDown(1))
        {
            ChangeCell();
        }

        if (Input.GetKeyDown(KeyCode.E)) // E tuşuna basılınca
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
            {
                UniversalIdentifier id = hit.collider.GetComponent<UniversalIdentifier>();
                if (id != null && id.ID.ToLower() == "halci")
                {
                    // Market paneli aktifse kapat, değilse aç
                    if (id.market.activeSelf)
                    {
                        id.closemarket();
                        Debug.Log("🛒 Halcı ile etkileşim → Market kapatıldı.");
                    }
                    else
                    {
                        id.openmarket();
                        Debug.Log("🛒 Halcı ile etkileşim → Market açıldı.");
                    }
                }
                else
                {
                    Debug.LogWarning("Bu NPC'nin universal ID'si 'halci' değil veya UniversalIdentifier bileşeni yok.");
                }
            }
            if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                    if (interactable != null)
                    {
                        interactable.Interact(); // Nesneye özel etkileşimi tetikle
                        Debug.Log("Etkileşim gerçekleşti: " + hit.collider.gameObject.name);
                    }

                    Tools item = hit.collider.GetComponent<Tools>();
                    if (item == null)
                        item = hit.collider.GetComponentInParent<Tools>();

                    if (item != null)
                    {
                        Debug.Log("SATIN ALMA: Tools bulundu → " + item.itemName);
                        currentItem = item;
                        BuyItem();
                    }
                    else
                    {
                        Debug.LogWarning("SATIN ALMA: Tools component yok → BuyItem çalışmadı.");
                    }

                }
            if (Physics.Raycast(ray, out hit, 3f))
            {
                if (hit.collider.CompareTag("NPC"))
                {
                    NPCInteraction npc = hit.collider.GetComponent<NPCInteraction>();
                    if (npc != null)
                    {
                        npc.StartDialog();
                        Debug.Log("NPC ile etkileşim başladı: " + npc.gameObject.name);
                    }
                }
            }
        }
    }
    void Updateinfo()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            UniversalIdentifier npc = hit.collider.GetComponent<UniversalIdentifier>();

            if (npc != null)
            {
                // UI'yı güncelle
                NpcInfoPanel.SetActive(true);
                Npcname.text = npc.ID;
                return;
            }
        }
        // Eğer hiçbir uygun iteme çarpmadıysa paneli gizle
        NpcInfoPanel.SetActive(false);
    }
    void UpdateItemInfo()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            Tools item = hit.collider.GetComponent<Tools>();

            if (item != null)
            {
                // UI'yı güncelle
                itemInfoPanel.SetActive(true);
                itemNameText.text = item.itemName;
                itemPriceText.text = item.price.ToString();

                int currentMoney;
                if (!int.TryParse(money.moneyText.text, out currentMoney))
                {
                    Debug.LogWarning("Parayı parse edemedim: " + money.moneyText.text);
                    currentMoney = 0; // Hatalıysa 0 kabul et veya çık
                }


                // Renk değişimi
                if (currentMoney >= item.price)
                {
                    itemNameText.color = Color.green; // Yeterli para varsa yeşil
                    itemPriceText.color = Color.green; // Yeterli para varsa yeşil
                }
                else
                {
                    itemNameText.color = Color.red; // Yetersizse kırmızı
                    itemPriceText.color = Color.red; // Yeterli para varsa yeşil
                }

                return;
            }
        }

        // Eğer hiçbir uygun iteme çarpmadıysa paneli gizle
        itemInfoPanel.SetActive(false);
    }

    public void BuyItem()
    {
        Debug.Log("BuyItem tetiklendi!");

        // Sadece rakamları ayıkla
        string cleanText = Regex.Replace(money.moneyText.text, @"[^\d]", "");

        int currentMoney = 0;
        if (!int.TryParse(cleanText, out currentMoney))
        {
            Debug.LogWarning("Para parse edilemedi! Metin: " + money.moneyText.text);
            return;
        }

        if (currentMoney >= currentItem.price)
        {
            currentMoney -= currentItem.price;
            money.moneyText.text = currentMoney.ToString(); // sadece sayı göster

            ShootRay();
            Debug.Log(currentItem.itemName + " satın alındı!");
        }
        else
        {
            Debug.Log("Yetersiz altın!");
        }
    }
    public void ShootRay()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
        {
            // Etkileşimli nesneye ulaşıldıysa
            Debug.Log("Etkileşim: " + hit.collider.name);

            // Collectable bileşeni olup olmadığını kontrol et
            Collectable collectable = hit.collider.GetComponent<Collectable>();

            if (collectable != null)
            {
                // Nesnenin Collect metodunu çağırarak tetikle
                collectable.Collect();
            }
        }
    }

    public void HitTree()
    {
        // Nişangah pozisyonuna göre ray oluştur
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast ile tıklanan hücreyi bul
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject; // Tıklanan hücreyi al

            // Katman kontrolü ve seçili öğe adı kontrolü
            if (clickedCell.layer == LayerMask.NameToLayer("Tree") && toolbar.GetSelectedPrefab() == "axe")
            {
                // TreeFall bileşenini tıklanan objeden al
                TreeFall tree = clickedCell.GetComponent<TreeFall>();

                if (tree != null && !tree.isFalling)
                {
                    // Ağacı devirmek için ShakeAndFall coroutine'ini başlat
                    StartCoroutine(tree.ShakeAndFall());
                }
                else
                {
                    Debug.Log("Bu ağaç zaten devrilmiş.");
                }
            }
            else
            {
                // Şartlar sağlanmadığında kullanıcıyı bilgilendir
                Debug.Log("Ağaç değil veya elinde balta yok");
            }
        }
    }


    // Fare ile tıklanarak hücre değiştirilir
    public void ChangeCell()
    {
        // Nişangah pozisyonuna göre ray oluştur
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast ile tıklanan hücreyi bul
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject; // Tıklanan hücreyi al

            // Katman kontrolü ve seçili öğe adı kontrolü
            if (clickedCell.layer == LayerMask.NameToLayer("ground") && toolbar.GetSelectedPrefab() == "Hoe")
            {
                // Hücreyi sil ve yerine yeni hücre oluştur
                Vector3 cellPosition = clickedCell.transform.position;
                Quaternion cellRotation = clickedCell.transform.rotation;
                Vector3 cellScale = clickedCell.transform.localScale;

                // Yeni hücreyi oluştur
                GameObject newCell = Instantiate(replacementPrefab, cellPosition, cellRotation);
                newCell.transform.localScale = cellScale;

                // Eski hücreyi yok et
                Destroy(clickedCell);

                Debug.Log("Hücre başarıyla değiştirildi.");
            }
            else
            {
                // Şartlar sağlanmadığında kullanıcıyı bilgilendir
                Debug.Log("katman ground değil veya elinde hoe yok");
            }
        }
    }

    // Fare tıklama ile seçilen hücrenin rengini değiştirir ve aktif hale getirir

    public void ActivateCellAtMousePosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition); // Nişangahın ekran üzerindeki pozisyonundan ray oluştur
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer)) // Raycast ile vurulan nesneyi bul
        {
            GameObject clickedCell = hit.collider.gameObject; // Vurulan hücreyi al

            // Eğer hücre zemin katmanına aitse
            if (clickedCell.layer == LayerMask.NameToLayer("groundcell") && toolbar.GetSelectedPrefab() == "Hammer")
            {
                clickedCell.transform.GetChild(0).gameObject.SetActive(true); // Child objeyi aktif yap
            }
        }
    }
    public void AddSeed()
    {
        // Nişangah pozisyonuna göre ray oluştur
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast ile tıklanan hücreyi bul
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject; // Tıklanan hücreyi al
            Debug.Log($"Raycast başarılı, çarpılan obje: {clickedCell.name}, Layer: {clickedCell.layer}");

            // Tıklanan hücre SeedBox katmanında mı ve seçili öğe "seed" mi kontrol et
            int seedBoxLayer = LayerMask.NameToLayer("SeedBox");
            Debug.Log($"SeedBox Layer Index: {seedBoxLayer}");
            Debug.Log($"Seçili prefab tagı: {toolbar.GetSelectedPrefabTag()}");

            if (clickedCell.layer == seedBoxLayer && toolbar.GetSelectedPrefabTag() == "seed")
            {
                string selectedItemUsedPrefab = toolbar.GetSelectedUsedPrefab();
                Debug.Log($"Prefab adı: {selectedItemUsedPrefab}");

                if (!string.IsNullOrEmpty(selectedItemUsedPrefab))
                {
                    // Resources klasöründen prefab'ı yükle
                    GameObject newItem = Resources.Load<GameObject>($"Prefabs/foods/{selectedItemUsedPrefab}");
                    Debug.Log($"Prefab yükleniyor: {newItem}");
                    if (newItem != null)
                    {
                        // Yeni prefab'ı hücrenin merkezine spawnla
                        Vector3 spawnPosition = clickedCell.transform.position; // Hücrenin pozisyonu
                        Quaternion spawnRotation = Quaternion.identity; // Varsayılan rotasyon
                        Debug.Log($"Spawn pozisyonu: {spawnPosition}, Rotasyon: {spawnRotation}");
                        // Instantiate ile yeni prefab'ı oluştur
                        Instantiate(newItem, spawnPosition, spawnRotation);
                        // Hücrenin child'ı olan seedBox objesini aktif et
                        clickedCell.transform.GetChild(0).gameObject.SetActive(true);
                        Debug.Log($"Seed prefab spawned: {newItem} at {spawnPosition}");
                        // Hücrenin child'ı olan seedBox objesini aktif et
                        Destroy(clickedCell);
                        // Hücreyi yok et
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab bulunamadı: {selectedItemUsedPrefab}");
                    }
                }
                else
                {
                    Debug.LogWarning("Seçili prefab adı boş!");
                }
            }
            else
            {
                Debug.LogWarning($"Layer veya tag uyuşmuyor! clickedCell.layer: {clickedCell.layer}, seedBoxLayer: {seedBoxLayer}, tag: {toolbar.GetSelectedPrefabTag()}");
            }
        }
        else
        {
            Debug.LogWarning("Raycast bir objeye çarpmadı.");
        }
    }
    public void Watering()
    {
        // Nişangah pozisyonuna göre ray oluştur
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast ile tıklanan hücreyi bul
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject; // Tıklanan hücreyi al

            // Tıklanan hücre SeedBox katmanında mı ve seçili öğe "seed" mi kontrol et
            if (clickedCell.layer == LayerMask.NameToLayer("SeedBox") && toolbar.GetSelectedPrefab() == "WateringCan_full")
            {
                string selectedItemUsedPrefab = toolbar.GetSelectedUsedPrefab();

                if (!string.IsNullOrEmpty(selectedItemUsedPrefab))
                {
                    // Resources klasöründen prefab'ı yükle
                    GameObject newItem = Resources.Load<GameObject>($"Prefabs/{selectedItemUsedPrefab}");

                    if (newItem != null)
                    {
                        // Yeni prefab'ı hücrenin merkezine spawnla
                        Vector3 spawnPosition = clickedCell.transform.position; // Hücrenin pozisyonu
                        Quaternion spawnRotation = Quaternion.identity; // Varsayılan rotasyon
                        Instantiate(newItem, spawnPosition, spawnRotation);
                        Debug.Log($"Seed prefab spawned: {selectedItemUsedPrefab} at {spawnPosition}");
                        StartCoroutine(waterfall());
                    }
                    else
                    {
                        //Debug.LogWarning($"Prefab bulunamadı: {selectedItemUsedPrefab}");
                    }
                }
            }
            else
            {
                // Şartlar sağlanmadığında kullanıcıyı bilgilendir
                //Debug.Log("Tıklanan hücre SeedBox değil veya seçili öğe 'seed' değil.");
            }
        }
        else
        {
            Debug.Log("Raycast bir objeye çarpmadı.");
        }
    }
    public IEnumerator waterfall()
    {
        //WateringCan_full.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(1);
        //WateringCan_full.transform.GetChild(0).gameObject.SetActive(true);
    }
}







