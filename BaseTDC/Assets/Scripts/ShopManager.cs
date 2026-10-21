

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager main;

    // Re-wire to WoodManager in Inspector (was MoneyManager moneyManager).
    public WoodManager woodManager;

    // Wire to EctoManager in Inspector - boons now cost Ectos, not Wood.
    public EctoManager ectoManager;

    private void Start()
    {
        if (main == null) main = this;
    }

    public int GetTowerCost(GameObject towerPrefab)
    {
        return towerPrefab.GetComponent<Towers>().getCost();
    }

    public int GetUpgradeCost(GameObject towerPrefab)
    {
        return towerPrefab.GetComponent<Towers>().getUpgradeCost();
    }

    // Boon cost is now an Ecto cost.
    public int GetBoonCost(GameObject boonPrefab)
    {
        return boonPrefab.GetComponent<Boon>().getCost();
    }

    public void buyTower(GameObject towerPrefab)
    {
        woodManager.RemoveWood(GetTowerCost(towerPrefab));
    }

    public void sellTower(GameObject towerPrefab)
    {
        woodManager.AddWood(GetTowerCost(towerPrefab));
    }

    public void upgradeTower(GameObject towerPrefab)
    {
        woodManager.RemoveWood(GetUpgradeCost(towerPrefab));
    }

    public bool canUpgradeTower(GameObject towerPrefab)
    {
        return woodManager.GetCurrWood() >= GetUpgradeCost(towerPrefab);
    }

    // buyBoon / canBuyBoon now check Ectos, not Wood.
    public void buyBoon(GameObject boonPrefab)
    {
        ectoManager.RemoveEctos(GetBoonCost(boonPrefab));
    }

    public bool canBuyTower(GameObject towerPrefab)
    {
        return woodManager.GetCurrWood() >= GetTowerCost(towerPrefab);
    }

    public bool canBuyBoon(GameObject boonPrefab)
    {
        return ectoManager.GetCurrEctos() >= GetBoonCost(boonPrefab);
    }
}
