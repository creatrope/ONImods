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
                Alignment = TextAnchor.MiddleLeft,
            };

            if (icon != null)
            {
                var iconLabel = new PLabel()
                {
                    Sprite = icon,
                    ToolTip = rewardName,
                };
                iconLabel.AddOnRealize(go =>
                {
                    var img = go.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (img != null && img.sprite != null)
                    {
                        var sprite = img.sprite;
                        img.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
                        img.preserveAspect = true;
                    }
                });
                linePanel.AddChild(iconLabel);
            }

            linePanel.AddChild(new PLabel()
            {
                Text = $"{memento.GetDesc()}",
                TextAlignment = TextAnchor.MiddleLeft,
                FlexSize = Vector2.right, // Use Vector2.right to fill horizontally
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            return linePanel;
        }

        public static void ShowSideScreen()
        {
            var dialog = new PDialog("ScrollPaneDialog")
            {
                Size = new Vector2(600, 700),
                MaxSize = new Vector2(600, 700),
                SortKey = 300f,
            };

            var scrollBody = new PPanel("ScrollBody")
            {
                Direction = PanelDirection.Vertical,
                FlexSize = Vector2.right,
                Spacing = -5,
                Margin = new RectOffset(0, 0, -5, -5),
                BackColor = Color.white,
                Alignment = TextAnchor.UpperLeft // Add this line
            };

            var scrollPane = new PScrollPane("ScrollPane")
            {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.right,
                TrackSize = 15,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = false,
                BackColor = Color.white,
            };

            var mementos = UnityEngine.Object.FindObjectsOfType<MementoModifiable>().ToList();
            foreach (var memento in mementos)
                    scrollBody.AddChild(GetMementoLine(memento));

            dialog.Body.AddChild(scrollPane);
            dialog.Title = "Memento Gallery";
            dialog.AddButton("ok", "OK", null);

            var dialogGO = dialog.Build();
            var kscreen = dialogGO.GetComponent<KScreen>();
            if (kscreen != null)
                kscreen.ConsumeMouseScroll = true;
            kscreen?.Activate();
        }
    }
}