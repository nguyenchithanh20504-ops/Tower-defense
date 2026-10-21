
using UnityEngine;
using UnityEngine.UI;

public class EctoManager : MonoBehaviour {
    [SerializeField] private Text playerEctoTxt;

    public static EctoManager main;

    // "Copy Wood display and make Ectos display" - EctoDisplay.cs clones the
    // real "Wood: X" HUD box at runtime (rather than an improvised overlay) and
    // wires its Text component in here once the clone exists, since
    // playerEctoTxt has no other public setter and nothing in the scene wires
    // it via the Inspector.
    public void SetDisplayText(Text text) {
        playerEctoTxt = text;
        Refresh();
    }

    private static int currEctos;
    public int startEctos = 100;

    private void Start() {
        if (main == null) main = this;
        currEctos = startEctos;
        Refresh();
    }

    public int GetCurrEctos() { return currEctos; }

    public void AddEctos(int amount) {
        currEctos += amount;
        Refresh();
    }

    public void RemoveEctos(int amount) {
        currEctos -= amount;
        Refresh();
    }

    private void Refresh() {
        if (playerEctoTxt != null)
            playerEctoTxt.text = $"Ectos: {currEctos}";
    }
}
