using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections.Generic;
using PeterHan.PLib.UI;

namespace Mementos
{
    [HarmonyPatch(typeof(TelepadSideScreen), "OnSpawn")]
    public static class TelepadSideScreen_AddBlankScreenButtonPatch
    {
        private static void Postfix(TelepadSideScreen __instance)
        {
            var summaryButton = __instance.viewColonySummaryBtn;
            if (summaryButton == null || summaryButton.gameObject == null || summaryButton.transform == null)
                return;

            var parent = summaryButton.transform.parent;
            if (parent == null)
                return;

            // Prevent duplicate button creation
            if (parent.Find("MementoGalleryButton") != null)
                return;

            var mgButtonObj = Object.Instantiate(summaryButton.gameObject, parent);
            mgButtonObj.name = "MementoGalleryButton";
            mgButtonObj.transform.SetAsLastSibling();

            var text = mgButtonObj.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = "Memento Gallery";
            var tmpText = mgButtonObj.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmpText != null)
                tmpText.text = "Memento Gallery";

            var button = mgButtonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ShowPlibModal);
            }
            else
            {
                var kbutton = mgButtonObj.GetComponent<KButton>();
                if (kbutton != null)
                {
                    kbutton.ClearOnClick();
                    kbutton.onClick += ShowPlibModal;
                }
            }
        }

        private static PPanel CreateMementoRow(Mementos.MementoModifiable memento)
        {
            // Get anim string from reward
            var animName = Mementos.MementoData.GetAnimForReward(memento.rewardType);
            Debug.Log($"[Mementos] {memento.GetName()} anim: {animName}");

            // Get KAnimFile asset
            KAnimFile kanim = null;
            if (!string.IsNullOrEmpty(animName))
                kanim = Assets.GetAnim(animName);

            // Use Def.GetUISpriteFromMultiObjectAnim to get the "icon" sprite
            Sprite iconSprite = null;
            if (kanim != null)
                iconSprite = Def.GetUISpriteFromMultiObjectAnim(kanim, "icon");

            // Compose a horizontal panel for icon + desc
            var row = new PPanel("MementoRow")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 4,
                FlexSize = Vector2.right
            };

            if (iconSprite != null)
            {
                row.AddChild(new PLabel("Icon")
                {
                    Sprite = iconSprite,
                    Text = "",
                    TextAlignment = TextAnchor.MiddleCenter,
                });
            }
            else
            {
                row.AddChild(new PLabel("NoIcon")
                {
                    Text = "?",
                    TextAlignment = TextAnchor.MiddleCenter,
                });
            }

            // Only show the description, not the name
            row.AddChild(new PLabel("Desc")
            {
                Text = memento.GetDesc(),
                FlexSize = Vector2.right
            });

            return row;
        }

        private static void ShowPlibModal()
        {
            var dialog = new PDialog("MementoGalleryDialog")
            {
                Title = "Memento Gallery",
                Size = new Vector2(300, 250),
                MaxSize = new Vector2(300, 250),
                SortKey = 300.0f
            }
            .AddButton("ok", "OK", null, PUITuning.Colors.ButtonPinkStyle);

            var dialogBody = dialog.Body;

            var dialogBodyChild = new PPanel("MementoGallery_RecordsPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 4,
                FlexSize = Vector2.one
            };
            dialogBody.AddChild(dialogBodyChild);

            var scrollBody = new PPanel("ScrollContent")
            {
                Spacing = 2,
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                FlexSize = Vector2.right
            };

            var mementos = UnityEngine.Object.FindObjectsOfType<Mementos.MementoModifiable>();
            Debug.Log($"[Mementos] Found {mementos.Length} mementos.");

            foreach (var memento in mementos)
            {
                Debug.Log($"[Mementos] Adding memento: {memento.GetName()} - {memento.GetDesc()}");
                var row = CreateMementoRow(memento);
                scrollBody.AddChild(row);
            }

            var scrollPane = new PScrollPane()
            {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.one,
                TrackSize = 16f,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = true
            };
            dialogBodyChild.AddChild(scrollPane);

            dialog.Show();
        }
    }
}