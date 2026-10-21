/*
    Builder for the 3 placeholder RGB square towers (see PlaceholderTower.cs for
    what they *are*). Called once from DeckSelectionUI.Show() via
    PlaceholderTowerSetup.BuildAndRegister(this, menuSource) - not self-installing
    on its own, since it needs a DeckSelectionUI to hand the new shop entries to
    and an existing tower's `menu` reference to borrow (see menuSource below,
    same trick WoodFarmMergeSetup.cs uses for its own merge-result template).

    Builds 6 runtime-only templates total, all inactive/DontDestroyOnLoad, never
    placed themselves - only ever Instantiate()'d from by PlacementManager, same
    as WoodFarmMergeSetup's Wood Farm Tower:
      - Red / Green / Blue Square Tower: the 3 pickable base towers.
      - Yellow / Cyan / Magenta Square Tower: merge results only, not directly
        pickable (mirrors Wood Farm Tower never having its own shop entry either).

    "Cross merge into each other" - registered bidirectionally (Red+Green and
    Green+Red both resolve to Yellow, etc.) since Towers.TryGetMergeResult() is
    one-directional per recipe (see Towers.cs) - matching the intent, not just
    the literal one-directional mechanism the codebase already has elsewhere.
*/
using UnityEngine;
using UnityEngine.UI;

public static class PlaceholderTowerSetup {
    public static void BuildAndRegister(DeckSelectionUI owner, GameObject menuSource) {
        if (owner == null) return;

        GameObject red = BuildTemplate("Red Square Tower", Color.red, menuSource);
        GameObject green = BuildTemplate("Green Square Tower", Color.green, menuSource);
        GameObject blue = BuildTemplate("Blue Square Tower", Color.blue, menuSource);

        GameObject yellow = BuildTemplate("Yellow Square Tower", Color.yellow, menuSource);
        GameObject cyan = BuildTemplate("Cyan Square Tower", Color.cyan, menuSource);
        GameObject magenta = BuildTemplate("Magenta Square Tower", Color.magenta, menuSource);

        RegisterCrossMerge(red, green, yellow);
        RegisterCrossMerge(green, blue, cyan);
        RegisterCrossMerge(red, blue, magenta);

        owner.AddShopEntry(red, new Color(1f, 0.3f, 0.3f));
        owner.AddShopEntry(green, new Color(0.3f, 1f, 0.3f));
        owner.AddShopEntry(blue, new Color(0.3f, 0.5f, 1f));
    }

    private static void RegisterCrossMerge(GameObject a, GameObject b, GameObject result) {
        Towers ta = a.GetComponent<Towers>();
        Towers tb = b.GetComponent<Towers>();
        if (ta == null || tb == null) return;

        ta.AddMergeRecipe(tb.getName(), result);
        tb.AddMergeRecipe(ta.getName(), result);
    }

    private static GameObject BuildTemplate(string name, Color tint, GameObject menuSource) {
        GameObject go = new GameObject(name);
        go.SetActive(false); // template only, never runs on its own - PlacementManager Instantiate()s from it
        Object.DontDestroyOnLoad(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BuildWhiteSquareSprite();
        sr.sortingOrder = 5;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        PlaceholderTower tower = go.AddComponent<PlaceholderTower>();
        tower.Configure(name, tint);

        // Towers.OnMouseDown() needs `menu` (the Upgrade UI panel) set or clicking
        // this tower throws a NullReferenceException - borrow whatever the first
        // real tower card is already wired to, same fix WoodFarmMergeSetup.cs
        // applies for its own merge-result template.
        if (menuSource != null) {
            Towers menuSourceTower = menuSource.GetComponent<Towers>();
            if (menuSourceTower != null) tower.menu = menuSourceTower.menu;
        }

        return go;
    }

    private static Sprite BuildWhiteSquareSprite() {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }
}
