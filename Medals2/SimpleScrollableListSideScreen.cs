using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple scrollable side screen for displaying a list of text rows.
/// </summary>
public class SimpleScrollableListSideScreen : SideScreenContent
{
    private ScrollRect scrollRect;
    private RectTransform viewport;
    private RectTransform content;
    private VerticalLayoutGroup layout;
    private List<TMPro.TMP_Text> rows = new List<TMPro.TMP_Text>();
    private const float RowHeight = 30f;

    public override void OnSpawn()
    {
        base.OnSpawn();

        // Ensure this GameObject has a RectTransform and Image for white background
        var thisRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        thisRect.anchorMin = new Vector2(0, 0);
        thisRect.anchorMax = new Vector2(1, 1);
        thisRect.offsetMin = Vector2.zero;
        thisRect.offsetMax = Vector2.zero;

        var image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        image.color = Color.white;

        // Set up ScrollRect
        scrollRect = gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(transform, false);
        viewport = viewportGO.GetComponent<RectTransform>();
        viewport.anchorMin = new Vector2(0, 0);
        viewport.anchorMax = new Vector2(1, 1);
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0); // transparent
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewport, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        scrollRect.content = content;

        // Layout
        layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 0;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Set a minimum height for the scroll area
        thisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
    }

    public void AddRow(string text)
    {
        if (layout == null)
            return;

        var rowObj = new GameObject("Row", typeof(RectTransform));
        rowObj.transform.SetParent(content, false);

        // Add a CanvasRenderer so TextMeshProUGUI can render
        rowObj.AddComponent<CanvasRenderer>();

        var textComp = rowObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 20;
        textComp.enableWordWrapping = true;
        textComp.alignment = TMPro.TextAlignmentOptions.Left;
        textComp.color = Color.black;
        textComp.raycastTarget = false; // Optional: disables raycast for text
        rows.Add(textComp);

        var layoutElement = rowObj.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.preferredHeight = RowHeight;
    }

    public void ClearRows()
    {
        foreach (var row in rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();
    }

    public override void SetTarget(GameObject target) { }
    public override bool IsValidForTarget(GameObject target) => true;
}