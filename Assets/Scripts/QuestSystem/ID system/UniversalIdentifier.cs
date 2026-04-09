using UnityEngine;
/// <summary>
/// Bir nesneye evrensel bir ID ve gerekirse market acma-kapama davranisi baglar.
/// </summary>
public class UniversalIdentifier : MonoBehaviour
{
    public GameObject inventoryUI;
    public GameObject market;
    public GameObject emptyMarket;
    public SC_FPSController SC_FPSController;
    [SerializeField] private string id;
    public string ID => id;

    /// <summary>
    /// Bu nesneye editor veya runtime uzerinden yeni bir tanimlayici degeri atar.
    /// </summary>
    public void SetID(string newID) => id = newID;
    /// <summary>
    /// Bagli market arayuzunu ve gerekli UI objelerini acip oyuncu kontrolunu dondurur.
    /// </summary>
    public void openmarket()
    {
        market.SetActive(true);
        emptyMarket.SetActive(false);
        inventoryUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SC_FPSController.freeze();
    }

    /// <summary>
    /// Market arayuzunu kapatip oyuncunun normal kontrol akisini geri acar.
    /// </summary>
    public void closemarket()
    {
        market.SetActive(false);
        emptyMarket.SetActive(true);
        inventoryUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        SC_FPSController.unfreeze();
    }
}
