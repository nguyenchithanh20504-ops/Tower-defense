/*
    "Create a new scene that would showcase and make player go through every
    single game function that it have, minimum of 40 functions in total."

    ---------------------------------------------------------------------
    Revision notes (latest pass): "Make Showcase scene inherit everything from
    Scene... so no copying." There is no more ShowcaseScene.unity/.meta and no
    more appended EditorBuildSettings.asset entry - this checklist now overlays
    the real "Scene" directly instead of a duplicated copy of it, so any future
    change to Scene.unity is automatically reflected here with nothing to keep
    in sync by hand.

    Gating changed to match: instead of checking the active scene's name against
    a second scene, ShowcaseLauncher.cs (on StartScene) calls the static
    RequestShowcaseMode() below immediately before loading "Scene" - the request
    is consumed (reset to false) the moment it's read, so a later, ordinary
    "New Game" load of "Scene" never accidentally shows the checklist.

    Also removed the "Back to Menu" button - PauseMenu already has its own
    "Load Menu" exit ([ESC] to open Pause, per toDo2.txt), and having two
    separate ways to leave the scene was redundant.
    ---------------------------------------------------------------------
    Revision notes (previous pass): two problems with the original 45-item list
    were fixed without touching any other file.

    1) PADDING, NOT FUNCTION COUNT: "Placing Towers" used to list Archer /
       Ballista / Frost / Fire / Lightning / Trebuchet as six separate rows,
       but every one of them calls the exact same PlacementManager.PlaceBuilding()
       - the only thing that changes is which prefab string got passed in. That's
       one function exercised six times, not six functions. Below, every row is
       commented with the specific method it actually exercises, and no two rows
       point at the same method with only an argument swapped. The tower-variety
       rows were replaced with rows that hit methods nothing in the old list ever
       touched: UIScrollViewController (shop scroll/show-hide), DeckSelectionUI's
       mid-match gameplay-slot drag (distinct from the pre-match deck-build
       drag), CostDisplay, PlacementManager.StartPlacing (distinct from
       PlaceBuilding - "pick a tower up" vs. "drop it"),
       UIExpandButtonController/ExpandButton, MapGenerator.randomExpand, and the
       enemy Ability subclasses (HealerAbility.castHealing,
       SpawnerAbility.spawnEnemies, SpecialEnemy.useSpecialAbility). Net result:
       48 rows mapping to 48 distinct methods/state-transitions, comfortably
       over the 40 minimum on an honest count.

    2) SCREEN COVERAGE: the old panel was anchored (0,0)-(0,1) at 400px wide -
       i.e. the full height of the left edge of the screen, permanently. It's
       now a small collapsible header (progress count + one toggle button) that
       starts collapsed; expanding it opens a capped-height scroll view instead
       of an ever-growing full-height list, and the panel is narrower (300px).
       Nothing is drawn full-height anymore.
    ---------------------------------------------------------------------

    48 items across 11 categories, each either:
      - Auto-detected: polled every frame against real game state (a specific
        tower name appearing in Counter.towers, a status effect present on some
        enemy, a manager balance dropping, a static "EverX" flag flipped by the
        script that owns that action - see Towers.cs/UpgradeManager.cs/
        DevConsole.cs/CameraResetButton.cs/EnemyInfoPanel.cs/TowerHotkeys.cs/
        WoodGainPopup.cs, each carrying one of these flags for exactly this
        purpose), or
      - Manual: no cheap, unambiguous state to poll for (e.g. "hovered a tower's
        cooldown bar") - the player taps the row themselves once they've done it.
        This pass adds no new EverX flags to other files (kept to a single-file
        change), so every newly-added row is manual-confirm.

    Once auto-detected true, an item latches done permanently even if the
    underlying state later reverts (e.g. a Boon expiring, timeScale going back
    to 1) - the point is "did you ever do this", not "is this true right now".

    Self-installing, no scene/prefab wiring beyond the scene duplication above.
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FunctionShowcase : MonoBehaviour {
    private const string GameplaySceneName = "Scene";

    // Set by ShowcaseLauncher.cs right before it loads "Scene", consumed (reset
    // to false) the instant HandleSceneChange reads it - so this only ever
    // applies to the one load it was requested for, and an ordinary later
    // "New Game" -> "Scene" load is unaffected.
    private static bool pendingShowcaseMode;
    public static void RequestShowcaseMode() { pendingShowcaseMode = true; }

    private class Item {
        public string Category;
        public string Title;
        public string How;
        public Func<bool> AutoCheck; // null = manual only
        public bool Done;
        public Text RowText;
    }

    private readonly List<Item> items = new List<Item>();
    private Text summaryText;
    private Text toggleArrowText;
    private GameObject scrollArea;
    private bool expanded = false;
    private bool builtUi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<FunctionShowcase>() != null) return;
        GameObject go = new GameObject("~FunctionShowcase");
        DontDestroyOnLoad(go);
        go.AddComponent<FunctionShowcase>();
    }

    private void Awake() {
        SceneManager.sceneLoaded += (scene, mode) => HandleSceneChange(scene.name);
        HandleSceneChange(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneChange(string sceneName) {
        bool active = false;
        if (sceneName == GameplaySceneName && pendingShowcaseMode) {
            pendingShowcaseMode = false; // consume - one-shot per request
            active = true;
        }
        if (uiRoot != null) uiRoot.SetActive(active);

        if (active && !builtUi) {
            BuildItemList();
            BuildUi();
            builtUi = true;
        }
    }

    private void Update() {
        if (uiRoot == null || !uiRoot.activeSelf) return;

        bool anyChanged = false;
        foreach (Item item in items) {
            if (item.Done || item.AutoCheck == null) continue;
            if (item.AutoCheck()) {
                item.Done = true;
                anyChanged = true;
            }
        }

        if (anyChanged) RefreshRows();
    }

    // ------------------------------------------------------------------
    // Checklist content
    // ------------------------------------------------------------------

    private static bool AnyTowerNamed(string name) {
        foreach (GameObject t in Counter.towers) {
            if (t == null) continue;
            Towers tw = t.GetComponent<Towers>();
            if (tw != null && tw.getName() == name) return true;
        }
        return false;
    }

    private static bool AnyTowerPlaced() {
        foreach (GameObject t in Counter.towers) {
            if (t != null) return true;
        }
        return false;
    }

    private static bool AnyTowerUpgraded() {
        foreach (GameObject t in Counter.towers) {
            if (t == null) continue;
            Towers tw = t.GetComponent<Towers>();
            if (tw != null && tw.upgraded) return true;
        }
        return false;
    }

    private static bool AnyEnemyWithStatus(StatusType type) {
        foreach (GameObject e in Counter.enemies) {
            if (e == null) continue;
            Enemy en = e.GetComponent<Enemy>();
            if (en == null || en.Statuses == null) continue;
            foreach (Status s in en.Statuses) {
                if (s.statusType == type) return true;
            }
        }
        return false;
    }

    private static bool AnyHiddenEnemy() {
        foreach (GameObject e in Counter.enemies) {
            if (e == null) continue;
            Enemy en = e.GetComponent<Enemy>();
            if (en != null && en.IsHidden) return true;
        }
        return false;
    }

    private int baselineLives = int.MinValue;
    private bool MainHallEverDamaged() {
        int current = Mathf.RoundToInt(HealthBar.lives);
        if (baselineLives == int.MinValue) { baselineLives = current; return false; }
        return current < baselineLives;
    }

    private int baselineWood = int.MinValue;
    private bool EverSpentWood() {
        if (WoodManager.main == null) return false;
        int current = WoodManager.main.GetCurrWood();
        if (baselineWood == int.MinValue) { baselineWood = current; return false; }
        bool spent = current < baselineWood;
        baselineWood = Mathf.Min(baselineWood, current);
        return spent;
    }

    private int baselineEcto = int.MinValue;
    private bool EverSpentEcto() {
        if (EctoManager.main == null) return false;
        int current = EctoManager.main.GetCurrEctos();
        if (baselineEcto == int.MinValue) { baselineEcto = current; return false; }
        bool spent = current < baselineEcto;
        baselineEcto = Mathf.Min(baselineEcto, current);
        return spent;
    }

    private int baselineRound = int.MinValue;
    private bool RoundEverAdvanced() {
        if (RoundController.main == null) return false;
        int current = RoundController.main.round;
        if (baselineRound == int.MinValue) { baselineRound = current; return false; }
        return current != baselineRound;
    }

    private static DeckSelectionUI FindDeck() {
        return FindObjectOfType<DeckSelectionUI>();
    }

    private void BuildItemList() {
        void Add(string cat, string title, string how, Func<bool> auto = null) {
            items.Add(new Item { Category = cat, Title = title, How = how, AutoCheck = auto });
        }

        // -- Match Setup (3) --------------------------------------------
        Add("Match Setup", "Pick towers for your deck", "Drag or click tower cards on the deck screen",
            () => PlayerLoadout.SelectedTowers.Count > 0);
        Add("Match Setup", "Ready up", "Click the Ready button once your deck is picked",
            () => FindDeck() != null && FindDeck().IsReady);
        Add("Match Setup", "Start the match", "Click Start Wave to begin",
            () => FindDeck() != null && FindDeck().MatchStarted);

        // -- Placing & Merging (4) -- was 6 rows all calling PlaceBuilding()
        // with a different tower name; now 4 rows, 4 distinct methods.
        Add("Placing & Merging", "Pick a tower up from the deck bar", "Click or press a deck hotkey to start placing",
            null); // PlacementManager.StartPlacing() - distinct from the drop step below
        Add("Placing & Merging", "Drop a tower onto the field", "Click/drag a picked-up tower onto a buildable tile",
            AnyTowerPlaced); // PlacementManager.PlaceBuilding()
        Add("Placing & Merging", "Merge two towers into a Wood Farm Tower", "Place one tower onto another's tile",
            () => AnyTowerNamed("Wood Farm Tower")); // Towers.TryGetMergeResult()
        Add("Placing & Merging", "Place a Boon", "Buy a Boon from the shop and drop it on the field",
            null); // PlacementManager.PlaceBoon()

        // -- Tower Management (4) ---------------------------------------
        Add("Tower Management", "Open a tower's Upgrade menu", "Click any placed tower",
            null); // UpgradeManager.Open()
        Add("Tower Management", "Upgrade a tower", "Click Upgrade in the tower menu",
            AnyTowerUpgraded); // UpgradeManager.Upgrade() / Towers.upgrade()
        Add("Tower Management", "Sell a tower", "Click Sell in the tower menu",
            () => UpgradeManager.EverSoldATower); // UpgradeManager.Sell()
        Add("Tower Management", "Cycle a tower's target mode", "Press [R] with a tower selected, or the menu's Target text",
            () => Towers.EverCycledTarget); // UpgradeManager.CycleTarget() / Towers.CycleTargetMode()

        // -- Active Skills & Info (5) -------------------------------------
        Add("Active Skills & Info", "Manually trigger a tower's Active Skill", "Press [F] with a skill-ready tower selected",
            () => Towers.EverUsedActiveSkill); // Towers.UseActiveSkill()
        Add("Active Skills & Info", "Toggle a skill between Manual/Automatic", "Press [Z] with a tower selected",
            () => Towers.EverToggledSkillMode); // Towers.ToggleSkillMode()
        Add("Active Skills & Info", "Hover a tower to see its cooldown bar", "Mouse over any placed tower",
            null); // TowerCooldownDisplay
        Add("Active Skills & Info", "Hover a tower to see its range circle", "Mouse over any placed tower",
            null); // TowerRangeIndicator
        Add("Active Skills & Info", "Right-click an enemy for its info panel", "Right-click any enemy on the field",
            () => EnemyInfoPanel.EverOpened);

        // -- Combat & Status (4) ------------------------------------------
        Add("Combat & Status", "Freeze an enemy", "Hit one with a Frost Tower",
            () => AnyEnemyWithStatus(StatusType.Frozen));
        Add("Combat & Status", "Burn an enemy", "Hit one with a Fire Tower",
            () => AnyEnemyWithStatus(StatusType.Burning));
        Add("Combat & Status", "Stun an enemy", "Hit one with a Lightning Tower",
            () => AnyEnemyWithStatus(StatusType.Stunned));
        Add("Combat & Status", "Trigger a Lightning Tower's upgraded AoE explosion", "Fully upgrade a Lightning Tower and let it fire",
            null); // ElementalShot AoE branch

        // -- Enemy Behaviors (5) --------------------------------------------
        Add("Enemy Behaviors", "Hover an enemy for a quick HP/status readout", "Mouse over any enemy",
            null); // EnemyHoverInfo
        Add("Enemy Behaviors", "See a Hidden enemy (needs a Detection tower)", "Wait for a stealth enemy to spawn",
            AnyHiddenEnemy); // Enemy.SetHidden / IsHidden
        Add("Enemy Behaviors", "Watch an enemy Healer heal an ally", "Let a Healer-type enemy reach a damaged ally",
            null); // HealerAbility.castHealing()
        Add("Enemy Behaviors", "Watch an enemy Spawner summon adds", "Let a Spawner-type enemy use its ability",
            null); // SpawnerAbility.spawnEnemies()
        Add("Enemy Behaviors", "See a Special enemy use its unique ability", "Watch a Special enemy in a later wave",
            null); // SpecialEnemy.useSpecialAbility()

        // -- Map & Camera (5) -----------------------------------------------
        Add("Map & Camera", "Pan and zoom the camera", "Use your normal camera controls",
            null);
        Add("Map & Camera", "Highlight the enemy pathway", "Hold [Shift], or hover an enemy for its path line",
            null); // PathHighlighter
        Add("Map & Camera", "Reset a lost camera", "Click \"Return to Hall\" (top right) or press [Home]",
            () => CameraResetButton.EverClicked); // CameraController.ReturnToMainHall()
        Add("Map & Camera", "Expand the map via an expansion button", "Click a glowing expansion marker at the map's edge",
            null); // ExpandButton / UIExpandButtonController.trigger()
        Add("Map & Camera", "Watch the map auto-expand between waves", "Survive a wave that unlocks new tiles",
            null); // MapGenerator.randomExpand()

        // -- Economy (5) ------------------------------------------------
        Add("Economy", "Spend Wood buying or upgrading a tower", "Buy or upgrade anything",
            EverSpentWood); // WoodManager.RemoveWood()
        Add("Economy", "See a Wood \"+N\" gain popup", "Wait for a wave bonus or Wood Farm payout",
            () => WoodGainPopup.EverFired);
        Add("Economy", "Spend Ecto on a Boon", "Buy any Boon from the shop",
            EverSpentEcto); // EctoManager.RemoveEctos()
        Add("Economy", "Watch a Boon buff nearby towers or enemies", "Drop a Boon near your towers",
            null); // Boon.addBoon()
        Add("Economy", "Collect a wave-complete Wood/Ecto bonus", "Survive a full wave",
            RoundEverAdvanced); // RoundController.OnRoundComplete()

        // -- Wave & Match Flow (2) ----------------------------------------
        Add("Wave & Match Flow", "Take Main Hall damage", "Let an enemy reach the hall",
            MainHallEverDamaged); // HealthBar.updatePlayerHealth()
        Add("Wave & Match Flow", "See the Main Hall catch fire below 30% HP", "Let lives drop low, or run 'burnhall on' in console",
            () => MainHallBurnTest.IsBurning);

        // -- Shop & Deck UI (4) -- previously-unexercised functions -----
        Add("Shop & Deck UI", "Scroll the shop panel left or right", "Use the shop's scroll arrows",
            null); // UIScrollViewController.LeftButtonClick() / RightButtonClick()
        Add("Shop & Deck UI", "Show or hide the shop panel", "Click the shop's show/hide toggle",
            null); // UIScrollViewController.DisplayButtonClick()
        Add("Shop & Deck UI", "Swap a tower in your loadout mid-match", "Drag a new tower into an active deck slot",
            null); // DeckSelectionUI.BeginGameplayDrag() / EndGameplayDrag()
        Add("Shop & Deck UI", "Watch a tower's live cost readout", "Hover or select a tower card in the shop",
            null); // CostDisplay

        // -- Utility & Dev Tools (7) --------------------------------------
        Add("Utility & Dev Tools", "Open the dev console", "Press [`] (backtick)",
            () => DevConsole.EverOpened);
        Add("Utility & Dev Tools", "Run a dev console command", "Try 'wood 100' or 'test'",
            () => DevConsole.EverRanCommand);
        Add("Utility & Dev Tools", "Skip a wave via the dev console", "Open console, run 'skipwave'",
            () => DevConsole.EverSkippedWave);
        Add("Utility & Dev Tools", "Pause the game", "Open the Pause menu",
            () => Mathf.Approximately(Time.timeScale, 0f)); // PauseMenu.Pause()
        Add("Utility & Dev Tools", "Resume from pause", "Click Resume, or press [Space]/[Esc] again",
            null); // PauseMenu.Resume()
        Add("Utility & Dev Tools", "Use Fast-Forward", "Click the Fast-Forward icon",
            () => Time.timeScale > 1f); // FastForward.SpeedUp()
        Add("Utility & Dev Tools", "Use a [1]-[4] tower hotkey", "Press 1, 2, 3, or 4",
            () => TowerHotkeys.EverUsed);
    }

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    private GameObject uiRoot;

    // "Increase function showcase size" - bumped up from 34/480/300 (and row/
    // header font sizes below bumped to match) so the expanded panel reads
    // comfortably at normal viewing distance instead of being a cramped strip.
    private const float CollapsedHeight = 40f;
    private const float ExpandedMaxHeight = 680f;
    private const float PanelWidth = 380f;

    private RectTransform panelRect;

    private void BuildUi() {
        GameObject canvasObj = new GameObject("FunctionShowcaseCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000; // above gameplay UI, below DevConsole (10000)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        uiRoot = canvasObj;

        // Panel now anchors to the top-left corner only (not full screen height)
        // and starts at CollapsedHeight - just the header bar - instead of a
        // permanent full-height 400px-wide strip.
        GameObject panel = new GameObject("ChecklistPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(PanelWidth, CollapsedHeight);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        Font font = RuntimeUIFont.Get();

        // Header bar: summary text + collapse/expand toggle. Always visible,
        // always exactly CollapsedHeight tall, and the only thing shown while
        // collapsed.
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panel.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, CollapsedHeight);

        Button headerBtn = headerObj.AddComponent<Button>();
        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(1f, 1f, 1f, 0.001f); // invisible but clickable
        headerBtn.targetGraphic = headerBg;
        headerBtn.onClick.AddListener(ToggleExpanded);

        GameObject summaryObj = new GameObject("Summary");
        summaryObj.transform.SetParent(headerObj.transform, false);
        RectTransform summaryRect = summaryObj.AddComponent<RectTransform>();
        summaryRect.anchorMin = Vector2.zero;
        summaryRect.anchorMax = Vector2.one;
        summaryRect.offsetMin = new Vector2(10f, 0f);
        summaryRect.offsetMax = new Vector2(-30f, 0f);
        summaryText = summaryObj.AddComponent<Text>();
        summaryText.font = font;
        summaryText.fontSize = 16;
        summaryText.fontStyle = FontStyle.Bold;
        summaryText.color = Color.white;
        summaryText.alignment = TextAnchor.MiddleLeft;

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(headerObj.transform, false);
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-8f, 0f);
        arrowRect.sizeDelta = new Vector2(20f, 0f);
        toggleArrowText = arrowObj.AddComponent<Text>();
        toggleArrowText.font = font;
        toggleArrowText.fontSize = 16;
        toggleArrowText.color = Color.white;
        toggleArrowText.alignment = TextAnchor.MiddleCenter;
        toggleArrowText.text = "\u25B8"; // collapsed indicator

        // Scrollable body - only exists below the header, only visible while
        // expanded, and capped at ExpandedMaxHeight so a 48-row list scrolls
        // instead of growing the panel past the bottom of the screen.
        scrollArea = new GameObject("ScrollArea");
        scrollArea.transform.SetParent(panel.transform, false);
        RectTransform scrollAreaRect = scrollArea.AddComponent<RectTransform>();
        scrollAreaRect.anchorMin = new Vector2(0f, 1f);
        scrollAreaRect.anchorMax = new Vector2(1f, 1f);
        scrollAreaRect.pivot = new Vector2(0f, 1f);
        scrollAreaRect.anchoredPosition = new Vector2(0f, -CollapsedHeight);
        scrollAreaRect.sizeDelta = new Vector2(0f, ExpandedMaxHeight - CollapsedHeight);

        Image scrollAreaBg = scrollArea.AddComponent<Image>();
        scrollAreaBg.color = new Color(0f, 0f, 0f, 0.15f);
        ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRect;

        GameObject listObj = new GameObject("List");
        listObj.transform.SetParent(viewport.transform, false);
        RectTransform listRect = listObj.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0f, 1f);
        listRect.anchoredPosition = Vector2.zero;
        listRect.sizeDelta = new Vector2(0f, 0f);
        scrollRect.content = listRect;

        VerticalLayoutGroup layout = listObj.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.spacing = 1f;
        layout.padding = new RectOffset(10, 10, 6, 6);
        ContentSizeFitter fitter = listObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string lastCategory = null;

        foreach (Item item in items) {
            if (item.Category != lastCategory) {
                lastCategory = item.Category;
                GameObject header = new GameObject("Header_" + item.Category);
                header.transform.SetParent(listObj.transform, false);
                LayoutElement headerLe = header.AddComponent<LayoutElement>();
                headerLe.preferredHeight = 24f;
                Text headerText = header.AddComponent<Text>();
                headerText.font = font;
                headerText.fontSize = 15;
                headerText.fontStyle = FontStyle.Bold;
                headerText.color = new Color(1f, 0.85f, 0.4f, 1f);
                headerText.text = item.Category;
                headerText.alignment = TextAnchor.LowerLeft;
            }

            GameObject rowObj = new GameObject("Row");
            rowObj.transform.SetParent(listObj.transform, false);
            LayoutElement rowLe = rowObj.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 18f;

            Button rowBtn = rowObj.AddComponent<Button>();
            Text rowText = rowObj.AddComponent<Text>();
            rowText.font = font;
            rowText.fontSize = 13;
            rowText.color = Color.white;
            rowText.alignment = TextAnchor.MiddleLeft;
            rowText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rowBtn.targetGraphic = rowText;

            item.RowText = rowText;

            Item captured = item; // local copy for the closure
            rowBtn.onClick.AddListener(() => {
                if (captured.AutoCheck == null) {
                    captured.Done = !captured.Done;
                    RefreshRows();
                }
                // Auto-tracked items can't be manually unchecked/rechecked -
                // clicking them is a no-op, same as clicking a disabled button.
            });
        }

        // No more "Back to Menu" button here - PauseMenu ([ESC]) already has its
        // own "Load Menu" exit, so this doesn't need a second one.

        SetExpanded(false);
        RefreshRows();
    }

    private void ToggleExpanded() {
        SetExpanded(!expanded);
    }

    private void SetExpanded(bool value) {
        expanded = value;
        if (scrollArea != null) scrollArea.SetActive(expanded);
        if (toggleArrowText != null) toggleArrowText.text = expanded ? "\u25BE" : "\u25B8";
        if (panelRect != null) {
            float height = expanded ? ExpandedMaxHeight : CollapsedHeight;
            panelRect.sizeDelta = new Vector2(PanelWidth, height);
        }
    }

    private void RefreshRows() {
        int done = 0;
        foreach (Item item in items) {
            if (item.Done) done++;
            if (item.RowText == null) continue;

            string mark = item.Done ? "[x] " : "[ ] ";
            string tag = item.AutoCheck != null ? " (auto)" : " (tap to mark)";
            item.RowText.text = mark + item.Title + (item.Done ? "" : " - " + item.How) + (item.Done ? "" : tag);
            item.RowText.color = item.Done ? new Color(0.55f, 1f, 0.55f) : Color.white;
        }

        if (summaryText != null)
            summaryText.text = "Function Showcase - " + done + " / " + items.Count + (expanded ? "" : " (tap to expand)");
    }
}
