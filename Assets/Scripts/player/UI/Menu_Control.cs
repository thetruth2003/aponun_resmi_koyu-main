using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Radyal menuler arasinda gecis yapip sadece secili menuyu acik tutar.
/// </summary>
public class Menu_Control : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject toolsMenu;
    public GameObject farmingMenu;
    public GameObject fenceMenu;
    public GameObject treesMenu;
    public GameObject financeMenu;
    public GameObject itemsMenu;
    public GameObject foodMenu;
    public GameObject blacksmithMenu;
    public GameObject buildingsMenu;

    private List<GameObject> allMenus;

    public void ActivateMenu(GameObject menuToActivate)
    {
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }

        if (menuToActivate != null)
        {
            menuToActivate.SetActive(true);
        }
    }

    public void OpenToolsMenu() => ActivateMenu(toolsMenu);
    public void OpenFarmingMenu() => ActivateMenu(farmingMenu);
    public void OpenFenceMenu() => ActivateMenu(fenceMenu);
    public void OpenTreesMenu() => ActivateMenu(treesMenu);
    public void OpenFinanceMenu() => ActivateMenu(financeMenu);
    public void OpenItemsMenu() => ActivateMenu(itemsMenu);
    public void OpenFoodMenu() => ActivateMenu(foodMenu);
    public void OpenBlacksmithMenu() => ActivateMenu(blacksmithMenu);
    public void OpenBuildingsMenu() => ActivateMenu(buildingsMenu);
    public void OpenMainMenu() => ActivateMenu(mainMenu);
}
