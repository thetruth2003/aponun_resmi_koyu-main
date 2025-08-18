using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;

    [Header("Buttons")]
    public Button continueButton;

    // Basit "save var mı?" anahtarı
    private const string SAVE_EXISTS_KEY = "SaveExists";
    private const string LAST_SCENE_KEY  = "LastScene"; // istersen

    private void Start()
    {
        // Continue, kayıt yoksa pasif
        bool hasSave = PlayerPrefs.GetInt(SAVE_EXISTS_KEY, 0) == 1;
        if (continueButton != null)
            continueButton.interactable = hasSave;

        // Güvenlik
        if (mainPanel)    mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    // --- MainPanel butonları ---
    public void OnClick_NewGame()
    {
        // Yeni oyun: save’ı sıfırla
        PlayerPrefs.DeleteKey(SAVE_EXISTS_KEY);
        PlayerPrefs.DeleteKey(LAST_SCENE_KEY);
        PlayerPrefs.Save();

        // İstersen önce "Loading" sahnesi vs. ekleyebilirsin
        SceneManager.LoadScene("Game"); // oyun sahnenin ismi
    }

    public void OnClick_Continue()
    {
        // Devam: basit yaklaşım → aynı "Game" sahnesini aç
        // ve sahne açılınca SaveLoadManager.LoadAll() çağrılır (oyun tarafında).
        if (PlayerPrefs.GetInt(SAVE_EXISTS_KEY, 0) == 1)
        {
            // İstersen son sahneyi de okuyabilirsin:
            // string lastScene = PlayerPrefs.GetString(LAST_SCENE_KEY, "Game");
            SceneManager.LoadScene("Game");
        }
    }

    public void OnClick_Options()
    {
        if (mainPanel)    mainPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    public void OnClick_Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- OptionsPanel butonları ---
    public void OnClick_Back()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainPanel)    mainPanel.SetActive(true);
    }
}
