    using UnityEditor.UIElements;
    using UnityEngine;
    using System.Collections; // IEnumerator kullanabilmek için gerekli namespace
    using TMPro;
    public class Crosshair : MonoBehaviour
    {
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
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    interactable.Interact(); // Nesneye özel etkileşimi tetikle
                    Debug.Log("Etkileşim gerçekleşti: " + hit.collider.gameObject.name);
                }

                // SATIN ALMA SİSTEMİ
                Tools item = hit.collider.GetComponent<Tools>();
                if (item != null)
                {
                    currentItem = item;
                    BuyItem();
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
            npc_info npc = hit.collider.GetComponent<npc_info>();

            if (npc != null)
            {
                // UI'yı güncelle
                NpcInfoPanel.SetActive(true);
                Npcname.text = npc.Npc;
                Npcetkileşim.text = npc.etkilesim; 
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

                // Mevcut parayı al
                int currentMoney = int.Parse(inventory_uı.money_text.text);

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


    private void BuyItem()
    {
        int currentMoney = int.Parse(inventory_uı.money_text.text); // Mevcut parayı al (string → int)

        if (currentMoney >= currentItem.price)
        {
            currentMoney -= currentItem.price; // Parayı düş
            inventory_uı.money_text.text = currentMoney.ToString(); // UI'yi güncelle
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

                // Tıklanan hücre SeedBox katmanında mı ve seçili öğe "seed" mi kontrol et
                if (clickedCell.layer == LayerMask.NameToLayer("SeedBox") && toolbar.GetSelectedPrefabTag() == "seed")
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
                        }
                        else
                        {
                            Debug.LogWarning($"Prefab bulunamadı: {selectedItemUsedPrefab}");
                        }
                    }
                }
                else
                {
                    // Şartlar sağlanmadığında kullanıcıyı bilgilendir
                    Debug.Log("Tıklanan hücre SeedBox değil veya seçili öğe 'seed' değil.");
                }
            }
            else
            {
                Debug.Log("Raycast bir objeye çarpmadı.");
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
                            Debug.LogWarning($"Prefab bulunamadı: {selectedItemUsedPrefab}");
                        }
                    }
                }
                else
                {
                    // Şartlar sağlanmadığında kullanıcıyı bilgilendir
                    Debug.Log("Tıklanan hücre SeedBox değil veya seçili öğe 'seed' değil.");
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







