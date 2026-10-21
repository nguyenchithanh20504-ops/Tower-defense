using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

class UIScrollViewController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [SerializeField] private Text text;
    [SerializeField] private float scrollSpeed = 0.1f; // no longer used - kept so existing Inspector values aren't lost; see Page() below
    [SerializeField] private string shopName;
    [SerializeField] private float distanceY = 0.1f;

    [Tooltip("\"Tower scrolling but make it so it scroll between 4 towers instead of pages\" - " +
             "how many cards Left/Right moves by in one click.")]
    [SerializeField] private int towersPerPage = 4;

    private bool isHidden = false;

    public void LeftButtonClick()
    {
        Page(-1);
    }

    public void RightButtonClick()
    {
        Page(1);
    }

    // horizontalNormalizedPosition runs 0..1 across the whole scrollable range,
    // and one full traversal of that range covers (content.childCount -
    // towersPerPage) card-widths, since towersPerPage cards are already visible
    // in the viewport at any position. Converting towersPerPage into that same
    // normalized unit means Left/Right always lands exactly towersPerPage cards
    // over, however many towers are in the shop - instead of the old flat
    // scrollSpeed (10%) jump, which didn't line up with card boundaries at all
    // and had nothing to do with how many towers were actually in the shop.
    private void Page(int direction)
    {
        if (scrollRect == null || scrollRect.content == null) return;

        int totalCards = scrollRect.content.childCount;
        int scrollableSteps = totalCards - towersPerPage;
        if (scrollableSteps <= 0)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            return;
        }

        float step = (float)towersPerPage / scrollableSteps;
        float target = scrollRect.horizontalNormalizedPosition + direction * step;
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(target);
    }

    public void DisplayButtonClick()
    {
        if (isHidden)
        {
            text.text = $"Hide {shopName}";
            leftButton.SetActive(true);
            rightButton.SetActive(true);
            gameObject.transform.position += new Vector3(0, distanceY, 0);
            isHidden = false;
        }
        else
        {
            text.text = $"Show {shopName}";
            leftButton.SetActive(false);
            rightButton.SetActive(false);
            gameObject.transform.position -= new Vector3(0, distanceY, 0);
            isHidden = true;
        }
    }

}