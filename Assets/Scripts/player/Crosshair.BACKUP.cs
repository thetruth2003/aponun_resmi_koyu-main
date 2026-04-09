#if false
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Nişangah üzerinden bakılan nesne bilgisini gösterir ve tıklama bazlı etkileşimleri yönetir.
/// </summary>
public class Crosshair : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 100f;
    public LayerMask interactableLayer;
    public GameObject player;
    public DynamicGridManager gridManager;
    public GameObject replacementPrefab;
    public UI_Manager manager;
    public static bool dragSingle;
    public TreeFall TreeFall;
    public Toolbar_UI toolbar;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public TextMeshProUGUI Npcname;
    public TextMeshProUGUI Npcetkileşim;
    public Tools currentItem;
    public Inventory_UI inventory_uı;
    public GameObject itemInfoPanel;
    public GameObject NpcInfoPanel;
    public Muhasebeci muhasebeci;
    public Inventory inventory;

    public void Update()
    {
        if (PauseMenuUI.IsInputLocked)
        {
            return;
        }

        UpdateItemInfo();
        Updateinfo();

        if (Input.GetMouseButtonDown(0))
        {
            HandlePrimaryClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ChangeCell();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInteractKey();
        }
    }

    private void HandlePrimaryClick()
    {
        ShootRay();
        HitTree();
        AddSeed();
        Watering();
    }

    private void HandleInteractKey()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        HandleHalciInteraction(ray);
        HandleGenericInteraction(ray);
        HandleNpcDialog(ray);
    }

    private void HandleHalciInteraction(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            UniversalIdentifier id = hit.collider.GetComponent<UniversalIdentifier>();
            if (id != null && id.ID.ToLower() == "halci")
            {
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
    }

    private void HandleGenericInteraction(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                Debug.Log("Etkileşim gerçekleşti: " + hit.collider.gameObject.name);
            }

            Tools item = hit.collider.GetComponent<Tools>();
            if (item == null)
            {
                item = hit.collider.GetComponentInParent<Tools>();
            }

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
    }

    private void HandleNpcDialog(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
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

    private void Updateinfo()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            UniversalIdentifier npc = hit.collider.GetComponent<UniversalIdentifier>();
            if (npc != null)
            {
                NpcInfoPanel.SetActive(true);
                Npcname.text = npc.ID;
                return;
            }
        }

        NpcInfoPanel.SetActive(false);
    }

    private void UpdateItemInfo()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            Tools item = hit.collider.GetComponent<Tools>();
            if (item != null)
            {
                itemInfoPanel.SetActive(true);
                itemNameText.text = item.itemName;
                itemPriceText.text = item.price.ToString();

                int currentMoney = muhasebeci.playerMoney;
                if (currentMoney >= item.price)
                {
                    itemNameText.color = Color.green;
                    itemPriceText.color = Color.green;
                }
                else
                {
                    itemNameText.color = Color.red;
                    itemPriceText.color = Color.red;
                }

                return;
            }
        }

        itemInfoPanel.SetActive(false);
    }

    public void BuyItem()
    {
        Debug.Log("BuyItem tetiklendi!");

        int currentMoney = muhasebeci.playerMoney;
        if (currentMoney >= currentItem.price)
        {
            muhasebeci.playerMoney -= currentItem.price;
            if (muhasebeci.moneyText != null)
            {
                muhasebeci.moneyText.text = muhasebeci.playerMoney.ToString();
            }

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
            {
                Debug.Log("Etkileşim: " + hit.collider.name);

                Collectable collectable = hit.collider.GetComponent<Collectable>();
                Tools tools = hit.collider.GetComponent<Tools>();
                if (collectable != null && tools != null)
                {
                    collectable.Buy(tools.amount);
                }
            }
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
            Debug.Log("Etkileşim: " + hit.collider.name);

            Collectable collectable = hit.collider.GetComponent<Collectable>();
            Tools tools = hit.collider.GetComponent<Tools>();
            if (collectable != null && tools == null)
            {
                collectable.Collect();
            }
        }
    }

    public void HitTree()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject;
            if (clickedCell.layer == LayerMask.NameToLayer("Tree") && toolbar.GetSelectedPrefab() == "axe")
            {
                TreeFall tree = clickedCell.GetComponent<TreeFall>();
                if (tree != null && !tree.isFalling)
                {
                    StartCoroutine(tree.ShakeAndFall());
                }
                else
                {
                    Debug.Log("Bu ağaç zaten devrilmiş.");
                }
            }
            else
            {
                Debug.Log("Ağaç değil veya elinde balta yok");
            }
        }
    }

    public void ChangeCell()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject;
            if (clickedCell.layer == LayerMask.NameToLayer("ground") && toolbar.GetSelectedPrefab() == "Hoe")
            {
                Vector3 cellPosition = clickedCell.transform.position;
                Quaternion cellRotation = clickedCell.transform.rotation;
                Vector3 cellScale = clickedCell.transform.localScale;

                GameObject newCell = Instantiate(replacementPrefab, cellPosition, cellRotation);
                newCell.transform.localScale = cellScale;
                Destroy(clickedCell);

                Debug.Log("Hücre başarıyla değiştirildi.");
            }
            else
            {
                Debug.Log("katman ground değil veya elinde hoe yok");
            }
        }
    }

    public void ActivateCellAtMousePosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject;
            if (clickedCell.layer == LayerMask.NameToLayer("groundcell") && toolbar.GetSelectedPrefab() == "Hammer")
            {
                clickedCell.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    public void AddSeed()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject;
            Debug.Log($"Raycast başarılı, çarpılan obje: {clickedCell.name}, Layer: {clickedCell.layer}");

            int seedBoxLayer = LayerMask.NameToLayer("SeedBox");
            Debug.Log($"SeedBox Layer Index: {seedBoxLayer}");
            Debug.Log($"Seçili prefab tagı: {toolbar.GetSelectedPrefabTag()}");

            if (clickedCell.layer == seedBoxLayer && toolbar.GetSelectedPrefabTag() == "seed")
            {
                string selectedItemUsedPrefab = toolbar.GetSelectedUsedPrefab();
                SeedData selectedSeedData = toolbar.GetSelectedPrefabSeedData();
                Debug.Log($"Prefab adı: {selectedItemUsedPrefab}");

                if (!string.IsNullOrEmpty(selectedItemUsedPrefab))
                {
                    GameObject newItem = Resources.Load<GameObject>($"Prefabs/foods/{selectedItemUsedPrefab}");
                    Debug.Log($"Prefab yükleniyor: {newItem}");
                    SeedPoint seedPoint = hit.collider.GetComponent<SeedPoint>();
                    if (newItem != null)
                    {
                        seedPoint.seedData = selectedSeedData;
                        seedPoint.PlantSeed(selectedSeedData.seedType);

                        Inventory.Slot selectedInvSlot = toolbar.GetSelectedInventorySlot();
                        Inventory inv = inventory ?? InventoryManager.Instance?.toolbar;

                        if (inv != null && selectedInvSlot != null)
                        {
                            inv.selectedSlot = selectedInvSlot;
                            inv.TryConsumeSelectedSlot(1);
                            Debug.Log("[AddSeed] Aktif slot azaltıldı.");
                        }
                        else
                        {
                            Debug.LogWarning("[AddSeed] Slot veya envanter bulunamadı, azaltılamadı.");
                        }

                        Debug.Log($"SeedPoint'e ekim yapıldı: {selectedSeedData.seedType}");
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
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            GameObject clickedCell = hit.collider.gameObject;

            int seedBoxLayer = LayerMask.NameToLayer("SeedBox");
            bool isWaterSelected =
                (toolbar.GetSelectedPrefabTag() == "water") ||
                (toolbar.GetSelectedPrefab() == "WateringCan_full");

            if (clickedCell.layer == seedBoxLayer && isWaterSelected)
            {
                SeedPoint sp = clickedCell.GetComponent<SeedPoint>();
                if (sp == null)
                {
                    Debug.LogWarning($"[Watering] SeedPoint yok: {clickedCell.name}");
                    return;
                }

                if (!sp.hasSeed)
                {
                    Debug.Log($"[Watering] Hücrede tohum yok: {clickedCell.name}");
                }

                sp.isWatered = true;

                if (sp.wateringEffectPrefab != null)
                {
                    Transform marker = sp.transform.Find("WaterIndicator");
                    if (marker == null)
                    {
                        GameObject fx = Instantiate(
                            sp.wateringEffectPrefab,
                            sp.transform.position + Vector3.up * 0.1f,
                            Quaternion.identity,
                            sp.transform
                        );
                        fx.name = "WaterIndicator";
                    }
                }
                else
                {
                    Debug.LogWarning($"[Watering] {sp.name} için wateringEffectPrefab atanmadı.");
                }

                Debug.Log($"Hücre sulandı: {clickedCell.name}");
            }
        }
        else
        {
            Debug.Log("Raycast bir objeye çarpmadı.");
        }
    }

public IEnumerator waterfall()
    {
        yield return new WaitForSeconds(1);
    }
}
#endif
