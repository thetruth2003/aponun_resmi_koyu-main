using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestUIManager : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform listParent;

    public TMP_Text mainTitleText;
    public Transform subQuestListParent;
    public GameObject subQuestLinePrefab;
 
    void Start()
    {
        foreach (var q in ActiveQuestSystem.Instance.allQuests)
        {
            var btnObj = Instantiate(buttonPrefab, listParent);
            btnObj.GetComponentInChildren<TMP_Text>().text = q.asset.quests[0].questName;

            var asset = q.asset;
            btnObj.GetComponent<Button>().onClick.AddListener(() => ShowDetails(asset));
        }
    }

    void ShowDetails(QuestEditorAsset asset)
    {
        // 1. Başlık yaz
        mainTitleText.text = asset.quests[0].questName;

        // 2. Öncekileri temizle
        foreach (Transform child in subQuestListParent)
            Destroy(child.gameObject);

        int current = ActiveQuestSystem.Instance.GetCurrentIndex(asset);

        int index = 0;
        for (int i = 0; i < asset.quests.Count; i++)
        {
            var qc = asset.quests[i];
            if (qc.GetStepInstance() == null) continue; // başlıkları atla

            var line = Instantiate(subQuestLinePrefab, subQuestListParent);
            var label = line.GetComponentInChildren<TMP_Text>();
            string status = (index < current) ? "✔ Completed"
                          : (index == current) ? "→ Active"
                          : "(Inactive)";
            label.text = $"{status} - {qc.questName}";
            index++;
        }
    }
}
