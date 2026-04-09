using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrackedQuest = ActiveQuestSystem.TrackedQuest;

/// <summary>
/// QuestUIManager, gorev listesindeki butonlari ve secilen gorevin alt adimlarini gosterir.
/// </summary>
public class QuestUIManager : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform listParent;
    public TMP_Text mainTitleText;
    public Transform subQuestListParent;
    public GameObject subQuestLinePrefab;

    private void Start()
    {
        foreach (TrackedQuest q in ActiveQuestSystem.Instance.allQuests)
        {
            GameObject btnObj = Instantiate(buttonPrefab, listParent);
            btnObj.GetComponentInChildren<TMP_Text>().text = q.asset.quests[0].questName;

            QuestEditorAsset asset = q.asset;
            btnObj.GetComponent<Button>().onClick.AddListener(() => ShowDetails(asset));
        }
    }

    private void ShowDetails(QuestEditorAsset asset)
    {
        mainTitleText.text = asset.quests[0].questName;

        foreach (Transform child in subQuestListParent)
        {
            Destroy(child.gameObject);
        }

        int current = ActiveQuestSystem.Instance.GetCurrentIndex(asset);
        int index = 0;

        for (int i = 0; i < asset.quests.Count; i++)
        {
            QuestContainer qc = asset.quests[i];
            if (qc.GetStepInstance() == null)
            {
                continue;
            }

            GameObject line = Instantiate(subQuestLinePrefab, subQuestListParent);
            TMP_Text label = line.GetComponentInChildren<TMP_Text>();
            string status = (index < current) ? "[OK] Completed"
                          : (index == current) ? "-> Active"
                          : "(Inactive)";

            label.text = $"{status} - {qc.questName}";
            index++;
        }
    }
}
