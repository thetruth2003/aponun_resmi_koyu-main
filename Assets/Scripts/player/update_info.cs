using UnityEngine;
using TMPro;

public class update_info : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxDistance = 5f;

    [Header("NPC UI")]
    public GameObject NpcInfoPanel;
    public TextMeshProUGUI Npcname;

    [Header("Item UI")]
    public GameObject itemInfoPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Muhasebeci money;   // mevcut Money script'in

    private void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            bool somethingShown = false;

            // ===== NPC BİLGİSİ =====
            UniversalIdentifier npc = hit.collider.GetComponent<UniversalIdentifier>();
            if (npc == null) npc = hit.collider.GetComponentInParent<UniversalIdentifier>();

            if (npc != null)
            {
                NpcInfoPanel.SetActive(true);
                Npcname.text = string.IsNullOrWhiteSpace(npc.ID) ? npc.gameObject.name : npc.ID;
                somethingShown = true;
            }
            else
            {
                NpcInfoPanel.SetActive(false);
            }

            // ===== ITEM (TOOLS) BİLGİSİ =====
            Tools item = hit.collider.GetComponent<Tools>();
            if (item == null) item = hit.collider.GetComponentInParent<Tools>();

            if (item != null)
            {
                itemInfoPanel.SetActive(true);
                itemNameText.text = item.itemName;
                itemPriceText.text = item.price.ToString();

                int currentMoney;
                if (!int.TryParse(money.moneyText.text, out currentMoney))
                {
                    Debug.LogWarning("Parayı parse edemedim: " + money.moneyText.text);
                    currentMoney = 0;
                }

                bool canAfford = currentMoney >= item.price;
                itemNameText.color = canAfford ? Color.green : Color.red;
                itemPriceText.color = canAfford ? Color.green : Color.red;

                somethingShown = true;
            }
            else
            {
                itemInfoPanel.SetActive(false);
            }

            if (!somethingShown)
            {
                NpcInfoPanel.SetActive(false);
                itemInfoPanel.SetActive(false);
            }
        }
        else
        {
            // Hiçbir şeye bakmıyorsan her şeyi kapat
            NpcInfoPanel.SetActive(false);
            itemInfoPanel.SetActive(false);
        }
    }
}
