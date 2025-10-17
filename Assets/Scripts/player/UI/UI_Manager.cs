using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    public Dictionary<string, Inventory_UI> inventoryUIByName = new Dictionary<string, Inventory_UI>(); // Envanterleri saklamak için sözlük
    public List<Inventory_UI> inventoryUIs; // Envanterlerin listesi
    public GameObject inventoryPanel; // Envanter paneli
    public GameObject MenuPanel; // menuneli
    public GameObject Stamina; // Envanter paneli
    public Camera playerCamera; // Oyuncunun kamerası
    public float maxDistance = 100f; // Raycast mesafesi
    public GameObject player; // Oyuncu karakteri
    public static Slot_UI draggedSlot;
    public static Image draggedIcon;
    public static bool dragSingle;
    public SC_FPSController playerMovementScript; 
    public GameObject Crosshair;
    public GameObject CrosshairCanvas;
    private bool isMenuOpen = false; // Menü durumu
// --- class alanlarına EKLE (diğer alanların yanına) ---
[Header("Toolbar (opsiyonel)")]
[SerializeField] private Toolbar_UI toolbarUI;   // inspector’dan atayabilirsin; boş kalırsa biz buluruz.

// --- Initialize() SONUNA EKLE ---


// --- class içine YENİ yardımcılar ---
public int GetToolbarSelectedIndex()
{
    return toolbarUI ? toolbarUI.GetSelectedIndex() : -1;
}

public void SelectToolbarSlot(int index)
{
    if (toolbarUI) toolbarUI.SelectSlot(index);
}

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        // OYUN AÇILIR AÇILMAZ ENVANTERİ AÇMA – erken refresh’e sebep oluyordu.
        // Eğer başlangıçta açmak istiyorsan, 1 frame geciktir:
        // StartCoroutine(OpenInventoryNextFrame());
    }

    private IEnumerator OpenInventoryNextFrame()
    {
        yield return null; // bir frame bekle
        ToggleInventoryUI();
    }

    public void Update()
    {
                if (PauseMenuUI.IsInputLocked)
        return; // 👈 Menü açıkken hiçbir tuş çalışmaz (ESC dışında)
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
            ToggleMenuUI(); // Menü aç
            isMenuOpen = true;         // Durum güncellenir
        }
        else if (Input.GetKeyUp(KeyCode.Q) && isMenuOpen)
        {
            ToggleMenuUI(); // Menü kapat
            isMenuOpen = false;         // Durum güncellenir
        }
    }

    private void ChestOpen()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition); // Ekrandan ray oluştur
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Chest"))
            {
                ToggleInventoryUI(); // Sandık açıldığında envanteri aç
            }
            else
            {
                ToggleInventoryUI();
            }
        }

        dragSingle = Input.GetKey(KeyCode.LeftShift);
    }

    // Envanteri açma/kapama fonksiyonu
    public void ToggleInventoryUI()
    {
        if (inventoryPanel == null) return;

        bool willOpen = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(willOpen);

        if (willOpen)
        {
            
            // Mouse
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerMovementScript != null) playerMovementScript.enabled = false;
            if (Crosshair != null) Crosshair.SetActive(false);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(false);

            // ✅ Güvenli refresh: 1 frame geciktir + hazır olana kadar bekle
            StartCoroutine(SafeRefreshInventoryUI("backpack"));
        }
        else
        {
            // Mouse
            if (playerMovementScript != null) playerMovementScript.enabled = true;
            if (Crosshair != null) Crosshair.SetActive(true);
            if (CrosshairCanvas != null) CrosshairCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ToggleMenuUI()
    {
        if (MenuPanel == null) return;

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

    void DeactivateMenuAndChildren(GameObject menu)
    {
        foreach (Transform child in menu.transform)
        {
            child.gameObject.SetActive(false);
        }
        if (menu.transform.childCount > 0)
            menu.transform.GetChild(0).gameObject.SetActive(true);
    }

    // === GÜVENLİ REFRESH ===
    public void RefreshInventoryUI(string inventoryName)
    {
        // Eski doğrudan çağrı NRE’ye sebep oluyordu.
        StartCoroutine(SafeRefreshInventoryUI(inventoryName));
    }

    private IEnumerator SafeRefreshInventoryUI(string inventoryName)
    {
        // 1 frame bekle: tüm Awake/Start zinciri tamamlansın
        yield return null;

        if (!inventoryUIByName.TryGetValue(inventoryName, out var invUI) || invUI == null)
        {
            Debug.LogWarning($"[UI_Manager] Inventory UI bulunamadı: '{inventoryName}'");
            yield break;
        }

        // En fazla ~1 saniye bekle (60 frame) – Inventory_UI tarafı hazır olana kadar
        const int maxWait = 60;
        int waited = 0;
        while (!invUI.InventoryIsReady() && waited < maxWait)
        {
            waited++;
            yield return null;
        }

        if (!invUI.InventoryIsReady())
        {
            Debug.LogWarning($"[UI_Manager] '{inventoryName}' inventory hazır olmadı (timeout). Refresh atlandı.");
            yield break;
        }

        invUI.Refresh();
    }

    // Tüm envanterleri yenileme
    public void RefreshAll()
    {
        foreach (KeyValuePair<string, Inventory_UI> kv in inventoryUIByName)
        {
            var ui = kv.Value;
            if (ui == null) continue;

            if (ui.InventoryIsReady())
                ui.Refresh();
            else
                StartCoroutine(SafeRefreshInventoryUI(kv.Key));
        }
    }

    // Envanteri al
    public Inventory_UI GetInventoryUI(string inventoryName)
    {
        if (inventoryUIByName.TryGetValue(inventoryName, out var ui))
            return ui;
        return null;
    }

    // Envanter UI'lerini başlat
    private void Initialize()
    {
        inventoryUIByName.Clear();
        if (inventoryUIs == null) return;

        foreach (Inventory_UI ui in inventoryUIs)
        {
            if (ui == null) continue;
            var key = (ui.inventoryName ?? "").Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[UI_Manager] Inventory_UI 'inventoryName' boş.", ui);
                continue;
            }
            if (!inventoryUIByName.ContainsKey(key))
            {
                inventoryUIByName.Add(key, ui);
            }
            else
            {
                Debug.LogWarning($"[UI_Manager] Aynı isimli Inventory_UI zaten var: '{key}'", ui);
            }
        }
        if (toolbarUI == null)
        toolbarUI = FindObjectOfType<Toolbar_UI>(includeInactive: true);
    }
    
}
