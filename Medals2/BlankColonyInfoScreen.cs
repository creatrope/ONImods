using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A minimal ONI-style modal window, blank except for a close button and ONI-like color scheme.
/// </summary>
public class BlankColonyInfoScreen : MonoBehaviour
{
    private GameObject modal;
    private GameObject panel;

    public static void Show()
    {
        // Prevent multiple modals
        if (GameObject.Find("BlankColonyInfoModal") != null)
            return;

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        // Modal background
        var modalObj = new GameObject("BlankColonyInfoModal", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(canvas.transform, false);
        var modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;
        var modalImg = modalObj.GetComponent<Image>();
        modalImg.color = new Color(0, 0, 0, 0.7f); // ONI modal background

        // Centered panel
        var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(modalObj.transform, false);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.18f, 0.22f, 0.25f, 1f); // ONI panel color

        // Close button
        var closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Button), typeof(Image));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        var closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.sizeDelta = new Vector2(32, 32);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        var closeBtn = closeBtnObj.GetComponent<Button>();
        var closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        var closeTextObj = new GameObject("X", typeof(RectTransform), typeof(Text));
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        var closeText = closeTextObj.GetComponent<Text>();
        closeText.text = "X";
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeText.fontSize = 20;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        var closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        closeBtn.onClick.AddListener(() => Object.Destroy(modalObj));
    }
}