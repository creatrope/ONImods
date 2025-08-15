using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using Medals2;

[HarmonyPatch(typeof(TelepadSideScreen), "OnSpawn")]
public static class TelepadSideScreen_AddMementoGalleryButtonPatch
{
    private static void Postfix(TelepadSideScreen __instance)
    {
        Debug.Log("[MementoGallery] Postfix called for TelepadSideScreen.OnSpawn");

        if (__instance == null)
        {
            Debug.LogError("[MementoGallery] __instance is null!");
            return;
        }

        var summaryButton = __instance.viewColonySummaryBtn;
        if (summaryButton == null || summaryButton.gameObject == null || summaryButton.transform == null)
        {
            Debug.LogError("[MementoGallery] summaryButton or its gameObject/transform is null!");
            return;
        }

        var parent = summaryButton.transform.parent;
        if (parent == null)
        {
            Debug.LogError("[MementoGallery] summaryButton.transform.parent is null!");
            return;
        }

        Debug.Log("[MementoGallery] Instantiating MementoGalleryButton...");
        var mementoButtonObj = Object.Instantiate(summaryButton.gameObject, parent);
        mementoButtonObj.name = "MementoGalleryButton";
        mementoButtonObj.transform.SetSiblingIndex(summaryButton.transform.GetSiblingIndex() + 1);

        var text = mementoButtonObj.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = "Memento Gallery";
            Debug.Log("[MementoGallery] Set Text component text.");
        }
        else
        {
            Debug.LogWarning("[MementoGallery] No Text component found in button.");
        }

        var tmpText = mementoButtonObj.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = "Memento Gallery";
            Debug.Log("[MementoGallery] Set TMP_Text component text.");
        }

        var button = mementoButtonObj.GetComponent<Button>();
        if (button == null)
        {
            button = mementoButtonObj.GetComponentInChildren<Button>(true);
            if (button != null)
                Debug.Log("[MementoGallery] Found Button component in children.");
        }
        if (button == null)
        {
            // Try KButton (used in ONI UI)
            var kbutton = mementoButtonObj.GetComponent<KButton>();
            if (kbutton == null)
                kbutton = mementoButtonObj.GetComponentInChildren<KButton>(true);
            if (kbutton != null)
            {
                Debug.Log("[MementoGallery] Found KButton component, using it.");
                kbutton.ClearOnClick(); // Clear existing event handlers
                kbutton.onClick += () =>
                {
                    Debug.Log("[MementoGallery] KButton clicked, calling ShowMementoGalleryScreen.");
                    ShowMementoGalleryScreen();
                };
            }
            else
            {
                Debug.LogError("[MementoGallery] No Button or KButton component found on mementoButtonObj or its children!");
            }
        }
        else
        {
            button.onClick.RemoveAllListeners();
            Debug.Log("[MementoGallery] Cleared old listeners, adding new listener.");
            button.onClick.AddListener(() =>
            {
                Debug.Log("[MementoGallery] Button clicked, calling ShowMementoGalleryScreen.");
                ShowMementoGalleryScreen();
            });
        }
    }

    private static void ShowMementoGalleryScreen()
    {
        Debug.Log("[MementoGallery] ShowMementoGalleryScreen called.");

        if (GameObject.Find("MementoGalleryModal") != null)
        {
            Debug.LogWarning("[MementoGallery] Modal already exists, aborting.");
            return;
        }

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MementoGallery] No Canvas found in scene!");
            return;
        }

        Debug.Log("[MementoGallery] Creating modal background...");
        var modal = new GameObject("MementoGalleryModal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        var modalRect = modal.GetComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0, 0);
        modalRect.anchorMax = new Vector2(1, 1);
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        Debug.Log("[MementoGallery] Creating panel...");
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(modal.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400); // 50% wider (400 * 1.5 = 600)
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Debug.Log("[MementoGallery] Creating title...");
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40);
        titleRect.anchoredPosition = new Vector2(0, -10);
        var title = titleObj.GetComponent<Text>();
        title.text = "Memento Gallery";
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 24;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;

        Debug.Log("[MementoGallery] Creating scroll view...");
        var scrollViewObj = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollViewObj.transform.SetParent(panel.transform, false);
        var scrollRect = scrollViewObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 20);
        scrollRect.offsetMax = new Vector2(-20, -60);
        scrollViewObj.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        Debug.Log("[MementoGallery] Creating content for scroll view...");
        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        // Add a ContentSizeFitter to the contentObj to allow vertical resizing
        var fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Also, ensure your VerticalLayoutGroup settings:
        var layout = contentObj.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        var scroll = scrollViewObj.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.viewport = scrollRect;

        Debug.Log("[MementoGallery] Finding mementos...");
        var mementos = Object.FindObjectsOfType<MementoModifiable>();
        Debug.Log($"[MementoGallery] Found {mementos.Length} mementos.");

        if (mementos.Length == 0)
        {
            Debug.Log("[MementoGallery] No mementos found, adding message.");
            var noMementosObj = new GameObject("NoMementos", typeof(RectTransform), typeof(Text));
            noMementosObj.transform.SetParent(contentObj.transform, false);
            var noMementosText = noMementosObj.GetComponent<Text>();
            noMementosText.text = "No mementos have been awarded yet.";
            noMementosText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            noMementosText.fontSize = 16;
            noMementosText.color = Color.white;
            noMementosText.alignment = TextAnchor.MiddleCenter;
        }
        else
        {
            foreach (var memento in mementos)
            {
                Debug.Log($"[MementoGallery] Adding memento: {memento.GetName()} - {memento.GetDesc()}");

                // Create a horizontal container for icon + text
                var entryObj = new GameObject("MementoEntry", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                entryObj.transform.SetParent(contentObj.transform, false);
                var entryRect = entryObj.GetComponent<RectTransform>();
                entryRect.anchorMin = new Vector2(0, 1);
                entryRect.anchorMax = new Vector2(1, 1);
                entryRect.pivot = new Vector2(0.5f, 1);
                entryRect.sizeDelta = new Vector2(0, 30);
                entryRect.anchoredPosition = Vector2.zero;

                // Add icon image (get anim file from reward type)
                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(entryObj.transform, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(30, 30); // Match text height (30)
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.pivot = new Vector2(0, 0.5f);

                var iconImage = iconObj.GetComponent<Image>();

                string anim = MementoData.GetAnimForReward(memento.rewardType);
                Debug.Log($"[MementoGallery] Using animation '{anim}' for memento '{memento.GetName()}'.");
                KAnimFile kanim = Assets.GetAnim(anim);
                Sprite iconSprite = Def.GetUISpriteFromMultiObjectAnim(kanim, "icon", false, string.Empty);
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.color = iconSprite != null ? Color.white : new Color(0, 0, 0, 0);

                // Add text
                var entryTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                entryTextObj.transform.SetParent(entryObj.transform, false);
                var entryText = entryTextObj.GetComponent<Text>();
                entryText.text = $"{memento.GetDesc()}";
                entryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                entryText.fontSize = 16;
                entryText.color = Color.white;
                entryText.alignment = TextAnchor.MiddleLeft;
                entryText.raycastTarget = false;
                entryText.horizontalOverflow = HorizontalWrapMode.Overflow;
                entryText.verticalOverflow = VerticalWrapMode.Overflow;
                var entryTextRect = entryTextObj.GetComponent<RectTransform>();
                entryTextRect.sizeDelta = new Vector2(0, 30);
                entryTextRect.anchorMin = new Vector2(0, 0.5f);
                entryTextRect.anchorMax = new Vector2(1, 0.5f);
                entryTextRect.pivot = new Vector2(0, 0.5f);
            }
        }

        Debug.Log("[MementoGallery] Creating close button...");
        var closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Button), typeof(Image));
        closeBtnObj.transform.SetParent(panel.transform, false);
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
        closeBtn.onClick.AddListener(() =>
        {
            Debug.Log("[MementoGallery] Close button clicked, destroying modal.");
            Object.Destroy(modal);
        });

        Debug.Log("[MementoGallery] Modal should now be visible.");
    }
}