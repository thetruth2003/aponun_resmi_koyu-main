using UnityEngine;

/// <summary>
/// Oyuncu, UI ve temel oyun yoneticilerini merkezi olarak bir arada tutar.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public ItemManager itemManager;
    public UI_Manager uiManager;
    public StateManger stateManger;
    public Player player;
    public Toolbar_UI toolbarUI;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        itemManager = GetComponent<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning("GameManager: ItemManager bileseni atanmadý!");
        }

        uiManager = GetComponent<UI_Manager>();
        if (uiManager == null)
        {
            Debug.LogWarning("GameManager: UI_Manager bileseni atanmadý!");
        }

        player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("GameManager: Oyuncu (Player) nesnesi sahnede bulunamadý!");
        }

        if (toolbarUI == null)
        {
            Debug.LogWarning("GameManager: Toolbar_UI referansý atanmadý!");
        }
    }
}
