using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar_UI : MonoBehaviour
{
    public Movement movement;
    public List<Slot_UI> toolbarSlots = new List<Slot_UI>();
    private Slot_UI selectedSlot;

    private void Start()
    {
        // Başlangıçta ilk slotu seç
        SelectSlot(0);
    }

    private void Update()
    {
        CheckAlphaNumericKeys();
    }
    public void SelectSlot(Slot_UI slot)
    {
        SelectSlot(slot.slotID);
    }

    public string GetSelectedPrefab()
    {
        // Seçilen slotta geçerli bir eşya varsa, prefab'ı döndür
        return selectedSlot != null && selectedSlot.inventorySlot != null ? selectedSlot.inventorySlot.itemPrefab.name : null;
    }
    public string GetSelectedPrefabTag()
    {
        // Seçilen slotta geçerli bir eşya varsa, prefab'ı döndür
        return selectedSlot != null && selectedSlot.inventorySlot != null ? selectedSlot.inventorySlot.itemPrefab.tag : null;
    }
    public string GetSelectedUsedPrefab()
    {
        return selectedSlot != null && selectedSlot.inventorySlot != null
     ? selectedSlot.inventorySlot.itemUsedPrefab != null
         ? selectedSlot.inventorySlot.itemUsedPrefab.name : null : null;
    }

    public void SelectSlot(int index)
    {
        if (toolbarSlots.Count == 9)
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetHighlight(false);
            }

            selectedSlot = toolbarSlots[index];
            selectedSlot.SetHighlight(true);

            // GameManager ve player'ın null olmadığını kontrol et
            if (GameManager.instance.player == null)
            {
                Debug.LogError("Player nesnesi GameManager içinde atanmadı!");
                return; // Eğer player null ise, işlemi durdur
            }

            // Player'ın UpdateHandObject metodunu çağır
            GameManager.instance.player.UpdateHandObject();

            // Seçili slotta bir eşya varsa, item adı ve prefab'ını kontrol et
            if (selectedSlot.inventorySlot != null && selectedSlot.inventorySlot.itemPrefab != null)
            {
                Debug.Log("Selected item: " + selectedSlot.inventorySlot.itemPrefab.name);
            }
            else
            {
                Debug.Log("Seçilen slotta item yok.");
            }

            GameManager.instance.player.inventoryManager.toolbar.SelectSlot(index);
        }
    }

    private void CheckAlphaNumericKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SelectSlot(8);
    }
}
