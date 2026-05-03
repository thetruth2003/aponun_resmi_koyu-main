using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Manager, oyuncu arayuzlerinin ac/kapa durumunu ve Inventory_UI referanslarini
/// tek merkezde tutup refresh akislarini koordine eder.
/// </summary>
public class UI_Manager : MonoBehaviour
{
    public Dictionary<string, Inventory_UI> inventoryUIByName = new Dictionary<string, Inventory_UI>();
    public List<Inventory_UI> inventoryUIs;
    public GameObject inventoryPanel;
    public GameObject MenuPanel;
    public GameObject Stamina;
    public Camera playerCamera;
    public float maxDistance = 100f;
    public GameObject player;
    public static Slot_UI draggedSlot;
    public static Image draggedIcon;
    public static bool dragSingle;
    public SC_FPSController playerMovementScript;
    public GameObject Crosshair;
    public GameObject CrosshairCanvas;
    private bool isMenuOpen = false;

    [Header("Toolbar (opsiyonel)")]
    [SerializeField] private Toolbar_UI toolbarUI;

    public int GetToolbarSelectedIndex()
    {
        return toolbarUI ? toolbarUI.GetSelectedIndex() : -1;
    }

    public void SelectToolbarSlot(int index)
    {
        if (toolbarUI)
        {
            toolbarUI.SelectSlot(index);
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
    }

    private IEnumerator OpenInventoryNextFrame()
    {
        yield return null;
        ToggleInventoryUI();
    }

    public void Update()
    {
        if (PauseMenuUI.IsInputLocked)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventoryUI();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleInventoryUI();
        }

        if (Input.GetKey(KeyCode.Q) && !isMenuOpen)
        {
            ToggleMenuUI();
            isMenuOpen = true;
        }
        else if (Input.GetKeyUp(KeyCode.Q) && isMenuOpen)
        {
            ToggleMenuUI();
            isMenuOpen = false;
        }
    }

    private void ChestOpen()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Chest"))
            {
                ToggleInventoryUI();
            }
            else
            {
                ToggleInventoryUI();
            }
        }

        dragSingle = Input.GetKey(KeyCode.LeftShift);
    }

    public void ToggleInventoryUI()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool willOpen = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(willOpen);

        if (willOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerMovementScript != null) playerMovementScript.enabled = false;
            if (Crosshair != null) Crosshair.SetActive(false);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(false);
            StartCoroutine(SafeRefreshInventoryUI("backpack"));
        }
        else
        {
            if (playerMovementScript != null) playerMovementScript.enabled = true;
            if (Crosshair != null) Crosshair.SetActive(true);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ToggleMenuUI()
    {
        if (MenuPanel == null)
        {
            return;
        }

        bool willOpen = !MenuPanel.activeSelf;
        MenuPanel.SetActive(willOpen);

        if (willOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            if (playerMovementScript != null) playerMovementScript.enabled = false;
            if (Crosshair != null) Crosshair.SetActive(false);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerMovementScript != null) playerMovementScript.enabled = true;
            if (Crosshair != null) Crosshair.SetActive(true);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(true);
            DeactivateMenuAndChildren(MenuPanel);
        }
    }

    private void DeactivateMenuAndChildren(GameObject menu)
    {
        foreach (Transform child in menu.transform)
        {
            child.gameObject.SetActive(false);
        }

        if (menu.transform.childCount > 0)
        {
            menu.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    public void RefreshInventoryUI(string inventoryName)
    {
        StartCoroutine(SafeRefreshInventoryUI(inventoryName));
    }

    private IEnumerator SafeRefreshInventoryUI(string inventoryName)
    {
        yield return null;

        if (!inventoryUIByName.TryGetValue(inventoryName, out Inventory_UI invUI) || invUI == null)
        {
            Debug.LogWarning($"[UI_Manager] Inventory UI bulunamadi: '{inventoryName}'");
            yield break;
        }

        const int maxWait = 60;
        int waited = 0;
        // Inventory_UI bazen slot referanslarini bir frame gec kuruyor.
        // Hazir degilse timeout'a kadar bekleyip sonra refresh deniyoruz.
        while (!invUI.InventoryIsReady() && waited < maxWait)
        {
            waited++;
            yield return null;
        }

        if (!invUI.InventoryIsReady())
        {
            Debug.LogWarning($"[UI_Manager] '{inventoryName}' inventory hazir olmadi (timeout). Refresh atlandi.");
            yield break;
        }

        invUI.Refresh();
    }

    public void RefreshAll()
    {
        foreach (KeyValuePair<string, Inventory_UI> kv in inventoryUIByName)
        {
            Inventory_UI ui = kv.Value;
            if (ui == null)
            {
                continue;
            }

            if (ui.InventoryIsReady())
            {
                ui.Refresh();
            }
            else
            {
                StartCoroutine(SafeRefreshInventoryUI(kv.Key));
            }
        }
    }

    public Inventory_UI GetInventoryUI(string inventoryName)
    {
        if (inventoryUIByName.TryGetValue(inventoryName, out Inventory_UI ui))
        {
            return ui;
        }

        return null;
    }

    private void Initialize()
    {
        inventoryUIByName.Clear();
        if (inventoryUIs == null)
        {
            return;
        }

        // Sahnedeki Inventory_UI referanslarini isimle map'leyip daha sonra
        // "backpack", "chest" gibi anahtarlarla hizli ulasilabilir hale getir.
        foreach (Inventory_UI ui in inventoryUIs)
        {
            if (ui == null)
            {
                continue;
            }

            string key = (ui.inventoryName ?? "").Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[UI_Manager] Inventory_UI 'inventoryName' bos.", ui);
                continue;
            }

            if (!inventoryUIByName.ContainsKey(key))
            {
                inventoryUIByName.Add(key, ui);
            }
            else
            {
                Debug.LogWarning($"[UI_Manager] Ayni isimli Inventory_UI zaten var: '{key}'", ui);
            }
        }

        if (toolbarUI == null)
        {
            toolbarUI = FindObjectOfType<Toolbar_UI>(includeInactive: true);
        }
    }
}
