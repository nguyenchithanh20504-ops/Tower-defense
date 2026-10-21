/*
    "Remove Ectoplasma being display on the screen" (StartScene) + "Copy Wood
    display and make Ectos display" (Scene) + "Increase font size" (Scene).

    Previous revision built its own improvised floating black box, and - because
    it self-installed with no scene check at all - showed up on every scene
    including StartScene, which is exactly the bug the "remove it from Start
    Scene" request was about.

    This revision instead clones the real "PlayerMoneyBG" HUD box (the "Wood: X"
    display already in GameCanvas.prefab, live in Scene.unity) at runtime, so the
    Ecto counter matches its actual background sprite/font/position instead of a
    separate improvised style. That clone-source lookup is also what gates this
    correctly now: PlayerMoneyBG only exists once GameCanvas has loaded (i.e. in
    the real gameplay "Scene"), so GameObject.Find comes back null on StartScene
    and nothing is built there at all - no explicit scene-name check needed.

    Font size: bumps both the cloned Ecto text AND the original Wood text it was
    copied from, so the two match and both satisfy "increase font size".
*/
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EctoDisplay : MonoBehaviour {
    private const string WoodBoxName = "PlayerMoneyBG";
    private const float VerticalOffset = -55f;
    private const int BiggerFontSize = 22; // was best-fit up to 18 - see BumpFontSize()

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<EctoDisplay>() != null) return;
        GameObject go = new GameObject("~EctoDisplay");
        DontDestroyOnLoad(go);
        go.AddComponent<EctoDisplay>();
    }

    private void Awake() {
        SceneManager.sceneLoaded += (scene, mode) => TryBuild();
        TryBuild();
    }

    private void TryBuild() {
        GameObject wood = GameObject.Find(WoodBoxName);
        if (wood == null) return; // StartScene (or anywhere else with no HUD) - nothing to clone, nothing shown

        BumpFontSize(wood);

        GameObject ectoBox = Instantiate(wood, wood.transform.parent);
        ectoBox.name = "PlayerEctoBG";

        RectTransform woodRect = wood.GetComponent<RectTransform>();
        RectTransform ectoRect = ectoBox.GetComponent<RectTransform>();
        if (woodRect != null && ectoRect != null)
            ectoRect.anchoredPosition = woodRect.anchoredPosition + new Vector2(0f, VerticalOffset);

        // The clone carries a duplicate MoneyManager component (PlayerMoneyBG's
        // own leftover script reference) - inert since nothing calls
        // addMoney/removeMoney on it, but its Start() would otherwise overwrite
        // the real MoneyManager.main singleton. Destroyed before it ever runs
        // (Instantiate() calls Awake() synchronously but defers Start() to the
        // next frame, so destroying it this same frame skips Start() entirely).
        MoneyManager staleManager = ectoBox.GetComponent<MoneyManager>();
        if (staleManager != null) Destroy(staleManager);

        Text ectoText = ectoBox.GetComponentInChildren<Text>();
        if (ectoText != null) {
            ectoText.name = "PlayerEctoText";
            ectoText.text = "Ectos: 0";
            StartCoroutine(WireWhenReady(ectoText));
        }
    }

    private IEnumerator WireWhenReady(Text ectoText) {
        while (EctoManager.main == null) yield return null;
        EctoManager.main.SetDisplayText(ectoText);
    }

    private static void BumpFontSize(GameObject wood) {
        Text woodText = wood.GetComponentInChildren<Text>();
        if (woodText == null) return;
        // The original is best-fit (min 1 / max 18) - fixing a larger explicit
        // size makes "increase font size" actually visible instead of best-fit
        // silently shrinking it back down to whatever last fit the box.
        woodText.resizeTextForBestFit = false;
        woodText.fontSize = BiggerFontSize;
    }
}
