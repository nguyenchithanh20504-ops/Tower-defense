/*
    "Copy paste the New Game button and paste function showcase in it."

    Was previously a hand-built green button of its own; now it's a runtime clone
    of the real "StartExpGame" (New Game) button from StartCanvas.prefab, so it
    matches the game's actual button art/font instead of an improvised box. Cloning
    at runtime (rather than hand-editing StartCanvas.prefab) avoids the same risk
    called out in earlier revisions of this file: a raw text edit to a prefab's
    serialized UI hierarchy can silently corrupt button wiring in a way that's hard
    to verify without the Editor.

    "Make Showcase scene inherit everything from Scene... so no copying" - there is
    no more ShowcaseScene.unity. This button now loads "Scene" directly (the exact
    same scene "New Game" loads) and flips FunctionShowcase's static request flag
    right before doing so, so FunctionShowcase.cs knows to build its checklist
    overlay on top of this specific load. Since it's literally the same scene file,
    any change to Scene.unity is automatically reflected here too - nothing to keep
    in sync by hand anymore.

    Scene-gated to "StartScene" only, so it never appears mid-match.
*/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ShowcaseLauncher : MonoBehaviour {
    private const string StartSceneName = "StartScene";
    private const string GameplaySceneName = "Scene";
    private const string NewGameButtonName = "StartExpGame";
    private const float VerticalOffset = -60f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<ShowcaseLauncher>() != null) return;
        GameObject go = new GameObject("~ShowcaseLauncher");
        DontDestroyOnLoad(go);
        go.AddComponent<ShowcaseLauncher>();
    }

    private void Awake() {
        SceneManager.sceneLoaded += (scene, mode) => Refresh(scene.name);
        Refresh(SceneManager.GetActiveScene().name);
    }

    private void Refresh(string sceneName) {
        // The clone lives on the StartScene GameObject it was copied from, so it's
        // destroyed automatically on scene unload - just rebuild it every time
        // StartScene loads instead of tracking a persistent instance.
        if (sceneName == StartSceneName) BuildClone();
    }

    private void BuildClone() {
        GameObject source = GameObject.Find(NewGameButtonName);
        if (source == null) {
            Debug.LogWarning("ShowcaseLauncher: couldn't find '" + NewGameButtonName + "' to clone - " +
                              "no Function Showcase button this scene load.");
            return;
        }

        GameObject clone = Instantiate(source, source.transform.parent);
        clone.name = "ShowcaseButton";

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (sourceRect != null && cloneRect != null)
            cloneRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, VerticalOffset);

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = "Function Showcase";

        Button button = clone.GetComponent<Button>();
        if (button != null) {
            // Replaces the button's onClick outright - the cloned copy still carries
            // the source button's serialized "SceneHandler.loadNewGame" persistent
            // call, and RemoveAllListeners() only clears runtime listeners, not
            // persistent ones. Assigning a brand-new event object is the same trick
            // DeckSelectionUI.WireShopButton() uses for the same reason.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => {
                FunctionShowcase.RequestShowcaseMode();
                SceneManager.LoadScene(GameplaySceneName);
            });
        }
    }
}
