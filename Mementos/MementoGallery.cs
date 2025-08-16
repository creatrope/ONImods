using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections.Generic;
using PeterHan.PLib.UI;
using System;
using System.Linq;

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

            var linePanel = new PPanel("MementoLinePanel") {
                Direction = PanelDirection.Horizontal,
                Spacing = 4
            };

            if (icon != null) {
                var iconLabel = new PLabel()
                {
                    Sprite = icon,
                    ToolTip = rewardName,
                    FlexSize = new Vector2(1, 1) // Minimal flex to avoid stretching
                };
                // Set a small margin to control spacing if needed
                linePanel.AddChild(iconLabel);

                // Scale down the icon after realization
                iconLabel.AddOnRealize(go => {
                    var img = go.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (img != null) {
                        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 24);
                        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24);
                        img.preserveAspect = true;
                    }
                });
            }

            linePanel.AddChild(new PLabel() {
                Text = $"{memento.GetDesc()}",
                TextAlignment = TextAnchor.MiddleLeft 
            });

            return linePanel;
        }

        public static void ShowSideScreen()
        {
            var mementos = GameObject.FindObjectsOfType<MementoModifiable>().ToList();

            var panel = new PPanel("RandomListPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 4
            };

            foreach (var memento in mementos)
            {
                var linePanel = GetMementoLine(memento);
                panel.AddChild(linePanel);
            }

            var dialog = new PDialog("Memento Gallery");
            dialog.Body.AddChild(panel);
            dialog.Title = "Memento Gallery";
            dialog.AddButton("ok", "OK", null); 
            dialog.Show();
        }
    }
}