using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public ItemManager itemManager;
    public UI_Manager uiManager;
    public StateManger stateManger;
    public Player player;
    public Toolbar_UI toolbarUI; // Toolbar UI referansı

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

        DontDestroyOnLoad(this.gameObject);

        // Bileşenlerin atanıp atanmadığını kontrol ediyoruz
        itemManager = GetComponent<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning("GameManager: ItemManager bileşeni atanmadı!");
        }
        uiManager = GetComponent<UI_Manager>();
        if (uiManager == null)
        {
            Debug.LogWarning("GameManager: UI_Manager bileşeni atanmadı!");
        }

        player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("GameManager: Oyuncu (Player) nesnesi sahnede bulunamadı!");
        }

        if (toolbarUI == null)
        {
            Debug.LogWarning("GameManager: Toolbar_UI referansı atanmadı!");
        }
    }
}
