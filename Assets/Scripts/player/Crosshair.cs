using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Nisangah uzerinden bakilan nesne bilgisini gosterir ve tiklama bazli etkilesimleri yonetir.
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
    [FormerlySerializedAs("Npcetkile\u015Fim")]
    public TextMeshProUGUI NpcEtkilesim;
    public Tools currentItem;
    [FormerlySerializedAs("inventory_u\u0131")]
    public Inventory_UI inventoryUI;
    public GameObject itemInfoPanel;
    public GameObject NpcInfoPanel;
    public Muhasebeci muhasebeci;
    public Inventory inventory;

    private Ray CreateMouseRay()
    {
        return playerCamera.ScreenPointToRay(Input.mousePosition);
    }

    private Ray CreateForwardRay()
    {
        return new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        return Physics.Raycast(CreateMouseRay(), out hit, maxDistance, interactableLayer);
    }

    private bool TryForwardRaycast(out RaycastHit hit)
    {
        return Physics.Raycast(CreateForwardRay(), out hit, maxDistance);
    }

    private UniversalIdentifier GetUniversalIdentifier(Collider collider, bool includeParent = false)
    {
        UniversalIdentifier identifier = collider.GetComponent<UniversalIdentifier>();
        if (identifier == null && includeParent)
        {
            identifier = collider.GetComponentInParent<UniversalIdentifier>();
        }

        return identifier;
    }

    private Tools GetTools(Collider collider, bool includeParent = false)
    {
        Tools tools = collider.GetComponent<Tools>();
        if (tools == null && includeParent)
        {
            tools = collider.GetComponentInParent<Tools>();
        }

        return tools;
    }

    private Collectable GetCollectable(Collider collider)
    {
        return collider.GetComponent<Collectable>();
    }

    private SeedPoint GetSeedPoint(Collider collider)
    {
        SeedPoint seedPoint = collider.GetComponent<SeedPoint>();
        if (seedPoint == null)
        {
            seedPoint = collider.GetComponentInParent<SeedPoint>();
        }

        return seedPoint;
    }

    private string GetSelectedPrefab()
    {
        return toolbar != null ? toolbar.GetSelectedPrefab() : null;
    }

    private string GetSelectedPrefabTag()
    {
        return toolbar != null ? toolbar.GetSelectedPrefabTag() : null;
    }

    private SeedData GetSelectedSeedData()
    {
        return toolbar != null ? toolbar.GetSelectedPrefabSeedData() : null;
    }

    private Inventory.Slot GetSelectedToolbarSlot()
    {
        return toolbar != null ? toolbar.GetSelectedInventorySlot() : null;
    }

    private bool IsSelectedPrefab(string prefabName)
    {
        return GetSelectedPrefab() == prefabName;
    }

    private bool IsSelectedPrefabTag(string prefabTag)
    {
        return GetSelectedPrefabTag() == prefabTag;
    }

    private bool IsOnLayer(GameObject target, string layerName)
    {
        return target.layer == LayerMask.NameToLayer(layerName);
    }

    private int GetCurrentMoney()
    {
        return muhasebeci != null ? muhasebeci.GetMoney() : 0;
    }

    private bool HasEnoughMoney(int amount)
    {
        return GetCurrentMoney() >= amount;
    }

    private void SpendMoney(int amount)
    {
        if (muhasebeci == null)
        {
            return;
        }

        muhasebeci.SetMoney(GetCurrentMoney() - amount);
    }

    private void RefreshMoneyText()
    {
        if (muhasebeci != null && muhasebeci.moneyText != null)
        {
            muhasebeci.moneyText.text = muhasebeci.GetMoney().ToString();
        }
    }

    private void ShowNpcInfo(string npcId)
    {
        if (NpcInfoPanel != null)
        {
            NpcInfoPanel.SetActive(true);
        }

        if (Npcname != null)
        {
            Npcname.text = npcId;
        }
    }

    private void HideNpcInfo()
    {
        if (NpcInfoPanel != null)
        {
            NpcInfoPanel.SetActive(false);
        }
    }

    private void ShowItemInfo(Tools item)
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(true);
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = item.price.ToString();
        }

        if (GetCurrentMoney() >= item.price)
        {
            if (itemNameText != null)
            {
                itemNameText.color = Color.green;
            }

            if (itemPriceText != null)
            {
                itemPriceText.color = Color.green;
            }
        }
        else
        {
            if (itemNameText != null)
            {
                itemNameText.color = Color.red;
            }

            if (itemPriceText != null)
            {
                itemPriceText.color = Color.red;
            }
        }
    }

    private void HideItemInfo()
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(false);
        }
    }

    private bool IsWaterSelected()
    {
        return IsSelectedPrefabTag("water") || IsSelectedPrefab("WateringCan_full");
    }

    private Inventory GetTargetInventory()
    {
        return inventory ?? InventoryManager.Instance?.toolbar;
    }

    private void ConsumeSelectedSeedSlot()
    {
        Inventory.Slot selectedInvSlot = GetSelectedToolbarSlot();
        Inventory targetInventory = GetTargetInventory();

        if (targetInventory != null && selectedInvSlot != null)
        {
            targetInventory.selectedSlot = selectedInvSlot;
            targetInventory.TryConsumeSelectedSlot(1);
        }
        else
        {
            Debug.LogWarning("[AddSeed] Slot veya envanter bulunamadi, azaltma yapilamadi.");
        }
    }

    private bool TryGetClickedCell(out RaycastHit hit, out GameObject clickedCell)
    {
        if (TryRaycast(out hit))
        {
            clickedCell = hit.collider.gameObject;
            return true;
        }

        clickedCell = null;
        return false;
    }

    private TreeFall GetTreeFall(GameObject clickedCell)
    {
        return clickedCell.GetComponent<TreeFall>();
    }

    private void ReplaceClickedCell(GameObject clickedCell)
    {
        Vector3 cellPosition = clickedCell.transform.position;
        Quaternion cellRotation = clickedCell.transform.rotation;
        Vector3 cellScale = clickedCell.transform.localScale;

        GameObject newCell = Instantiate(replacementPrefab, cellPosition, cellRotation);
        newCell.transform.localScale = cellScale;
        Destroy(clickedCell);
    }

    private bool TryInteractRaycast(Ray ray, out RaycastHit hit)
    {
        return Physics.Raycast(ray, out hit, maxDistance, interactableLayer);
    }

    private bool TryNpcRaycast(Ray ray, out RaycastHit hit)
    {
        return Physics.Raycast(ray, out hit, 3f);
    }

    private bool IsHalci(UniversalIdentifier identifier)
    {
        return identifier != null && identifier.ID.ToLower() == "halci";
    }

    private void ToggleHalciMarket(UniversalIdentifier identifier)
    {
        if (identifier.market.activeSelf)
        {
            identifier.closemarket();
        }
        else
        {
            identifier.openmarket();
        }
    }

    private void HandleInteractableComponent(Collider collider)
    {
        IInteractable interactable = collider.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact();
        }
    }

    private void HandleToolsPurchase(Collider collider)
    {
        Tools item = GetTools(collider, true);

        if (item != null)
        {
            currentItem = item;
            BuyItem();
        }
    }

    private void TryBuyTargetAtCursor()
    {
        if (TryRaycast(out RaycastHit hit))
        {
            Collectable collectable = GetCollectable(hit.collider);
            Tools tools = GetTools(hit.collider);
            if (collectable != null && tools != null)
            {
                collectable.Buy(tools.amount);
            }
        }
    }

    private void TryCollectTargetAtCursor()
    {
        if (TryRaycast(out RaycastHit hit))
        {
            if (TryHandleHarvestAction(hit.collider))
                return;

            Collectable collectable = GetCollectable(hit.collider);
            Tools tools = GetTools(hit.collider);
            if (collectable != null && tools == null && GetSeedPoint(hit.collider) == null)
            {
                collectable.Collect();
            }
        }
    }

    /// <summary>
    /// Her karede bilgi panellerini gunceller ve ana input akislarini ilgili helper metodlara dagitir.
    /// </summary>
    public void Update()
    {
        if (PauseMenuUI.IsInputLocked)
        {
            return;
        }

        UpdateItemInfo();
        UpdateNpcInfo();

        if (Input.GetMouseButtonDown(0))
        {
            HandlePrimaryClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ChangeCell();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInteractKey();
        }
    }

    /// <summary>
    /// Sol tik akisini tek raycast uzerinden sirayla toplama, agac, ekim ve sulama kararlarina dagitir.
    /// </summary>
    private void HandlePrimaryClick()
    {
        if (!TryGetClickedCell(out RaycastHit hit, out GameObject clickedCell))
        {
            return;
        }

        if (TryHandleTreeAction(clickedCell))
            return;

        if (TryHandleHarvestAction(hit.collider))
            return;

        if (TryHandleSeedAction(hit))
            return;

        if (TryHandleWaterAction(hit, clickedCell))
            return;

        TryHandleCollectAction(hit.collider);
    }

    /// <summary>
    /// Etkilesim tusunda market, satin alma ve NPC diyalog akisini ayni bakis yonu uzerinden isletir.
    /// </summary>
    private void HandleInteractKey()
    {
        Ray ray = CreateMouseRay();

        if (TryInteractRaycast(ray, out RaycastHit interactHit))
        {
            HandleHalciInteraction(interactHit.collider);
            HandleGenericInteraction(interactHit.collider);
        }

        HandleNpcDialog(ray);
    }

    private bool TryHandleCollectAction(Collider collider)
    {
        Collectable collectable = GetCollectable(collider);
        Tools tools = GetTools(collider);
        if (collectable != null && tools == null && GetSeedPoint(collider) == null)
        {
            collectable.Collect();
            return true;
        }

        return false;
    }

    private bool TryHandleHarvestAction(Collider collider)
    {
        SeedPoint seedPoint = GetSeedPoint(collider);
        if (seedPoint == null || !seedPoint.IsHarvestReady)
            return false;

        bool harvested = seedPoint.TryHarvest();
        return harvested;
    }

    private bool TryHandleTreeAction(GameObject clickedCell)
    {
        if (!IsOnLayer(clickedCell, "Tree") || !IsSelectedPrefab("axe"))
        {
            return false;
        }

        TreeFall tree = GetTreeFall(clickedCell);
        if (tree != null && !tree.isFalling)
        {
            StartCoroutine(tree.ShakeAndFall());
            return true;
        }

        return true;
    }

    private bool TryHandleSeedAction(RaycastHit hit)
    {
        if (!IsSelectedPrefabTag("seed"))
        {
            return false;
        }

        SeedData selectedSeedData = GetSelectedSeedData();
        SeedPoint seedPoint = GetSeedPoint(hit.collider);

        if (seedPoint == null)
        {
            return false;
        }

        if (selectedSeedData == null)
        {
            Debug.LogWarning("[AddSeed] Secili SeedData bulunamadi.");
            return true;
        }

        if (seedPoint.TryPlant(selectedSeedData))
        {
            ConsumeSelectedSeedSlot();
        }

        return true;
    }

    private bool TryHandleWaterAction(RaycastHit hit, GameObject clickedCell)
    {
        if (!IsWaterSelected())
        {
            return false;
        }

        SeedPoint seedPoint = GetSeedPoint(hit.collider);
        if (seedPoint == null)
        {
            return false;
        }

        if (seedPoint.TryWater())
        {
            return true;
        }

        return true;
    }

    private void HandleHalciInteraction(Collider collider)
    {
        UniversalIdentifier id = GetUniversalIdentifier(collider);
        if (IsHalci(id))
        {
            ToggleHalciMarket(id);
        }
    }

    private void HandleGenericInteraction(Collider collider)
    {
        HandleInteractableComponent(collider);
        HandleToolsPurchase(collider);
    }

    private void HandleNpcDialog(Ray ray)
    {
        if (TryNpcRaycast(ray, out RaycastHit hit) && hit.collider.CompareTag("NPC"))
        {
            NPCInteraction npc = hit.collider.GetComponent<NPCInteraction>();
            if (npc != null)
            {
                npc.StartDialog();
            }
        }
    }

    private void UpdateNpcInfo()
    {
        if (TryForwardRaycast(out RaycastHit hit))
        {
            UniversalIdentifier npc = GetUniversalIdentifier(hit.collider);
            if (npc != null)
            {
                ShowNpcInfo(npc.ID);
                return;
            }
        }

        HideNpcInfo();
    }

    private void UpdateItemInfo()
    {
        if (TryForwardRaycast(out RaycastHit hit))
        {
            Tools item = GetTools(hit.collider);
            if (item != null)
            {
                ShowItemInfo(item);
                return;
            }
        }

        HideItemInfo();
    }

    /// <summary>
    /// Market uzerinden secili urunun odemesini dusup ilgili satin alma akisini tetikler.
    /// </summary>
    public void BuyItem()
    {
        if (currentItem == null)
        {
            Debug.LogWarning("BuyItem icin currentItem atanamadi.");
            return;
        }

        if (HasEnoughMoney(currentItem.price))
        {
            SpendMoney(currentItem.price);
            RefreshMoneyText();
            TryBuyTargetAtCursor();
        }
        else
        {
            return;
        }
    }

    public void ShootRay()
    {
        TryCollectTargetAtCursor();
    }

    public void HitTree()
    {
        if (TryGetClickedCell(out _, out GameObject clickedCell))
        {
            TryHandleTreeAction(clickedCell);
        }
    }

    /// <summary>
    /// Sag tikla toprak hucresini islenmis hucre prefab'ina cevirir.
    /// </summary>
    public void ChangeCell()
    {
        if (TryGetClickedCell(out _, out GameObject clickedCell) && IsOnLayer(clickedCell, "ground") && IsSelectedPrefab("Hoe"))
        {
            ReplaceClickedCell(clickedCell);
        }
    }

    public void ActivateCellAtMousePosition()
    {
        if (TryGetClickedCell(out _, out GameObject clickedCell) && IsOnLayer(clickedCell, "groundcell") && IsSelectedPrefab("Hammer"))
        {
            clickedCell.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Secili tohum aracini kullanarak ekim noktasina tohum diker ve aktif slottan tuketir.
    /// </summary>
    public void AddSeed()
    {
        if (TryGetClickedCell(out RaycastHit hit, out _))
        {
            TryHandleSeedAction(hit);
        }
    }

    /// <summary>
    /// Secili sulama araci ile ekim noktasini sulayip visual state'i gunceller.
    /// </summary>
    public void Watering()
    {
        if (TryGetClickedCell(out RaycastHit hit, out GameObject clickedCell))
        {
            TryHandleWaterAction(hit, clickedCell);
        }
    }

    public IEnumerator waterfall()
    {
        yield return new WaitForSeconds(1);
    }
}
