/*
    "Create 3 placeholder tower, they are just big RGB square that does nothing
    and cost 0 and can be cross merge into each other to make other color."

    Same runtime-only-template technique as WoodFarmTower.cs/WoodFarmMergeSetup.cs
    - no hand-authored prefab assets, no scene/prefab wiring. This file only
    defines what one of these towers *is*: zero cost, zero combat stats, and a
    no-op shoot() as a second, explicit guard against ever attacking (mirrors
    WoodFarmTower's own reasoning - range/damage/timeBtwShots == 0 already means
    Towers.updateClosestEnemy()/TowerLogic() never fire it, this is belt-and-braces).

    See PlaceholderTowerSetup.cs for where the three instances (Red/Green/Blue)
    are actually built, tinted, put in the shop, and cross-merged into the three
    secondary colors (Yellow/Cyan/Magenta).
*/
using UnityEngine;

public class PlaceholderTower : Towers {
    public void Configure(string name, Color tint) {
        towerName = name;
        towerCost = 0;
        upgradeCost = 0;
        range = 0f;
        damage = 0f;
        timeBtwShots = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tint;

        // "big ... square" - roughly 1.5x a normal tower's footprint.
        transform.localScale = new Vector3(1.5f, 1.5f, 1f);
    }

    protected override void shoot() {
        // Intentionally does nothing - placeholder squares never attack.
    }

    public override string GetCurrentStatsText() {
        return "Placeholder - no combat stats";
    }

    public override string GetUpgradePreviewText() {
        return "Placeholder tower - nothing to upgrade";
    }
}
