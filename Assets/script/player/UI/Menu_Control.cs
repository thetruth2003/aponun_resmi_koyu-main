using System.Collections.Generic;
using UnityEngine;

public class Menu_Control : MonoBehaviour
{
    // Ana menü GameObject'i (radial menu için)
    public GameObject mainMenu;

    // Diğer radial menüler (Aletler, Tarım, Çit vb.)
    public GameObject toolsMenu;       // Aletler
    public GameObject farmingMenu;    // Tarım
    public GameObject fenceMenu;      // Çit
    public GameObject treesMenu;      // Ağaçlar
    public GameObject financeMenu;    // Finans
    public GameObject itemsMenu;      // Eşya
    public GameObject foodMenu;       // Yemek
    public GameObject blacksmithMenu; // Demircilik
    public GameObject buildingsMenu;  // Binalar

    // Tüm menülerin listesini tut
    private List<GameObject> allMenus;


    // Bir menüyü aktif eder, diğerlerini kapatır
    public void ActivateMenu(GameObject menuToActivate)
    {
        // Ana menüyü de kapat
        if (mainMenu != null)
            mainMenu.SetActive(false);

        // İstenen menüyü aktif et
        if (menuToActivate != null)
            menuToActivate.SetActive(true);
    }

    // Her buton için çağrılacak fonksiyonlar
    public void OpenToolsMenu() => ActivateMenu(toolsMenu);
    public void OpenFarmingMenu() => ActivateMenu(farmingMenu);
    public void OpenFenceMenu() => ActivateMenu(fenceMenu);
    public void OpenTreesMenu() => ActivateMenu(treesMenu);
    public void OpenFinanceMenu() => ActivateMenu(financeMenu);
    public void OpenItemsMenu() => ActivateMenu(itemsMenu);
    public void OpenFoodMenu() => ActivateMenu(foodMenu);
    public void OpenBlacksmithMenu() => ActivateMenu(blacksmithMenu);
    public void OpenBuildingsMenu() => ActivateMenu(buildingsMenu);

    // Ana menüye geri dönme fonksiyonu
    public void OpenMainMenu() => ActivateMenu(mainMenu);
}
