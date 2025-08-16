using HarmonyLib;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PeterHan.PLib.UI.PUIDelegates;

namespace Mementos
{
    [HarmonyPatch(typeof(TelepadSideScreen), "OnSpawn")]
    public static class TelepadSideScreen_AddBlankScreenButtonPatch
    {
        public static void Postfix(TelepadSideScreen __instance)
        {
            var summaryButton = __instance.viewColonySummaryBtn;
            if (summaryButton == null || summaryButton.gameObject == null || summaryButton.transform == null)
                return;

            var parent = summaryButton.transform.parent;
            if (parent == null)
                return;

            if (parent.Find("MementoGalleryButton") != null)
                return;

            var mgButtonObj = UnityEngine.Object.Instantiate(summaryButton.gameObject, parent); mgButtonObj.name = "MementoGalleryButton";
            mgButtonObj.transform.SetAsLastSibling();

            var kbutton = mgButtonObj.GetComponent<KButton>();
            if (kbutton != null)
                kbutton.ClearOnClick();
            var button = mgButtonObj.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveAllListeners();

            var tmp = mgButtonObj.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) tmp.text = "Memento Gallery";

            kbutton.onClick += MementoGallery.ShowSideScreen;
        }
    }

    public class MementoGallery : MonoBehaviour
    {
        // Returns a combined line for a given memento object: icon + reward name + desc
        public static PPanel GetMementoLine(MementoModifiable memento)
        {
            // Get reward type and name
            var reward = memento.rewardType;
            var rewardName = reward.ToString();

            var anim = Mementos.MementoData.GetAnimForReward(reward);
            Debug.Log($"GetAnimForReward({reward}) returned: {anim}");

            var kanim = Assets.GetAnim(anim); // Returns KAnimFile
            Debug.Log($"Assets.GetAnim({anim}) returned: {kanim}");

            Sprite icon = null;
            if (kanim != null)
            {
                icon = Def.GetUISpriteFromMultiObjectAnim(kanim, "icon", false);
                Debug.Log($"Def.GetUISpriteFromMultiObjectAnim({kanim}, \"icon\", false) returned: {icon}");
            }
            else
            {
                Debug.Log("kanim is null, skipping icon generation.");
            }

            var linePanel = new PPanel("MementoLinePanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 4
            };

            if (false && icon != null)
            {
                var iconLabel = new PLabel()
                {
                    Sprite = icon,
                    ToolTip = rewardName,
                    FlexSize = new Vector2(1, 1) // Minimal flex to avoid stretching
                };
                // Set a small margin to control spacing if needed
                linePanel.AddChild(iconLabel);

                // Scale down the icon after realization
                iconLabel.AddOnRealize(go =>
                {
                    var img = go.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 24);
                        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24);
                        img.preserveAspect = true;
                    }
                });
            }

            linePanel.AddChild(new PLabel()
            {
                Text = $"{memento.GetDesc()}",
                TextAlignment = TextAnchor.MiddleLeft,
                FlexSize = new Vector2(500, 0),
                TextStyle = PUITuning.Fonts.TextDarkStyle // Ensures black font
            });

            return linePanel;
        }

        public static void ShowSideScreen()
        {
            var dialog = new PDialog("ScrollPaneDialog") {
                Size = new Vector2(700, 700),
                MaxSize = new Vector2(700, 700),
                SortKey = 300f
            };

            // Create an empty vertical panel as the scroll body
            var scrollBody = new PPanel("ScrollBody") {
                Direction = PanelDirection.Vertical,
                FlexSize = Vector2.right,
                BackColor = PUITuning.Colors.BackgroundLight
            };

            // Create the scroll pane exactly as requested
            var scrollPane = new PScrollPane("ScrollPane") {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.right,
                TrackSize = 15,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = false,
            };

            AddLinesToScrollBody(scrollBody, 50);

            dialog.Body.AddChild(scrollPane);
            dialog.Title = "ScrollPane Example";
            dialog.AddButton("ok", "OK", null);

            // Build the dialog and set ConsumeMouseScroll on the root KScreen
            var dialogGO = dialog.Build();
            var kscreen = dialogGO.GetComponent<KScreen>();
            if (kscreen != null)
                kscreen.ConsumeMouseScroll = true;
            kscreen?.Activate(); // Ensure it's active

            // Show the dialog (if not already shown by Build/Activate)
        }

        public static void AddLinesToScrollBody(PPanel scrollBody, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var linePanel = new PPanel($"LinePanel_{i}")
                {
                    Direction = PanelDirection.Horizontal,
                    Spacing = 4
                };
                linePanel.AddChild(new PLabel()
                {
                    Text = $"Line {i + 1}",
                    TextAlignment = TextAnchor.MiddleLeft,
                    FlexSize = new Vector2(1, 0),
                    TextStyle = PUITuning.Fonts.TextDarkStyle
                });
                scrollBody.AddChild(linePanel);
            }
        }
    }
}